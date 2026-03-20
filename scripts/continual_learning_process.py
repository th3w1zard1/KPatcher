#!/usr/bin/env python3
"""Process agent transcripts incrementally and update AGENTS.md with learned patterns."""

import json
import os
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Set, Tuple
import re

# Paths
TRANSCRIPTS_DIR = Path(r"C:\Users\boden\.cursor\projects\c-GitHub-KPatcher\agent-transcripts")
INDEX_FILE = Path(r"C:\GitHub\KPatcher\.cursor\hooks\state\continual-learning-index.json")
AGENTS_MD = Path(r"C:\GitHub\KPatcher\AGENTS.md")

def get_file_mtime_ms(filepath: Path) -> int:
    """Get file modification time in milliseconds since epoch."""
    return int(filepath.stat().st_mtime * 1000)

def discover_transcripts() -> Dict[str, int]:
    """Discover all .jsonl files and return {path: mtime_ms}."""
    transcripts = {}
    for jsonl_file in TRANSCRIPTS_DIR.rglob("*.jsonl"):
        transcripts[str(jsonl_file)] = get_file_mtime_ms(jsonl_file)
    return transcripts

def load_index() -> Dict:
    """Load the incremental index."""
    if INDEX_FILE.exists():
        with open(INDEX_FILE, 'r', encoding='utf-8') as f:
            return json.load(f)
    return {"version": 1, "transcripts": {}}

def find_transcripts_to_process(discovered: Dict[str, int], index: Dict) -> List[str]:
    """Find transcripts that need processing (new or modified)."""
    indexed = index.get("transcripts", {})
    to_process = []
    
    for path, mtime_ms in discovered.items():
        if path not in indexed:
            to_process.append(path)
        elif indexed[path].get("mtimeMs", 0) < mtime_ms:
            to_process.append(path)
    
    return to_process

def extract_user_messages(transcript_path: str) -> List[str]:
    """Extract user messages from a transcript JSONL file."""
    messages = []
    try:
        with open(transcript_path, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    event = json.loads(line)
                    # Handle both "message" and direct "content" structures
                    if event.get("role") == "user":
                        content = None
                        if "message" in event and "content" in event["message"]:
                            content = event["message"]["content"]
                        elif "content" in event:
                            content = event["content"]
                        
                        if content:
                            if isinstance(content, str):
                                messages.append(content)
                            elif isinstance(content, list):
                                # Extract text from content blocks
                                for block in content:
                                    if isinstance(block, dict):
                                        if block.get("type") == "text":
                                            messages.append(block.get("text", ""))
                                        elif "text" in block:
                                            messages.append(block.get("text", ""))
                                    elif isinstance(block, str):
                                        messages.append(block)
                except json.JSONDecodeError:
                    continue
    except Exception as e:
        print(f"Error reading {transcript_path}: {e}")
    
    return messages

def extract_patterns(messages: List[str]) -> Dict[str, List[str]]:
    """Extract high-signal patterns from user messages."""
    patterns = {
        "preferences": [],
        "facts": []
    }
    
    # Strong preference indicators (capture even if only once)
    strong_keywords = ["must be", "should be", "always", "never", "strict", "thorough", "meticulous"]
    
    # General correction keywords
    correction_keywords = [
        "should be", "must be", "always", "never", "do not", "don't",
        "prefer", "use", "avoid", "instead of", "rather than",
        "treat", "consider", "when", "if", "for"
    ]
    
    for msg in messages:
        msg_lower = msg.lower()
        
        # Skip one-off task instructions at message level
        if any(oneoff in msg_lower for oneoff in ["right now", "today", "yesterday"]):
            continue
        
        # Check for test-related strictness preferences (multi-sentence)
        if any(kw in msg_lower for kw in ["meticulous", "thorough", "strict", "assert"]) and "test" in msg_lower:
            # Extract the core preference, removing profanity and file references
            # Look for patterns like "make tests X", "tests should be X", "X asserts per stage"
            cleaned = re.sub(r'@?\w+\.(cs|md|jsonl|py)', 'file', msg)
            cleaned = re.sub(r'\(\d+-\d+\)', '', cleaned)  # Remove line numbers
            cleaned = re.sub(r'[^\w\s,.\-:;]', '', cleaned)  # Remove special chars but keep punctuation
            # Extract key phrases
            if "meticulous" in msg_lower or "thorough" in msg_lower:
                if "assert" in msg_lower or "per stage" in msg_lower:
                    patterns["preferences"].append("Tests should be meticulous and thorough with multiple assertions per stage, as strict as possible")
        
        # Extract sentences for other patterns
        sentences = re.split(r'[.!?]\s+', msg)
        for sent in sentences:
            sent = sent.strip()
            if len(sent) < 20 or len(sent) > 500:
                continue
            
            sent_lower = sent.lower()
            
            # Skip one-off task instructions
            if any(oneoff in sent_lower for oneoff in ["this file", "this test", "now", "today", "yesterday", "right now"]):
                continue
            
            # Skip transient details
            if any(transient in sent_lower for transient in ["branch", "commit", "pr #", "pull request"]):
                continue
            
            # Check for strong preferences (capture even if only once)
            has_strong = any(kw in sent_lower for kw in strong_keywords)
            has_correction = any(kw in sent_lower for kw in correction_keywords)
            
            if has_strong or has_correction:
                # Extract actionable preference
                # Look for patterns like "tests should be X", "make X strict", etc.
                if any(pattern in sent_lower for pattern in [
                    "test", "assert", "should", "must", "strict", "thorough", "meticulous"
                ]):
                    # Generalize: remove file-specific references
                    generalized = re.sub(r'@?\w+\.(cs|md|jsonl)', 'file', sent)
                    generalized = re.sub(r'\(\d+-\d+\)', '', generalized)  # Remove line numbers
                    # Remove profanity and strong language while keeping the preference
                    generalized = re.sub(r'\b(dumb|fuck|shit|damn)\w*\b', '', generalized, flags=re.IGNORECASE)
                    generalized = ' '.join(generalized.split())  # Normalize whitespace
                    if len(generalized.strip()) > 20:
                        patterns["preferences"].append(generalized.strip())
    
    return patterns

def merge_patterns(all_patterns: Dict[str, List[str]]) -> Dict[str, List[str]]:
    """Merge and deduplicate patterns."""
    merged = {
        "preferences": [],
        "facts": []
    }
    
    # Count occurrences
    pref_counts = {}
    fact_counts = {}
    
    for category in ["preferences", "facts"]:
        for pattern in all_patterns.get(category, []):
            # Normalize pattern
            normalized = pattern.lower().strip()
            if category == "preferences":
                pref_counts[normalized] = pref_counts.get(normalized, 0) + 1
            else:
                fact_counts[normalized] = fact_counts.get(normalized, 0) + 1
    
    # Include patterns that appear multiple times OR are strongly stated
    strong_indicators = ["must", "should", "always", "never", "strict", "thorough", "meticulous"]
    
    for normalized, count in pref_counts.items():
        is_strong = any(indicator in normalized for indicator in strong_indicators)
        
        if count >= 2 or is_strong:  # Multiple occurrences OR strongly stated
            # Find the original (most complete) version
            for pattern in all_patterns["preferences"]:
                if pattern.lower().strip() == normalized:
                    merged["preferences"].append(pattern)
                    break
    
    for normalized, count in fact_counts.items():
        if count >= 2:
            for pattern in all_patterns["facts"]:
                if pattern.lower().strip() == normalized:
                    merged["facts"].append(pattern)
                    break
    
    # Deduplicate semantically similar
    merged["preferences"] = list(dict.fromkeys(merged["preferences"]))[:12]
    merged["facts"] = list(dict.fromkeys(merged["facts"]))[:12]
    
    return merged

def update_agents_md(patterns: Dict[str, List[str]], existing_content: str) -> str:
    """Update AGENTS.md with new patterns, merging with existing."""
    lines = existing_content.split('\n')
    
    # Find the "## Learned User Preferences" section
    pref_start = -1
    pref_end = -1
    facts_start = -1
    facts_end = -1
    
    for i, line in enumerate(lines):
        if line.strip() == "## Learned User Preferences":
            pref_start = i
        elif pref_start >= 0 and line.startswith("##") and pref_end == -1:
            pref_end = i
        if line.strip() == "## Learned Workspace Facts":
            facts_start = i
        elif facts_start >= 0 and line.startswith("##") and facts_end == -1:
            facts_end = i
    
    # If sections don't exist, add them
    if pref_start == -1:
        # Add after "## Learned User Preferences" header if it exists, otherwise at end
        lines.append("## Learned User Preferences")
        lines.append("")
        pref_start = len(lines) - 1
        pref_end = len(lines)
    
    if facts_start == -1:
        lines.append("## Learned Workspace Facts")
        lines.append("")
        facts_start = len(lines) - 1
        facts_end = len(lines)
    
    # Extract existing bullets
    existing_prefs = []
    existing_facts = []
    
    for i in range(pref_start + 1, pref_end):
        line = lines[i].strip()
        if line.startswith("- "):
            existing_prefs.append(line[2:])
    
    for i in range(facts_start + 1, facts_end):
        line = lines[i].strip()
        if line.startswith("- "):
            existing_facts.append(line[2:])
    
    # Merge: update existing or add new
    new_prefs = existing_prefs.copy()
    new_facts = existing_facts.copy()
    
    for pattern in patterns["preferences"]:
        # Check if similar pattern exists
        pattern_lower = pattern.lower()
        found = False
        for i, existing in enumerate(new_prefs):
            if pattern_lower in existing.lower() or existing.lower() in pattern_lower:
                # Update in place
                new_prefs[i] = pattern
                found = True
                break
        if not found:
            new_prefs.append(pattern)
    
    for pattern in patterns["facts"]:
        pattern_lower = pattern.lower()
        found = False
        for i, existing in enumerate(new_facts):
            if pattern_lower in existing.lower() or existing.lower() in pattern_lower:
                new_facts[i] = pattern
                found = True
                break
        if not found:
            new_facts.append(pattern)
    
    # Limit to 12 bullets each
    new_prefs = new_prefs[:12]
    new_facts = new_facts[:12]
    
    # Rebuild content
    result_lines = []
    i = 0
    while i < len(lines):
        if i == pref_start:
            result_lines.append("## Learned User Preferences")
            result_lines.append("")
            for pref in new_prefs:
                result_lines.append(f"- {pref}")
            result_lines.append("")
            i = pref_end
        elif i == facts_start:
            result_lines.append("## Learned Workspace Facts")
            result_lines.append("")
            for fact in new_facts:
                result_lines.append(f"- {fact}")
            result_lines.append("")
            i = facts_end
        else:
            result_lines.append(lines[i])
            i += 1
    
    return '\n'.join(result_lines)

def main():
    """Main processing workflow."""
    print("Discovering transcripts...")
    discovered = discover_transcripts()
    print(f"Found {len(discovered)} transcript files")
    
    print("Loading index...")
    index = load_index()
    
    print("Finding transcripts to process...")
    to_process = find_transcripts_to_process(discovered, index)
    print(f"Found {len(to_process)} transcripts to process")
    
    if not to_process:
        print("No high-signal memory updates.")
        return
    
    # Process transcripts
    all_patterns = {"preferences": [], "facts": []}
    
    for transcript_path in to_process:
        print(f"Processing {Path(transcript_path).name}...")
        messages = extract_user_messages(transcript_path)
        patterns = extract_patterns(messages)
        all_patterns["preferences"].extend(patterns["preferences"])
        all_patterns["facts"].extend(patterns["facts"])
    
    # Merge patterns
    merged = merge_patterns(all_patterns)
    
    if not merged["preferences"] and not merged["facts"]:
        print("No high-signal memory updates.")
        # Still update index
        for path in to_process:
            index["transcripts"][path] = {
                "mtimeMs": discovered[path],
                "lastProcessedAt": datetime.utcnow().isoformat() + "Z"
            }
        # Remove entries for files that no longer exist
        existing_paths = set(discovered.keys())
        index["transcripts"] = {k: v for k, v in index["transcripts"].items() if k in existing_paths}
        
        INDEX_FILE.parent.mkdir(parents=True, exist_ok=True)
        with open(INDEX_FILE, 'w', encoding='utf-8') as f:
            json.dump(index, f, indent=2)
        return
    
    # Read existing AGENTS.md
    existing_content = ""
    if AGENTS_MD.exists():
        with open(AGENTS_MD, 'r', encoding='utf-8') as f:
            existing_content = f.read()
    
    # Update AGENTS.md
    updated_content = update_agents_md(merged, existing_content)
    
    with open(AGENTS_MD, 'w', encoding='utf-8') as f:
        f.write(updated_content)
    
    print(f"Updated AGENTS.md with {len(merged['preferences'])} preferences and {len(merged['facts'])} facts")
    
    # Update index
    for path in to_process:
        index["transcripts"][path] = {
            "mtimeMs": discovered[path],
            "lastProcessedAt": datetime.utcnow().isoformat() + "Z"
        }
    
    # Remove entries for files that no longer exist
    existing_paths = set(discovered.keys())
    index["transcripts"] = {k: v for k, v in index["transcripts"].items() if k in existing_paths}
    
    INDEX_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(INDEX_FILE, 'w', encoding='utf-8') as f:
        json.dump(index, f, indent=2)
    
    print("Index updated")

if __name__ == "__main__":
    main()
