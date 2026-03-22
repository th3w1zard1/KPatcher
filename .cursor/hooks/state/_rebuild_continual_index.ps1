$root = "C:\Users\boden\.cursor\projects\c-GitHub-KPatcher\agent-transcripts"
$outPath = "C:\GitHub\KPatcher\.cursor\hooks\state\continual-learning-index.json"
$files = Get-ChildItem -Path $root -Recurse -Filter "*.jsonl" -File
$transcripts = @{}
$now = [DateTime]::UtcNow.ToString("o")
foreach ($f in $files) {
    $p = $f.FullName
    $ms = [int64]([DateTimeOffset]::new($f.LastWriteTimeUtc).ToUnixTimeMilliseconds())
    $transcripts[$p] = @{ mtimeMs = $ms; lastProcessedAt = $now }
}
$rootObj = @{ version = 1; transcripts = $transcripts }
$rootObj | ConvertTo-Json -Depth 8 | Set-Content -Path $outPath -Encoding UTF8
