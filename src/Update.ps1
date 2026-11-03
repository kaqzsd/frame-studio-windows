param([string]$TargetDirectory, [switch]$NoLaunch, [switch]$NoDialogs)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.UTF8Encoding]::new()
function Assert-OrdinaryPath([string]$Path) {
    $item=[IO.Path]::GetFullPath($Path)
    while($item){
        if((Test-Path -LiteralPath $item) -and ((Get-Item -LiteralPath $item -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw '更新路径不能包含链接或目录联接。'}
        $parent=[IO.Path]::GetDirectoryName($item)
        if($parent -eq $item){break};$item=$parent
    }
}
function Safe-Path([string]$Base,[string]$Relative){
    if([IO.Path]::IsPathRooted($Relative) -or $Relative.Contains('..')){throw '安装包路径无效。'}
    $path=[IO.Path]::GetFullPath((Join-Path $Base $Relative))
    if(-not $path.StartsWith($Base.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw '安装包路径越界。'}
    Assert-OrdinaryPath $path
    return $path
}
$backup=$null;$mutated=$false;$restored=$false
try{
    $source=[IO.Path]::GetFullPath($PSScriptRoot)
    Assert-OrdinaryPath $source
    $manifest=Get-Content -LiteralPath (Join-Path $source 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($entry in $manifest.files.PSObject.Properties){
        $path=Safe-Path $source $entry.Name
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Value){throw '新安装包校验未通过，请重新下载完整包。'}
    }
    if(-not $TargetDirectory){
        $link=Join-Path ([Environment]::GetFolderPath('Desktop')) '帧间连接助手.lnk'
        if(Test-Path -LiteralPath $link){
            $shell=New-Object -ComObject WScript.Shell
            $TargetDirectory=Split-Path -Parent ($shell.CreateShortcut($link).TargetPath)
        }
    }
    if(-not $TargetDirectory){
        if($NoDialogs){throw '未找到旧版助手目录。'}
        Add-Type -AssemblyName System.Windows.Forms
        $picker=New-Object System.Windows.Forms.OpenFileDialog
        $picker.Title='选择原来使用的帧间连接助手.exe';$picker.Filter='连接助手|帧间连接助手.exe'
        if($picker.ShowDialog() -ne 'OK'){exit 2}
        $TargetDirectory=Split-Path -Parent $picker.FileName
    }
    $target=[IO.Path]::GetFullPath($TargetDirectory).TrimEnd('\')
    Assert-OrdinaryPath $target
    if($source.TrimEnd('\') -eq $target){throw '请将新版解压到另一个目录，再运行更新入口。'}
    $oldExe=Join-Path $target '帧间连接助手.exe'
    if(-not (Test-Path -LiteralPath $oldExe)){throw '所选目录没有旧版连接助手。'}
    if(@(Get-Process -Name '帧间连接助手' -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $oldExe}).Count){throw '请先关闭旧版连接助手，再更新。'}
    $old=Get-Content -LiteralPath (Join-Path $target 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($entry in $old.files.PSObject.Properties){
        if(-not $manifest.files.PSObject.Properties[$entry.Name]){throw '旧版包含未知文件，未执行更新。'}
        $path=Safe-Path $target $entry.Name
        if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Value){throw '旧版程序文件已改变，未执行更新。'}
    }
    foreach($entry in $manifest.files.PSObject.Properties){
        $path=Safe-Path $target $entry.Name
        if((Test-Path -LiteralPath $path) -and -not $old.files.PSObject.Properties[$entry.Name]){throw '新增文件名与本地文件冲突，未执行更新。'}
    }
    $backupRoot=Join-Path $env:LOCALAPPDATA 'FrameStudioInstallerBackups'
    Assert-OrdinaryPath $backupRoot
    $backup=Join-Path $backupRoot ('program-'+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $names=@($old.files.PSObject.Properties.Name)+@('manifest.json')
    foreach($name in $names){
        $from=Safe-Path $target $name;$to=Join-Path $backup $name;$restore=Join-Path (Join-Path $backup 'restored') $name
        New-Item -ItemType Directory -Path (Split-Path -Parent $to) -Force | Out-Null
        New-Item -ItemType Directory -Path (Split-Path -Parent $restore) -Force | Out-Null
        Copy-Item -LiteralPath $from -Destination $to
        Copy-Item -LiteralPath $to -Destination $restore
        if((Get-FileHash -LiteralPath $from).Hash -ne (Get-FileHash -LiteralPath $restore).Hash){throw '旧程序备份恢复校验失败，未执行更新。'}
    }
    $mutated=$true
    foreach($entry in $manifest.files.PSObject.Properties){
        $to=Safe-Path $target $entry.Name
        New-Item -ItemType Directory -Path (Split-Path -Parent $to) -Force | Out-Null
        Copy-Item -LiteralPath (Safe-Path $source $entry.Name) -Destination $to -Force
        if((Get-FileHash -LiteralPath $to -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Value){throw '更新文件校验失败。'}
    }
    Copy-Item -LiteralPath (Join-Path $source 'manifest.json') -Destination (Join-Path $target 'manifest.json') -Force
    Write-Output '更新完成：程序文件已校验，原运行环境、配对、登录和项目数据保持。'
    if(-not $NoLaunch){Start-Process -FilePath $oldExe -WorkingDirectory $target}
}catch{
    if($mutated -and $backup){
        try{
            foreach($name in $names){Copy-Item -LiteralPath (Join-Path $backup $name) -Destination (Join-Path $target $name) -Force}
            $restored=$true
        }catch{}
    }
    $message=if(-not $mutated){[string]$_.Exception.Message}elseif($restored){'更新未完成，已恢复旧版程序。备份保留在 FrameStudioInstallerBackups。'}else{'更新未完成，请先不要启动助手。旧程序备份保留在 FrameStudioInstallerBackups，需要恢复后再使用。'}
    Write-Output $message
    if(-not $NoDialogs){Add-Type -AssemblyName System.Windows.Forms;[Windows.Forms.MessageBox]::Show($message,'更新连接助手') | Out-Null}
    exit 1
}
