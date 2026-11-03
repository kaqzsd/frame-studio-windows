# 帧间 Windows 原生制作端 · 测试包

在 Windows 本机使用本人 ChatGPT 订阅，完成需求制作、文件交付和反馈修改。原生调用 Codex，不使用 WSL，不开放远程端口。

## 一行启动本机测试

在 Windows **PowerShell** 中粘贴以下命令。不需要管理员权限，不要求与另一台电脑处于同一网段。

```powershell
$p=Join-Path $env:TEMP ("frame-test-"+[guid]::NewGuid()+".ps1"); iwr -UseBasicParsing "https://github.com/kaqzsd/frame-studio-windows/releases/download/windows-test-2026.11.03-2/start.ps1" -OutFile $p; powershell -NoProfile -ExecutionPolicy Bypass -File $p
```

需要 Python 3.12 或更新版本，以及官方 Codex CLI。也可尝试桌面应用内附 codex.exe；如果 Windows 拒绝从终端启动，请使用独立 CLI，不要修改 WindowsApps 权限。缺少 Python 时按提示安装后重新执行；脚本不会替换系统软件。

依次下载并核对 ZIP、建立本地 Python 环境、离线自检、本人 ChatGPT 登录。最后明确询问是否进行两轮订阅测试；回答 `y` 后才调用模型，使用该账号的订阅额度。测试采用进程内的临时网站副本，不访问正式网站，不需要网站连接码。

[下载 ZIP](https://github.com/kaqzsd/frame-studio-windows/releases/download/windows-test-2026.11.03-2/frame-studio-windows.zip)，也可手动解压，依次运行 `01-install.cmd`、`03-login.cmd`、`07-subscription-test.cmd`。

## 测试结果

- 离线报告：下载包目录里的 `windows-selftest-report.json`。
- 订阅报告：窗口显示的目录里的 `subscription-test-report.json`。
- 两轮成果：同一目录内 `delivery-v1.zip`、`delivery-v2.zip`。

只分享上述报告和测试成果。不要上传 `login/`、`connection-secret.json`、整个本地数据目录或未经核对的日志。

## 边界与状态

使用独立工作目录和 Codex 状态，关闭个人记忆注入、全局技能及应用工具。**原生直接执行没有系统沙箱，不能阻止任务程序访问当前 Windows 用户有权访问的其他文件。** 请只处理可信的任务。

软件只接受 ChatGPT 登录，不配置 API Key 后备；额度不足、登录过期或协议不兼容时停止。两轮测试最多每轮 5 分钟，不创建开机服务或永久后台任务。正式协作站现已支持独立制作电脑；接入时使用管理员单独分配的连接码。

2026-11-03：Windows 10 真机安装、中文路径、文件锁、连续三轮超时子进程清理、官方独立 CLI 0.166.0 配置检查和两轮模拟网站闭环已通过；修复后的完整 ZIP 也已重新安装并通过离线自检。开发机 120 项回归及额外的 Mac 系统隔离检查通过。Windows 10 已通过官方设备授权完成两轮真实 ChatGPT 订阅测试：第一版交付、反馈后第二版交付、内容与哈希校验、项目记忆延续、跨执行端任务范围及最终验收全部通过。两个任务进程均已退出，无待处理领取记录或临时登录文件，专用登录主文件保持不变。Windows 11 尚未真机测试。

ZIP SHA-256：`57fb61e2d12f69103a84e348b3c0f760a26ffccd4e710b4b67bf86f88ab926c5`。启动器核对这个值，安装器再次验证包内文件清单。仓库和发布包不含账号凭据、个人记忆、生产数据或真实连接码。

[官方 Codex CLI 文档](https://learn.chatgpt.com/docs/cli) · [官方 app-server 文档](https://learn.chatgpt.com/docs/app-server)

本版修复：Windows Python 启动竞态与子进程退出等待、数据库初始结构漏打包、多版本 Python 选择，以及 SSH 设备码登录。可用 `Install.ps1 -PythonPath "完整的 python.exe 路径"` 指定独立 Python；无须替换系统默认版本。

本版 ZIP 在真实订阅联调前构建，包内说明记录了当时的验证状态；上述真实联调使用的就是同一 SHA-256 的 ZIP，最新结果以本页面为准。最初测试使用本机网站副本；2026-11-03 已继续完成正式网站的真实两轮验收，下面记录最新接入方式。


## 正式网站接入 · 2026-11-03

[项目协作站](https://projects.zuichenji.cn)已发布独立制作电脑功能。Windows 10 真机完成了网页邀请注册、上传中文附件、提交需求、自动领取制作、下载 V1、网页反馈、自动回传 V2 和网页验收。两版内容与 SHA-256 一致；项目记忆延续、临时登录清理、任务进程退出均通过。另验证了其他成员无法读取项目和成果、普通成员无法访问管理接口、制作电脑无法领取另一电脑的任务。临时权限测试对象已停用。

新电脑仍先完成上方安装、自检和本人订阅登录。管理员在「邀请与成员」添加制作电脑，私下交付专用连接码，并将朋友的邀请分配给这台电脑。然后在解压目录依次运行：

1. `02-configure.cmd`：填写协作站地址、专用连接码和官方 Codex 路径。
2. `04-check.cmd`：检查账号登录和任务范围，不调用模型。
3. `05-start.cmd`：每 30 秒查询，最多查询 10 次、处理一个项目轮次。

需要在一次手动运行中接续制作和反馈，可在包目录的 PowerShell 执行：

```powershell
.\.venv\Scripts\python.exe -m project_worker.desktop run --max-jobs 10 --max-polls 120
```

这是有限轮询：最多查询 120 次、处理 10 个轮次，任务执行时间另计。电脑需开机联网；停止或达到上限后网页需求会保留排队，重新启动即可继续。不会安装开机服务。已完成的正式验收只调用了两轮 ChatGPT 订阅，没有 API Key 后备或长期任务。

原 ZIP 和启动器保持不变，包内“正式站未发布”是构建时的历史说明，以本页为准。不要把任何人的网站连接码、ChatGPT 登录或私人项目打包上传。
