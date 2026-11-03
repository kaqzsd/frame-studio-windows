# 交给本机 Codex 部署 Windows 接入端

在 Windows 10/11 x64 原生运行，不用 WSL、不安装开机服务。模型通过官方客户端使用本机登录的 ChatGPT 订阅，没有把 GPT 模型权重下载到电脑。Windows 10 已实测；每台新电脑都要先自检，不能用另一台机器的报告替代。

## 给本机 Codex 的任务

> 下载并校验下方固定版本，安装独立环境、自检，然后打开 00-connect.cmd 让我填写网站的临时连接码和完成官方 ChatGPT 授权。使用我的订阅，不用 API Key，不读取个人 Codex 的登录或记忆。保留现有软件、系统代理和个人配置，不用 WSL、不注册开机服务。不要自行开始接单或消耗额度；连接检查完成后报告结果，由我选择启动。密码、连接码、token 都不要输出。

## 网站先做什么

1. 电脑主人登录 https://projects.zuichenji.cn/projects 。站主可在「用户权限」允许已有用户添加自己的电脑。
2. 打开「电脑接入与邀请」，输入 GPT 提供方电脑名，添加电脑。
3. 在这台电脑的安装向导生成 10 分钟临时连接码。只在电脑本机输入，不要发给朋友。
4. 接入后生成朋友邀请注册链接；朋友注册后，再在「批准使用用户」批准他使用这台电脑。

## 固定包下载、校验和解压

在 PowerShell 运行；只下载，不调用模型。

```powershell
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$framePackageDir = Join-Path $env:LOCALAPPDATA ('FrameStudioWorker\packages\setup-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $framePackageDir | Out-Null
$frameZip = Join-Path $framePackageDir 'frame-studio-windows-connect.zip'
Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/kaqzsd/frame-studio-windows/releases/download/windows-connect-2026.11.04-2/frame-studio-windows-connect.zip' -OutFile $frameZip
$frameExpected = 'c0a9a40ea142820742e1b1dedda24ab824b3c438cf0c97419a57490b95d420c0'
if ((Get-FileHash -LiteralPath $frameZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $frameExpected) { throw 'Checksum mismatch' }
Expand-Archive -LiteralPath $frameZip -DestinationPath $framePackageDir
Set-Location -LiteralPath $framePackageDir
```

必须解压完整 ZIP。测试机桌面的 Frame-Projects.cmd 是该机器的快捷入口，不能单独复制到朋友电脑。

## 安装和连接

1. 让本人右键 `01-install.cmd` → **以管理员身份运行**，使用当前 Windows 账户。安装器核包内清单、安装独立虚拟环境、复用或下载安装官方 Codex CLI 并离线自检。需要指定 Python 时执行 `Install.ps1 -PythonPath "完整 python.exe 路径"`。
2. 安装通过后，**普通双击** `00-connect.cmd`，本人输入网站临时连接码，并在官方页面授权本人 ChatGPT 订阅。不要使用管理员身份运行任务，也不要读取、复制个人 `.codex/auth.json`。
3. `04-check.cmd` 检查专用登录、网站身份及真实可用模型，不领取任务或调用模型。只检查 `check-report.json`、`windows-selftest-report.json` 的无秘密结果；不要发送 connection-secret.json、login 目录或整个数据目录。
4. 检查通过后，由本人选择开始接单。日常普通双击 `05-start.cmd`，网站出现 **ChatGPT 在线** 才可接收任务；只打开桌面 ChatGPT 不会接单。

接单窗口每 5 秒查询，关闭窗口或 Ctrl+C 停止。网络短暂中断可恢复；未知终态的任务必须先核对，不能删除 current.json 后重做。网站已撤销或清空旧设备时，00-connect.cmd 会提示重新配对；保留专用官方登录。

## 数据、验收和边界

- 每台电脑、用户、项目使用独立工作及记忆目录；不改个人 Codex 记忆。原生直接执行没有操作系统沙箱，任务能访问本机允许访问的文件，只邀请可信任的人。
- 账号设置通用于 AI、排单和协作子站。网站管理员不能通过网页/API读别人对话，但任务执行电脑需要处理明文内容，不是端到端加密。
- 主人明确授权后再做至多两轮小型真实订阅测试；07-subscription-test.cmd 是可选的本机测试入口，不使用正式网站队列。
- 报告实际系统、Python、Codex 版本，安装路径、自检、连接与模型列表结果。未运行真实任务则写“已接入，真实制作待验收”，不能称完整流程通过。
