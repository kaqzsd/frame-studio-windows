# 交给本机 Codex 部署 Windows 制作端

本指南供能执行 Windows 终端命令、读写本机文件的 Codex 使用。只有聊天能力、没有本机工具权限的对话不能完成安装。这里安装的是任务执行程序；模型仍通过网络调用本人已登录的 ChatGPT 订阅，没有把 GPT 模型权重下载到电脑。

目前公开包是一行启动本机测试，以及分步连接正式站；不是已经实现的全自动一键生产安装器。本指南让本机 Codex 接手这些步骤。首次 ChatGPT 授权与私密连接码输入由电脑主人完成。

## 可直接复制给本机 Codex 的任务

> 请按 https://github.com/kaqzsd/frame-studio-windows/blob/main/DEPLOY-WITH-CODEX.md 在这台 Windows 上安装帧间制作端，并连接 https://projects.zuichenji.cn 。请实际执行下载校验、环境安装和自检，使用我本人的 ChatGPT 订阅登录，不使用 API Key。我授权使用原生 Windows 模式，不用 WSL、不安装开机服务；保留我的现有软件、个人 Codex 配置与记忆。需要网站专用连接码或官方登录授权时，由我在本机窗口完成，不要索取或输出密码、token 或登录文件。接入检查通过后报告结果；真实模型测试等我明确要求后再开始，不要自行消耗额度或接单。

## 网站管理员先完成的工作

1. 在正式协作站的「邀请与成员」添加一台新的制作电脑，例如“朋友的 Windows”。
2. 把这台电脑的专用连接码私下交给电脑主人，不能放进公开仓库、分享链接或聊天记录。不要复用站主/其他电脑的连接码。
3. 给提交需求的人生成邀请，选择这台制作电脑。每个人使用自己的协作站账号。已有成员的归属变更只影响以后新建的项目。

连接码不是网站管理员密码，也不是 ChatGPT 账号密码。本站只管理需求、状态与成果；本人 ChatGPT 登录保存在本机。

## 给执行部署的 Codex

### 先检查，不改个人环境

- Windows 10/11 原生运行。已有证据仅覆盖 Windows 10 x64；遇到 Windows 11、ARM64 或不同 CLI 版本，要在本机自检，不能引用其他机器的通过结果替代。
- 查找 Python 3.12 或更新版本与官方 Codex 可执行程序。不要读取个人 `.codex/auth.json`、记忆正文或聊天历史来“复用登录”。
- 优先使用已安装的兼容程序。依赖缺失时，从 Python/OpenAI 的官方来源或其官方包安装到当前用户的独立目录，不替换系统默认 Python、Node.js、桌面应用，也不修改 WindowsApps 权限。遵守本机工具的安装权限要求。
- 如果 `%LOCALAPPDATA%\FrameStudioWorker` 已存在，先检查是否已有连接或未结束任务；不要覆盖旧连接、删除 `current.json`、清理旧工作目录或切换已绑定账号。有冲突时说明冲突并保留现状。
- 本包按用户要求采用直接执行模式，独立目录不是系统沙箱，只应处理电脑主人信任的任务。

### 下载固定版本并核对

在 PowerShell 执行以下代码，只下载、验证和解压，不调用模型。不要改用未验证的 latest 包。

```powershell
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$framePackageDir = Join-Path $env:LOCALAPPDATA ('FrameStudioWorker\packages\codex-setup-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $framePackageDir | Out-Null
$frameZip = Join-Path $framePackageDir 'frame-studio-windows.zip'
Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/kaqzsd/frame-studio-windows/releases/download/windows-test-2026.11.03-2/frame-studio-windows.zip' -OutFile $frameZip
$frameExpected = '57fb61e2d12f69103a84e348b3c0f760a26ffccd4e710b4b67bf86f88ab926c5'
if ((Get-FileHash -LiteralPath $frameZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $frameExpected) {
    throw 'Package checksum mismatch. Stop without executing the package.'
}
Expand-Archive -LiteralPath $frameZip -DestinationPath $framePackageDir
Set-Location -LiteralPath $framePackageDir
```

不要把此前测试机桌面上的 `Frame-Projects.cmd` 单独复制到新电脑：它指向那台测试机的安装目录。新机器应使用本次实际的 `$framePackageDir`。

### 安装、自检、本人授权、连接

1. 运行 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1`。若使用独立 Python 路径，追加 `-PythonPath "完整的 python.exe 路径"`。安装器验证包内清单、创建虚拟环境并做离线自检；失败就诊断，不跳过或修改校验。
2. 离线报告 `windows-selftest-report.json` 应显示通过。此步骤不需要网站连接码、不调用模型。
3. 使用找到的官方 `codex.exe` 发起专用登录：

   ```powershell
   .\.venv\Scripts\python.exe -m project_worker.desktop login --device-auth --codex-path '实际官方 codex.exe 完整路径'
   ```

   保持进程运行，让主人在官方页面亲自完成授权。不要代填密码，不要复制个人应用登录文件。登录完成会写入本机专用目录；只依据退出码和程序的成功提示检查，不输出登录文件。
4. 为主人打开解压目录中的 `02-configure.cmd`。由主人填写网站地址 `https://projects.zuichenji.cn`、管理员私下提供的专用连接码和实际 `codex.exe` 路径。连接码使用隐藏输入；不要把它放在命令行参数、终端记录或模型聊天中。
5. 主人完成后，运行以下检查，不会领取任务或调用模型：

   ```powershell
   .\.venv\Scripts\python.exe -m project_worker.desktop check
   ```

6. 读取无凭据的 `check-report.json`，确认 `result=passed`、`site_protocol=2`、正确的制作电脑 ID、`subscription_login_present=true`、`model_called=false`。不要发送 `connection-secret.json`、`login/` 或整个数据目录。

这个固定版本 ZIP 在正式站发布前构建，安装末尾“网站尚待发布”的提示属于历史文字；正式站现已发布。是否可以接入以第 5 步的真实身份检查为准，不要因为旧提示跳过检查或更改源码。

### 保留手动入口，不自动启动

可创建指向本次实际安装目录中 `05-start.cmd` 的桌面快捷方式，命名“帧间制作端”。不要指向别的机器的目录，也不要把连接码写进快捷方式。

默认 `05-start.cmd` 每 30 秒查询，最多查询 10 次，完成一个项目轮次即退出。需要在同一次运行中接续反馈修改时，由主人明确启动：

```powershell
.\.venv\Scripts\python.exe -m project_worker.desktop run --max-jobs 10 --max-polls 120
```

最多查询 120 次或处理 10 个轮次，任务执行时间另计；不是永久服务。电脑应保持开机联网。不要注册服务、计划任务、开机项或偷偷启动后台常驻进程。

### 主人授权后再做真实验收

连接检查通过不等于模型任务已验证。只有主人明确同意消耗订阅时，才启动最多两轮执行；让一个分配到本机的获邀网站账号提交小型自包含项目，然后完成 V1 下载、网页反馈、V2 下载与验收。

首次建议用 `run --max-jobs 2 --max-polls 20`，单机同时只运行一个控制器。若启动命令通过远程工具执行，工具超时必须覆盖轮询和任务执行时间；不要因为工具断开而直接重启模型任务。先检查原进程和本机领取记录。

最终报告：实际系统/Python/Codex 版本、安装目录、离线自检、连接检查、手动启动入口。未运行真实任务就明确写“已接入，真实制作待验收”；只有实际网页完成两轮后才写“完整流程通过”。不要把测试机已有成果当作这台新电脑的验证结果。
