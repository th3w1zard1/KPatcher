$root = "C:\Users\boden\.cursor\projects\c-GitHub-KPatcher\agent-transcripts"
$now = [DateTimeOffset]::UtcNow.ToString("o")
$transcripts = @{}
Get-ChildItem -Path $root -Recurse -Filter "*.jsonl" | ForEach-Object {
    $ms = [DateTimeOffset]::new($_.LastWriteTimeUtc).ToUnixTimeMilliseconds()
    $transcripts[$_.FullName] = @{
        mtimeMs         = $ms
        lastProcessedAt = $now
    }
}
$sorted = [ordered]@{}
foreach ($k in ($transcripts.Keys | Sort-Object)) {
    $sorted[$k] = $transcripts[$k]
}
[ordered]@{
    version     = 1
    transcripts = $sorted
} | ConvertTo-Json -Depth 5 | Set-Content -Path "c:\GitHub\KPatcher\.cursor\hooks\state\continual-learning-index.json" -Encoding utf8
Write-Output ("Wrote $($transcripts.Count) entries")
