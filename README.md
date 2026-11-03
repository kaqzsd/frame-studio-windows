# 帧间 Windows 原生制作端 · 测试包

在 Windows 本机使用本人 ChatGPT 订阅，完成需求制作、文件交付和反馈修改。原生调用 Codex，不使用 WSL，不开放远程端口。

## 一行启动本机测试

在 Windows **PowerShell** 中粘贴以下命令。不需要管理员权限，不要求与另一台电脑处于同一网段。

```powershell
$p=Join-Path $env:TEMP ("frame-test-"+[guid]::NewGuid()+".ps1"); iwr -UseBasicParsing "https://github.com/kaqzsd/frame-studio-windows/releases/download/windows-test-2026.11.03-1/start.ps1" -OutFile $p; powershell -NoProfile -ExecutionPolicy Bypass -File $p
```

需要 Python 3.12 或更新版本，以及官方 Codex CLI 或带有 codex.exe 的官方桌面应用。缺少 Python 时按提示安装后重新执行；脚本不会替换系统软件。

依次下载并核对 ZIP、建立本地 Python 环境、离线自检、本人 ChatGPT 登录。最后明确询问是否进行两轮订阅测试；回答 `y` 后才调用模型，使用该账号的订阅额度。测试采用进程内的临时网站副本，不访问正式网站，不需要网站连接码。

[下载 ZIP](https://github.com/kaqzsd/frame-studio-windows/releases/download/windows-test-2026.11.03-1/frame-studio-windows.zip)，也可手动解压，依次运行 `01-install.cmd`、`03-login.cmd`、`07-subscription-test.cmd`。

## 测试结果

- 离线报告：下载包目录里的 `windows-selftest-report.json`。
- 订阅报告：窗口显示的目录里的 `subscription-test-report.json`。
- 两轮成果：同一目录内 `delivery-v1.zip`、`delivery-v2.zip`。

只分享上述报告和测试成果。不要上传 `login/`、`connection-secret.json`、整个本地数据目录或未经核对的日志。

## 边界与状态

使用独立工作目录和 Codex 状态，关闭个人记忆注入、全局技能及应用工具。**原生直接执行没有系统沙箱，不能阻止任务程序访问当前 Windows 用户有权访问的其他文件。** 请只处理可信的任务。

软件只接受 ChatGPT 登录，不配置 API Key 后备；额度不足、登录过期或协议不兼容时停止。两轮测试最多每轮 5 分钟，不创建开机服务或永久后台任务。普通网站接入在管理员发布新版网站并分配专用连接码后另行进行。

开发机 macOS 的 116 项回归、4 个浏览器场景，以及解压包两轮模拟闭环通过。Windows 真机安装、登录、Job Object 进程退出和真实订阅执行仍待操作者回传报告；没有把 Mac 测试写成 Windows 已验证。

ZIP SHA-256：`fdf1826e061f3496a8a816b3f6a0d9471b14064289b843d4418fa1391befa2c8`。启动器核对这个值，安装器再次验证包内文件清单。仓库和发布包不含账号凭据、个人记忆、生产数据或真实连接码。

[官方 Codex CLI 文档](https://learn.chatgpt.com/docs/cli) · [官方 app-server 文档](https://learn.chatgpt.com/docs/app-server)
