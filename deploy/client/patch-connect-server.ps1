param(
    [Parameter(Mandatory = $true)]
    [string] $ClientPath,

    [string] $ConnectServer = "play.muorigins.online"
)

$ErrorActionPreference = "Stop"

$launcherPath = Join-Path $ClientPath "Mu.exe"
$backupPath = Join-Path $ClientPath "Mu.exe.pre-origins.bak"
$originalConnectServer = "connect.muonline.webzen.net"

if (-not (Test-Path -LiteralPath $launcherPath -PathType Leaf)) {
    throw "Launcher not found: $launcherPath"
}

if ($ConnectServer.Length -gt $originalConnectServer.Length) {
    throw "Connect-server name must not exceed $($originalConnectServer.Length) ASCII characters."
}

$bytes = [System.IO.File]::ReadAllBytes($launcherPath)
$oldBytes = [System.Text.Encoding]::ASCII.GetBytes($originalConnectServer)
$newBytes = [System.Text.Encoding]::ASCII.GetBytes($ConnectServer)
$matches = [System.Collections.Generic.List[int]]::new()

for ($offset = 0; $offset -le $bytes.Length - $oldBytes.Length; $offset++) {
    $matchesHere = $true
    for ($index = 0; $index -lt $oldBytes.Length; $index++) {
        if ($bytes[$offset + $index] -ne $oldBytes[$index]) {
            $matchesHere = $false
            break
        }
    }

    if ($matchesHere) {
        $matches.Add($offset)
    }
}

if ($matches.Count -ne 1) {
    throw "Expected one '$originalConnectServer' entry in Mu.exe, found $($matches.Count). No changes were written."
}

if (-not (Test-Path -LiteralPath $backupPath)) {
    Copy-Item -LiteralPath $launcherPath -Destination $backupPath
}

$matchOffset = $matches[0]
[Array]::Clear($bytes, $matchOffset, $oldBytes.Length)
[Array]::Copy($newBytes, 0, $bytes, $matchOffset, $newBytes.Length)
[System.IO.File]::WriteAllBytes($launcherPath, $bytes)

$patchedText = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($launcherPath))
if ($patchedText.IndexOf($ConnectServer, [System.StringComparison]::Ordinal) -lt 0) {
    throw "Verification failed after patching $launcherPath. Restore $backupPath before use."
}

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $launcherPath).Hash
Write-Output "Patched launcher: $launcherPath"
Write-Output "Backup: $backupPath"
Write-Output "Connect server: $ConnectServer"
Write-Output "SHA256: $hash"
