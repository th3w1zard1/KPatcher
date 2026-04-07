#!/usr/bin/env python3
import json

from datetime import datetime
from pathlib import Path

index_file = Path(r"C:\GitHub\KPatcher\.cursor\hooks\state\continual-learning-index.json")
transcript_dir = Path(r"C:\Users\boden\.cursor\projects\c-GitHub-KPatcher\agent-transcripts")

# Load existing index
index_data = {"version": 1, "transcripts": {}}
if index_file.exists():
    with open(index_file, "r", encoding="utf-8") as f:
        index_data = json.load(f)

# Find all transcript files and update mtimes
all_files = list(transcript_dir.rglob("*.jsonl"))
for tf in all_files:
    full_path = str(tf.resolve())
    mtime_ms = int(tf.stat().st_mtime * 1000)
    index_data["transcripts"][full_path] = {
        "mtimeMs": mtime_ms,
        "lastProcessedAt": datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%S.000Z"),
    }

# Remove entries for files that no longer exist
existing_paths = {str(Path(p).resolve()) for p in all_files}
to_remove = [p for p in index_data["transcripts"].keys() if p not in existing_paths]
for p in to_remove:
    del index_data["transcripts"][p]

# Write back
index_file.parent.mkdir(parents=True, exist_ok=True)
with open(index_file, "w", encoding="utf-8") as f:
    json.dump(index_data, f, indent=2)

print(f"Updated index: {len(all_files)} files, removed {len(to_remove)} deleted entries")
