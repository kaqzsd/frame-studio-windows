# 让自己的 Codex 帮忙接入 Windows

把下面这段需求交给你电脑上的 Codex。首次官方登录由本人完成，不要把网站连接码、ChatGPT 密码或 auth.json 发到群聊、GitHub 或其他人的聊天。

> 请帮我把这台 Windows 10/11 连接到 https://projects.zuichenji.cn 的项目协作网站。
>
> 使用 https://github.com/kaqzsd/frame-studio-windows/releases/tag/windows-connect-2026.11.03-3 的 frame-studio-windows-connect.zip 和 SHA256SUMS.txt。先核对下载 SHA256，再解压到独立目录，检查 README.txt，并运行 00-connect.cmd。
>
> 不使用 WSL，不修改我原来的 ChatGPT/Codex 记忆、系统代理或默认 Python，不安装开机服务，不配置 API Key。需要依赖时仅使用包中声明的官方来源。Windows 为直接执行模式。
>
> 我会在网站「我的电脑与朋友」添加自己的电脑、生成临时电脑连接码，然后在本机向导中粘贴。OpenAI 官方订阅登录需要我本人操作时，请停在登录步骤让我完成，不读取或发送登录文件。
>
> 用 04-check.cmd 检查连接，确认网站显示正确电脑；检查本身不调用模型。准备接单时运行 05-start.cmd。告诉我如何停止和下次启动。不要自动领取不属于我的电脑的任务，不扩大网站账号权限。

如果网站还没有「我的电脑与朋友」的接入权限，请先让站主在「协作成员」开通，或接受带“允许接入自己的电脑并邀请朋友”的邀请。

已安装但显示离线时通常只需启动接单窗口，不需要重新配置或重装 GPT。
