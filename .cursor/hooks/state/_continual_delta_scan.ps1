$root = "C:\Users\boden\.cursor\projects\c-GitHub-KPatcher\agent-transcripts"
$idxPath = "C:\GitHub\KPatcher\.cursor\hooks\state\continual-learning-index.json"
$idx = Get-Content $idxPath -Raw | ConvertFrom-Json
$map = @{}
foreach ($p in $idx.transcripts.PSObject.Properties) {
    $map[$p.Name] = $p.Value.mtimeMs
}
$files = Get-ChildItem -Path $root -Recurse -Filter "*.jsonl" -File
$toRead = New-Object System.Collections.Generic.List[object]
foreach ($f in $files) {
    $p = $f.FullName
    $ms = [int64]([DateTimeOffset]::new($f.LastWriteTimeUtc).ToUnixTimeMilliseconds())
    $old = $null
    if ($map.ContainsKey($p)) { $old = $map[$p] }
    if ($null -eq $old -or $ms -gt $old) {
        $toRead.Add([PSCustomObject]@{ Path = $p; mtimeMs = $ms; Reason = $(if ($null -eq $old) { "new" } else { "stale" }) })
    }
}
$toRead | Sort-Object Path | ConvertTo-Json -Depth 3
