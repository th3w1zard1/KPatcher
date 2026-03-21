$idxPath = "c:\GitHub\KPatcher\.cursor\hooks\state\continual-learning-index.json"
$idx = Get-Content $idxPath -Raw | ConvertFrom-Json
$map = @{}
foreach ($prop in $idx.transcripts.PSObject.Properties) {
    $map[$prop.Name] = $prop.Value.mtimeMs
}
$root = "C:\Users\boden\.cursor\projects\c-GitHub-KPatcher\agent-transcripts"
Get-ChildItem -Path $root -Recurse -Filter "*.jsonl" | ForEach-Object {
    $p = $_.FullName
    $ms = [DateTimeOffset]::new($_.LastWriteTimeUtc).ToUnixTimeMilliseconds()
    $old = $map[$p]
    if ($null -eq $old -or $ms -gt $old) {
        Write-Output "PROCESS|$ms|$p"
    }
}
