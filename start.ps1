$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$bundleUrl = 'https://github.com/kaqzsd/frame-studio-windows/releases/download/windows-test-2026.11.03-1/frame-studio-windows.zip'
$expectedSha256 = 'fdf1826e061f3496a8a816b3f6a0d9471b14064289b843d4418fa1391befa2c8'
$version = 'windows-test-2026.11.03-1'
$folder = Join-Path $env:LOCALAPPDATA ('FrameStudioWorker\packages\' + $version + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$zip = Join-Path $folder 'frame-studio-windows.zip'
Write-Host 'Downloading the Windows-native test package from GitHub...'
Invoke-WebRequest -UseBasicParsing -Uri $bundleUrl -OutFile $zip
if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedSha256) {
    throw 'Package checksum mismatch. Nothing has been installed or executed.'
}
Expand-Archive -LiteralPath $zip -DestinationPath $folder
Set-Location -LiteralPath $folder
Write-Host 'Package verified. This uses native Windows, no WSL, no remote ports, no production website.'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $folder 'Install.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Installation or offline self-test failed. Keep the displayed report and stop here.' }
Write-Host 'Sign in to YOUR ChatGPT account in the official login page. Do not send anyone your password.'
& '.\.venv\Scripts\python.exe' -m project_worker.desktop login
if ($LASTEXITCODE -ne 0) { throw 'Login did not complete. No subscription test was started.' }
$answer = Read-Host 'Run TWO small subscription test rounds now? This consumes this account''s subscription allowance. [y/N]'
if ($answer -ne 'y' -and $answer -ne 'Y') {
    Write-Host "Stopped before model use. To test later: $folder\07-subscription-test.cmd"
    exit 0
}
& '.\.venv\Scripts\python.exe' -m pip install --disable-pip-version-check -r requirements-test.txt
if ($LASTEXITCODE -ne 0) { throw 'Test dependency installation failed. No model was called.' }
& '.\.venv\Scripts\python.exe' -m project_worker.local_demo --execute-subscription-test
$testResult = $LASTEXITCODE
Write-Host "Offline report: $folder\windows-selftest-report.json"
Write-Host 'Subscription report: see the exact path printed above. Share only the reports and delivery ZIPs, never the login folder.'
if ($testResult -ne 0) { throw 'Subscription test did not pass. Keep the report; do not repeatedly retry and spend allowance.' }
Write-Host 'Two-round local subscription test passed.'
