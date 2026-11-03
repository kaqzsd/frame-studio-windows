$ErrorActionPreference='Stop'
$base=Join-Path $env:RUNNER_TEMP ('frame-update-check-'+[Guid]::NewGuid().ToString('N'))
$old=Join-Path $base 'old';$new=Join-Path $base 'new'
New-Item -ItemType Directory -Path (Join-Path $old 'project_worker'),(Join-Path $old '.venv'),(Join-Path $new 'project_worker') -Force | Out-Null
Copy-Item '.\FrameConnectionAssistant.exe' (Join-Path $old '帧间连接助手.exe')
Copy-Item '.\FrameConnectionAssistant.exe' (Join-Path $new '帧间连接助手.exe')
Copy-Item '.\src\Update.ps1' (Join-Path $new 'Update.ps1')
[IO.File]::WriteAllText((Join-Path $old 'project_worker\executor.py'),'OLD_SOURCE')
[IO.File]::WriteAllText((Join-Path $new 'project_worker\executor.py'),'NEW_SOURCE')
[IO.File]::WriteAllText((Join-Path $new 'project_worker\failures.py'),'NEW_MODULE')
[IO.File]::WriteAllText((Join-Path $old '.venv\keep.txt'),'KEEP_ENV')
[IO.File]::WriteAllText((Join-Path $old 'unrelated.txt'),'KEEP_USER_FILE')
function Manifest([string]$root,[string[]]$names){
    $files=[ordered]@{}
    foreach($name in $names){$files[$name]=(Get-FileHash (Join-Path $root $name) -Algorithm SHA256).Hash.ToLowerInvariant()}
    [IO.File]::WriteAllText((Join-Path $root 'manifest.json'),(@{files=$files}|ConvertTo-Json -Depth 4),[Text.UTF8Encoding]::new($false))
}
Manifest $old @('帧间连接助手.exe','project_worker/executor.py')
Manifest $new @('帧间连接助手.exe','project_worker/executor.py','project_worker/failures.py','Update.ps1')
& powershell.exe -NoProfile -NonInteractive -File (Join-Path $new 'Update.ps1') -TargetDirectory $old -NoLaunch -NoDialogs
if($LASTEXITCODE -ne 0){throw 'Valid update failed'}
if([IO.File]::ReadAllText((Join-Path $old 'project_worker\executor.py')) -ne 'NEW_SOURCE'){throw 'Source not updated'}
if([IO.File]::ReadAllText((Join-Path $old '.venv\keep.txt')) -ne 'KEEP_ENV' -or [IO.File]::ReadAllText((Join-Path $old 'unrelated.txt')) -ne 'KEEP_USER_FILE'){throw 'Unrelated data changed'}
$before=(Get-FileHash (Join-Path $old 'manifest.json')).Hash
Add-Content -LiteralPath (Join-Path $new 'project_worker\executor.py') -Value 'TAMPERED'
& powershell.exe -NoProfile -NonInteractive -File (Join-Path $new 'Update.ps1') -TargetDirectory $old -NoLaunch -NoDialogs
if($LASTEXITCODE -ne 1 -or (Get-FileHash (Join-Path $old 'manifest.json')).Hash -ne $before){throw 'Tampered package was not refused'}
[IO.File]::WriteAllText((Join-Path $new 'project_worker\executor.py'),'NEW_SOURCE')
$junction=Join-Path $base 'junction'
New-Item -ItemType Junction -Path $junction -Target $old | Out-Null
& powershell.exe -NoProfile -NonInteractive -File (Join-Path $new 'Update.ps1') -TargetDirectory $junction -NoLaunch -NoDialogs
if($LASTEXITCODE -ne 1 -or (Get-FileHash (Join-Path $old 'manifest.json')).Hash -ne $before){throw 'Junction target was not refused'}
Write-Output 'FRAME_UPDATE|{"result":"passed","environment_preserved":true,"unrelated_file_preserved":true,"tampering_refused":true,"junction_refused":true,"model_called":false}'
exit 0
