#!/usr/bin/env python3
"""Process transcripts incrementally for continual learning."""
import json
import os
from pathlib import Path
from datetime import datetime

transcript_dir = Path(r"C:\Users\boden\.cursor\projects\c-GitHub-KPatcher\agent-transcripts")
index_file = Path(r"C:\GitHub\KPatcher\.cursor\hooks\state\continual-learning-index.json")

# Load existing index
index_data = {"version": 1, "transcripts": {}}
if index_file.exists():
    with open(index_file, 'r', encoding='utf-8') as f:
        index_data = json.load(f)

# Find all transcript files
transcript_files = list(transcript_dir.rglob("*.jsonl"))

# Determine which files need processing
files_to_process = []
for tf in transcript_files:
    full_path = str(tf.resolve())
    mtime_ms = int(tf.stat().st_mtime * 1000)
    
    if full_path not in index_data["transcripts"]:
        files_to_process.append((full_path, mtime_ms))
    else:
        indexed_mtime = index_data["transcripts"][full_path].get("mtimeMs", 0)
        if mtime_ms > indexed_mtime:
            files_to_process.append((full_path, mtime_ms))

# Output results
result = {
    "total_files": len(transcript_files),
    "files_to_process": len(files_to_process),
    "files": [{"path": p, "mtimeMs": m} for p, m in files_to_process]
}

print(json.dumps(result, indent=2))
