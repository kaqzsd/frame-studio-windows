$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new()
$source=Join-Path $PWD 'src\ConnectionAssistant.cs'
$exe=Join-Path $PWD 'FrameConnectionAssistant.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll "/out:$exe" $source
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
foreach($arguments in @('--layout-check','--shutdown-check run bounded','--shutdown-check check immediate','--shutdown-check install immediate','--shutdown-check run bounded blocked','--shutdown-check run force')){
    $process=Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
    if(-not $process.WaitForExit(30000)){$process.Kill();throw ('GUI check timed out: '+$arguments)}
    if($process.ExitCode -ne 0){
        Get-ChildItem 'shutdown-*.json' | ForEach-Object {Get-Content $_.FullName -Raw -Encoding UTF8}
        throw ('GUI check failed: '+$arguments+' exit='+$process.ExitCode)
    }
}
$reports=@(Get-ChildItem 'shutdown-*.json' | ForEach-Object {Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json})
if($reports.Count -ne 5 -or @($reports | Where-Object {$_.result -ne 'passed'}).Count){throw 'Missing shutdown reports'}
Write-Output ('FRAME_REPORT|'+(@{source_sha256=(Get-FileHash $source -Algorithm SHA256).Hash.ToLowerInvariant();exe_sha256=(Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant();tests=$reports} | ConvertTo-Json -Depth 8 -Compress))
function Emit-PublicArtifact([string]$name,[string]$path){
    $encoded=[Convert]::ToBase64String([IO.File]::ReadAllBytes($path))
    for($i=0;$i -lt $encoded.Length;$i+=4096){Write-Output ('FRAME_'+$name+'|'+$encoded.Substring($i,[Math]::Min(4096,$encoded.Length-$i)))}
}
Emit-PublicArtifact 'BINARY' $exe
Emit-PublicArtifact 'PREVIEW' (Join-Path $PWD 'gui-preview.png')
