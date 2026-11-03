// Native Windows Forms UI. Build with the Windows .NET Framework compiler; no WebView or WSL.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace FrameStudio {
    public sealed class ChildLifetime : IDisposable {
        [StructLayout(LayoutKind.Sequential)] struct Basic {
            public long ProcessTime, JobTime; public uint Flags;
            public UIntPtr Min, Max; public uint Limit; public UIntPtr Affinity; public uint Priority, Schedule;
        }
        [StructLayout(LayoutKind.Sequential)] struct Io { public ulong A,B,C,D,E,F; }
        [StructLayout(LayoutKind.Sequential)] struct Extended {
            public Basic Basic; public Io Io; public UIntPtr ProcessMemory, JobMemory, PeakProcess, PeakJob;
        }
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr a, string n);
        [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr j, int c, IntPtr p, uint s);
        [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr j, IntPtr p);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr p);
        [DllImport("kernel32.dll")] static extern bool TerminateJobObject(IntPtr j, uint exitCode);
        IntPtr handle;
        public ChildLifetime(Process process) {
            handle = CreateJobObject(IntPtr.Zero, null);
            Extended x = new Extended(); x.Basic.Flags = 0x2000;
            int size = Marshal.SizeOf(x); IntPtr ptr = Marshal.AllocHGlobal(size);
            try {
                Marshal.StructureToPtr(x, ptr, false);
                if (handle == IntPtr.Zero || !SetInformationJobObject(handle, 9, ptr, (uint)size) ||
                    !AssignProcessToJobObject(handle, process.Handle)) throw new IOException("无法管理后台进程");
            } catch { Dispose(); try { process.Kill(); } catch {} throw; }
            finally { Marshal.FreeHGlobal(ptr); }
        }
        public void Dispose() { IntPtr owned = Interlocked.Exchange(ref handle, IntPtr.Zero); if (owned != IntPtr.Zero) { try { TerminateJobObject(owned, 1); } finally { CloseHandle(owned); } } }
    }

    public sealed class Assistant : Form {
        readonly string root = AppDomain.CurrentDomain.BaseDirectory;
        readonly JavaScriptSerializer json = new JavaScriptSerializer();
        readonly Color Ink = Color.FromArgb(34,48,41), Green = Color.FromArgb(59,91,67);
        readonly TextBox nameBox = new TextBox(), codeBox = new TextBox(), siteBox = new TextBox();
        readonly TextBox logBox = new TextBox(), authCode = new TextBox();
        readonly Label state = new Label(), detail = new Label(), modelLabel = new Label();
        readonly Button connect = new Button(), login = new Button(), check = new Button(), start = new Button(), stop = new Button(), force = new Button();
        readonly Button install = new Button(), selftest = new Button(), website = new Button(), authOpen = new Button(), authCopy = new Button();
        readonly Panel authPanel = new Panel();
        readonly ProgressBar progress = new ProgressBar();
        readonly CheckBox showCode = new CheckBox(), editSite = new CheckBox();
        Process child; ChildLifetime lifetime; StreamWriter input; string operation = ""; bool closeWhenDone, chainConnect, operationOk;
        string initialAction; int generation; bool verified, needPython, stopRequested, forceStopping;
        DialogResult? shutdownTestChoice;
        readonly System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer();
        DateTime closeDeadline;
        public Assistant(string action, bool preview = false) {
            initialAction = action;
            Text = "帧间 · ChatGPT 连接助手"; Name = "FrameConnectionAssistant";
            StartPosition = FormStartPosition.CenterScreen; MinimumSize = new Size(760,650);
            Size = new Size(900,Math.Min(820,Screen.PrimaryScreen.WorkingArea.Height-30));
            AutoScaleMode = AutoScaleMode.Dpi; Font = new Font("Microsoft YaHei UI", 10F);
            BackColor = Color.FromArgb(245,246,241); ForeColor = Ink;
            var page = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=8, Padding=new Padding(26), AutoScroll=true };
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 102));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 49));
            page.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            Controls.Add(page);
            var title = new Label { Text="连接你的 ChatGPT，和朋友一起完成想法。", Dock=DockStyle.Fill,
                Font=new Font(Font.FontFamily,19,FontStyle.Bold), TextAlign=ContentAlignment.MiddleLeft };
            page.Controls.Add(title,0,0);
            var statusPanel = new Panel { Dock=DockStyle.Fill, BackColor=Color.FromArgb(232,238,225), Padding=new Padding(10) };
            state.Text="欢迎使用"; state.Name="连接状态"; state.Dock=DockStyle.Top; state.Height=29;
            state.Font=new Font(Font.FontFamily,13,FontStyle.Bold);
            detail.Text="先填写电脑名和网站生成的连接码。已有专用登录会自动复用。";
            detail.Dock=DockStyle.Fill; detail.TextAlign=ContentAlignment.MiddleLeft;
            statusPanel.Controls.Add(detail); statusPanel.Controls.Add(state); page.Controls.Add(statusPanel,0,1);
            var fields = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=4, Padding=new Padding(0,14,0,0) };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,118)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            for(int i=0;i<4;i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
            AddField(fields,0,"电脑名",nameBox,"GPT 提供方电脑名"); nameBox.MaxLength=80; nameBox.Text=Environment.MachineName;
            AddField(fields,1,"电脑连接码",codeBox,"电脑连接码"); codeBox.MaxLength=100; codeBox.UseSystemPasswordChar=true;
            AddField(fields,2,"网站地址",siteBox,"网站地址"); siteBox.Text="https://projects.zuichenji.cn"; siteBox.ReadOnly=true;
            var options = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
            showCode.Text="显示连接码"; showCode.AutoSize=true; showCode.CheckedChanged += delegate { codeBox.UseSystemPasswordChar=!showCode.Checked; };
            editSite.Text="修改网站地址"; editSite.AutoSize=true; editSite.CheckedChanged += delegate { siteBox.ReadOnly=!editSite.Checked; };
            options.Controls.Add(showCode); options.Controls.Add(editSite); fields.Controls.Add(options,1,3); page.Controls.Add(fields,0,2);
            var actions = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
            ButtonStyle(connect,"连接网站",true); ButtonStyle(login,"登录 ChatGPT",false); ButtonStyle(check,"检查连接",false);
            ButtonStyle(website,"打开协作网站",false);
            actions.Controls.AddRange(new Control[]{connect,login,check,website}); page.Controls.Add(actions,0,3);
            connect.Click += delegate { ConnectNow(); }; login.Click += delegate { StartBridge("login",null); };
            check.Click += delegate { StartBridge("check",null); }; website.Click += delegate { OpenWebsite(); };
            authPanel.Dock=DockStyle.Fill; authPanel.BackColor=Color.FromArgb(255,249,229); authPanel.Padding=new Padding(12); authPanel.Visible=false;
            authPanel.VisibleChanged += delegate { page.RowStyles[4].Height=authPanel.Visible?126:0; };
            var authTitle = new Label { Text="在官方页面授权（无需在此填写密码）", Dock=DockStyle.Top, Height=27 };
            authCode.Name="官方登录代码"; authCode.ReadOnly=true; authCode.Width=170; authCode.Font=new Font("Consolas",15,FontStyle.Bold);
            ButtonStyle(authOpen,"打开官方登录页",false); ButtonStyle(authCopy,"复制登录代码",false);
            authOpen.Click += delegate { OpenUrl("https://auth.openai.com/codex/device"); };
            authCopy.Click += delegate { if(authCode.Text.Length>0) Clipboard.SetText(authCode.Text); };
            var authRow = new FlowLayoutPanel { Dock=DockStyle.Fill, Padding=new Padding(0,9,0,0), WrapContents=false };
            authRow.Controls.AddRange(new Control[]{authCode,authOpen,authCopy}); authPanel.Controls.Add(authRow); authPanel.Controls.Add(authTitle);
            page.Controls.Add(authPanel,0,4);
            var runRow = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false };
            ButtonStyle(start,"开始接单",true); ButtonStyle(stop,"停止接单",false); stop.Enabled=false;
            ButtonStyle(force,"强制停止",false); force.Enabled=false;
            runRow.Controls.Add(start);runRow.Controls.Add(stop);runRow.Controls.Add(force);
            modelLabel.Text="检查连接后显示可用模型"; modelLabel.AutoEllipsis=true; modelLabel.Width=245;modelLabel.Height=40;
            modelLabel.TextAlign=ContentAlignment.MiddleLeft;runRow.Controls.Add(modelLabel);page.Controls.Add(runRow,0,5);
            start.Click += delegate { StartBridge("run",null); };stop.Click += delegate { RequestStop(); };force.Click += delegate { ConfirmForceStop(); };
            var bottom = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=3 };
            bottom.RowStyles.Add(new RowStyle(SizeType.Absolute,44));bottom.RowStyles.Add(new RowStyle(SizeType.Absolute,6));bottom.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var helpers=new FlowLayoutPanel { Dock=DockStyle.Fill,WrapContents=false };
            ButtonStyle(install,"安装 / 修复环境",false);ButtonStyle(selftest,"离线自检",false);
            install.Click += delegate { StartInstall(); };selftest.Click += delegate { StartBridge("selftest",null); };
            helpers.Controls.Add(install);helpers.Controls.Add(selftest);bottom.Controls.Add(helpers,0,0);
            progress.Dock=DockStyle.Fill;progress.Style=ProgressBarStyle.Marquee;progress.Visible=false;bottom.Controls.Add(progress,0,1);
            logBox.Name="操作记录";logBox.Multiline=true;logBox.ReadOnly=true;logBox.ScrollBars=ScrollBars.Vertical;logBox.Dock=DockStyle.Fill;
            logBox.BackColor=Color.White;logBox.BorderStyle=BorderStyle.FixedSingle;bottom.Controls.Add(logBox,0,2);page.Controls.Add(bottom,0,6);
            var foot=new Label { Dock=DockStyle.Fill,Text="保持窗口打开才会接单；停止等待本轮完成，卡住可强制停止。\n专用登录留在本机，不修改个人记忆。只批准信任的朋友使用。",Font=new Font(Font.FontFamily,9),ForeColor=Color.DimGray };
            page.Controls.Add(foot,0,7);
            Shown += delegate {
                if(preview)return;
                try { VerifyPackage();verified=true; }
                catch { state.Text="安装包校验未通过";detail.Text="请重新下载完整官方仓库安装包并解压，不要混用不同版本文件。";SetBusy(true);return; }
                if(IsElevated()) { state.Text="请使用普通权限打开";detail.Text="仅安装需要时再用管理员权限；不能以管理员身份接单。";Log(detail.Text); }
                if(File.Exists(PythonPath)) StartBridge("status",null);
                else { state.Text="先安装运行环境";detail.Text="点击「安装 / 修复环境」，完成后在本窗口连接网站。";
                    if(initialAction=="install"){initialAction="";StartInstall();} }
            };
            closeTimer.Interval=100;
            closeTimer.Tick += delegate { if(closeWhenDone&&child!=null&&DateTime.UtcNow>=closeDeadline){closeTimer.Stop();ForceStop(true);} };
            FormClosing += OnClosing;
            FormClosed += delegate {closeTimer.Stop();closeTimer.Dispose(); if(lifetime!=null)lifetime.Dispose();};
        }
        string PythonPath { get { return Path.Combine(root,@".venv\Scripts\python.exe"); } }
        static bool IsElevated() { using(var i=WindowsIdentity.GetCurrent()) return new WindowsPrincipal(i).IsInRole(WindowsBuiltInRole.Administrator); }
        void AddField(TableLayoutPanel parent,int row,string title,TextBox box,string accessible) {
            parent.Controls.Add(new Label { Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft },0,row);
            box.Dock=DockStyle.Fill;box.Margin=new Padding(0,5,0,5);box.Name=accessible;box.AccessibleName=accessible;parent.Controls.Add(box,1,row);
        }
        void ButtonStyle(Button b,string title,bool primary) {
            b.Name=title;b.Text=title;b.AutoSize=false;b.Size=new Size(142,38);b.Margin=new Padding(0,2,10,2);
            b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(197,207,192);
            b.BackColor=primary?Green:Color.White;b.ForeColor=primary?Color.White:Ink;
        }
        void Log(string text) {
            if(String.IsNullOrWhiteSpace(text))return;
            if(logBox.TextLength>32000)logBox.Text=logBox.Text.Substring(logBox.TextLength-16000);
            logBox.AppendText(DateTime.Now.ToString("HH:mm:ss")+"  "+text+Environment.NewLine);
        }
        void SetBusy(bool value) {
            foreach(var b in new[]{connect,login,check,start,install,selftest})b.Enabled=!value;
            nameBox.Enabled=!value;codeBox.Enabled=!value;siteBox.Enabled=!value;editSite.Enabled=!value;
            stop.Enabled=value&&!stopRequested&&(operation=="run"||operation=="login");force.Enabled=value&&child!=null&&!forceStopping;progress.Visible=value;
        }
        void VerifyPackage() {
            var m=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"manifest.json"),Encoding.UTF8));
            var files=(Dictionary<string,object>)m["files"];
            foreach(var entry in files) {
                string path=Path.GetFullPath(Path.Combine(root,entry.Key.Replace('/',Path.DirectorySeparatorChar)));
                if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase)||entry.Key.Contains(".."))throw new IOException();
                for(string current=path;current.Length>=root.TrimEnd(Path.DirectorySeparatorChar).Length;current=Path.GetDirectoryName(current)) {
                    if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException();
                    if(current.TrimEnd(Path.DirectorySeparatorChar).Equals(root.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))break;
                }
                using(var stream=File.OpenRead(path))using(var hash=SHA256.Create())
                    if(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=(string)entry.Value)throw new IOException();
            }
        }
        void ConnectNow() {
            if(nameBox.Text.Trim().Length==0||codeBox.Text.Trim().Length==0){MessageBox.Show(this,"填写电脑名，并粘贴网站生成的电脑连接码。","还差一步");return;}
            chainConnect=true;
            StartBridge("pair",new Dictionary<string,object>{{"name",nameBox.Text.Trim()},{"site",siteBox.Text.Trim()},{"code",codeBox.Text.Trim()}});
        }
        void OpenWebsite(){
            Uri u;if(!Uri.TryCreate(siteBox.Text.Trim(),UriKind.Absolute,out u)||u.Scheme!="https"||!String.IsNullOrEmpty(u.UserInfo)||u.AbsolutePath!="/"||u.Query!=""||u.Fragment!=""){
                MessageBox.Show(this,"网站地址必须为 HTTPS 根地址，例如 https://projects.zuichenji.cn。","网站地址不正确");return;}
            OpenUrl(u.GetLeftPart(UriPartial.Authority)+"/projects");
        }
        void OpenUrl(string url){try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch{MessageBox.Show(this,"无法打开默认浏览器，请手动打开：\n"+url,"打开网页");}}
        void StartBridge(string action,Dictionary<string,object> values) {
            if(child!=null||!verified)return;
            if(!File.Exists(PythonPath)){chainConnect=false;MessageBox.Show(this,"请先点击「安装 / 修复环境」。","需要安装环境");return;}
            if(action=="run"&&IsElevated()){chainConnect=false;MessageBox.Show(this,"请关闭本窗口，然后普通双击「帧间连接助手.exe」。管理员权限只用于安装，不用于执行朋友的需求。","请使用普通权限接单");return;}
            var request=values??new Dictionary<string,object>();request["action"]=action;
            string basePython="";
            foreach(string line in File.ReadAllLines(Path.Combine(root,@".venv\pyvenv.cfg")))
                if(line.StartsWith("home = "))basePython=Path.Combine(line.Substring(7).Trim(),"python.exe");
            if(!File.Exists(basePython)){chainConnect=false;MessageBox.Show(this,"运行环境不完整，请点击「安装 / 修复环境」。","需要修复环境");return;}
            // Hold behind a one-byte gate until the Job Object owns the real interpreter and all descendants.
            string gate="import os,subprocess,sys; gate=os.read(0,1); sys.exit(subprocess.call(sys.argv[1:]) if gate==b'G' else 125)";
            StartProcess(basePython,"-I -S -u -c \""+gate+"\" \""+PythonPath+"\" -u -m project_worker.gui_bridge",action,json.Serialize(request));
        }
        void StartInstall(){
            if(child!=null||!verified)return;
            string ps=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe");
            StartProcess(ps,"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""+Path.Combine(root,"Install.ps1")+"\" -Graphical","install",null);
        }
        void StartProcess(string executable,string arguments,string action,string request) {
            operation=action;operationOk=false;needPython=false;stopRequested=false;forceStopping=false;generation++;int ownGeneration=generation;
            var info=new ProcessStartInfo(executable,arguments){WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,
                RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
            info.EnvironmentVariables["PYTHONUTF8"]="1";info.EnvironmentVariables["PYTHONIOENCODING"]="utf-8";
            var proc=new Process{StartInfo=info,EnableRaisingEvents=true};
            child=proc;SetBusy(true);state.Text=action=="run"?"正在准备接单":"正在处理";
            detail.Text=action=="install"?"正在安装独立环境，可能需要几分钟；请保持网络畅通。":action=="pair"?"正在使用连接码配对这台电脑…":action=="login"?"正在检查专用登录，必要时会显示官方授权代码…":action=="check"?"正在检查网站、官方订阅和可用模型…":"请稍候…";
            Log(detail.Text);
            proc.OutputDataReceived += delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)Ui(delegate{if(generation==ownGeneration)OnLine(e.Data,action);});};
            proc.ErrorDataReceived += delegate(object sender,DataReceivedEventArgs e){/* Never display unfiltered CLI/server stderr or credentials. */};
            proc.Exited += delegate{Ui(delegate{Finish(proc,action,ownGeneration);});};
            try{
                proc.Start();lifetime=new ChildLifetime(proc);proc.BeginOutputReadLine();proc.BeginErrorReadLine();
                input=new StreamWriter(proc.StandardInput.BaseStream,new UTF8Encoding(false));
                if(request!=null){
                    var writer=input;
                    SendInput(writer,"G"+request+"\n").ContinueWith(t=>Ui(delegate {
                        if(child==proc&&generation==ownGeneration&&!t.Result){Log("后台启动请求未送达，正在停止本助手后台进程。");ForceStop(false);}
                    }));
                }
            }catch{
                try{if(!proc.HasExited)proc.Kill();}catch{}
                if(lifetime!=null){lifetime.Dispose();lifetime=null;}child=null;SetBusy(false);chainConnect=false;
                state.Text="无法启动";detail.Text="请先安装运行环境，或重新下载完整接入包。";Log(detail.Text);
            }
        }
        void Ui(Action action){if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(action);}catch(InvalidOperationException){}}
        static string S(Dictionary<string,object> value,string key){object x;return value.TryGetValue(key,out x)&&x!=null?Convert.ToString(x):"";}
        void OnLine(string line,string action) {
            if(action=="install"){
                if(line=="FRAME_INSTALL|NEED_PYTHON"){needPython=true;return;}
                if(line.StartsWith("FRAME_INSTALL|")){detail.Text=line.Substring(14);Log(detail.Text);}return;
            }
            Dictionary<string,object> e;try{e=json.Deserialize<Dictionary<string,object>>(line);}catch{return;}
            string kind=S(e,"type"),message=S(e,"message");
            if(kind=="status"){
                string name=S(e,"name");if(name!="")nameBox.Text=name;
                string site=S(e,"site");if(site!="")siteBox.Text=site;
                state.Text="等待连接检查";detail.Text="填写连接码可重新配对；已有连接可直接点击「检查连接」。";
            }else if(kind=="paired"){
                codeBox.Clear();nameBox.Text=S(e,"name");state.Text="网站已连接";
            }else if(kind=="device_code"){
                if(S(e,"url")!="https://auth.openai.com/codex/device")return;
                authCode.Text=S(e,"code");authPanel.Visible=true;state.Text="等待本人授权";
            }else if(kind=="login_complete"){
                authCode.Clear();authPanel.Visible=false;state.Text="ChatGPT 已登录";
            }else if(kind=="checked"){
                state.Text="连接检查通过";nameBox.Text=S(e,"name");
                var models=e.ContainsKey("models")?e["models"] as IEnumerable:null;
                if(models!=null){var names=new List<string>();foreach(var m in models)names.Add(Convert.ToString(m));modelLabel.Text="可用模型："+String.Join("、",names.ToArray());}
            }else if(kind=="progress"){
                string stateName=S(e,"state");state.Text=stateName=="waiting_for_connection"?"正在等待网络恢复":"ChatGPT 在线";
                message=stateName=="processed"?"已完成并回传 "+S(e,"processed")+" 轮需求。":stateName=="waiting_for_connection"?"网络暂时断开，保留任务记录并等待恢复。":"正在接单。朋友发送需求后，本机会处理并回传结果。";
            }else if(kind=="stopped")state.Text="接单已停止";
            else if(kind=="error"){state.Text="需要处理";chainConnect=false;}
            else if(kind=="done")operationOk=e.ContainsKey("success")&&e["success"] is bool&&(bool)e["success"];
            if(message!=""){detail.Text=message;Log(message);}
        }
        async void Finish(Process proc,string action,int ownGeneration){
            // Let pending output callbacks (including the final result) arrive before changing buttons.
            await Task.Delay(150);
            if(child!=proc||generation!=ownGeneration)return;
            int exit;try{exit=proc.ExitCode;}catch{exit=-1;}
            bool forced=forceStopping;
            var ownedJob=lifetime;var ownedInput=input;lifetime=null;input=null;child=null;generation++;
            forceStopping=false;stopRequested=false;closeTimer.Stop();SetBusy(false);
            DisposeDetached(proc,ownedJob,ownedInput);
            authCode.Clear();authPanel.Visible=false;
            bool ok=!forced&&exit==0&&(action=="install"||operationOk);
            if(closeWhenDone){Close();return;}
            if(forced){state.Text="已停止本机执行";detail.Text="本助手后台进程已退出。未完成任务与文件保留，请核对网站状态后继续。";Log(detail.Text);return;}
            if(needPython){chainConnect=false;state.Text="需要安装 Python";detail.Text="安装 Python 3.12 或更新版本，然后回到这里点击「安装 / 修复环境」。";Log(detail.Text);
                if(MessageBox.Show(this,"这台电脑尚未找到 Python 3.12 或更新版本。是否打开官方下载页？安装时勾选 Add Python to PATH，安装完成后重新打开助手。","安装运行环境",MessageBoxButtons.YesNo,MessageBoxIcon.Information)==DialogResult.Yes)OpenUrl("https://www.python.org/downloads/windows/");return;}
            if(!ok){chainConnect=false;if(state.Text!="需要处理"){state.Text="操作未完成";detail.Text=action=="install"?"安装未完成。请检查网络；若提示权限问题，右键安装入口以管理员身份安装，再普通打开助手。":"请检查连接后重试，原项目和任务记录已保留。";Log(detail.Text);}}
            if(action=="install"&&ok){state.Text="环境安装完成";detail.Text="填写电脑名和连接码，点击「连接网站」。";Log(detail.Text);StartBridge("status",null);return;}
            if(closeWhenDone){Close();return;}
            if(ok&&chainConnect&&action=="pair"){StartBridge("login",null);return;}
            if(ok&&chainConnect&&action=="login"){chainConnect=false;StartBridge("check",null);return;}
            if(action=="status"&&initialAction!=""){
                string next=initialAction;initialAction="";
                if(next=="install")StartInstall();else if(next=="login"||next=="check"||next=="run"||next=="selftest")StartBridge(next,null);
            }
        }
        static Task<bool> SendInput(StreamWriter writer,string text){
            // Pipe writes and flushes can block when a child hangs. Never run them on the UI thread.
            return Task.Run(delegate {try{lock(writer){writer.Write(text);writer.Flush();}return true;}catch{return false;}});
        }
        static void DisposeDetached(Process proc,ChildLifetime job,StreamWriter writer){
            Task.Run(delegate {
                try{if(job!=null)job.Dispose();}catch{}
                // Close the pipe before disposing the writer, so no flush can hold the window open.
                try{if(writer!=null)writer.BaseStream.Close();}catch{}
                try{if(writer!=null)writer.Dispose();}catch{}
                try{proc.Dispose();}catch{}
            });
        }
        async void RequestStop(){
            if(child==null||stopRequested)return;chainConnect=false;stopRequested=true;
            var proc=child;var writer=input;int ownGeneration=generation;
            stop.Enabled=false;
            state.Text=operation=="login"?"正在取消登录":"正在停止接单";
            detail.Text=closeWhenDone?"正在退出；最多等待 10 秒，随后结束本助手后台进程。":"不再领取新需求；当前任务完成后停止。若卡住，可点击「强制停止」。";Log(detail.Text);
            bool sent=writer!=null&&await SendInput(writer,"{\"action\":\"stop\"}\n");
            if(child==proc&&generation==ownGeneration&&!sent)Log("停止请求未送达，可点击「强制停止」；关闭窗口也有退出时限。");
        }
        void ConfirmForceStop(){
            if(child==null)return;
            if(MessageBox.Show(this,"立即中断本助手当前任务？只结束本助手启动的后台进程，不关闭其他 GPT 窗口。未完成任务会保留，之后需要核对网站状态。","强制停止",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)ForceStop(false);
        }
        async void ForceStop(bool exit){
            if(exit)closeWhenDone=true;
            if(child==null){if(closeWhenDone)Close();return;}
            if(forceStopping)return;
            forceStopping=true;stopRequested=true;chainConnect=false;initialAction="";closeTimer.Stop();
            var proc=child;var ownedJob=lifetime;var writer=input;int ownGeneration=generation;
            stop.Enabled=false;force.Enabled=false;state.Text="正在强制停止";
            detail.Text="正在结束本助手后台进程，保留项目文件。不会重启电脑或关闭其他 GPT 窗口。";Log(detail.Text);
            // Ask the controller to cancel first. A stuck pipe never delays the kill deadline.
            if(writer!=null)SendInput(writer,"{\"action\":\"cancel\"}\n");
            await Task.Delay(closeWhenDone?0:2500);
            if(child!=proc||generation!=ownGeneration)return;
            var kill=Task.Run(delegate {
                try{if(ownedJob!=null)ownedJob.Dispose();}catch{}
                try{if(!proc.HasExited)proc.Kill();}catch{}
                try{return proc.WaitForExit(1500);}catch{return false;}
            });
            var completed=await Task.WhenAny(kill,Task.Delay(2000));
            if(child!=proc||generation!=ownGeneration)return;
            if(closeWhenDone&&completed!=kill){Environment.Exit(2);return;}
            bool stopped=completed==kill&&kill.Result;
            if(!stopped){
                forceStopping=false;force.Enabled=true;state.Text="未能确认后台退出";
                detail.Text="请关闭本窗口并选择「立即退出」。任务记录保留，不会自动重复执行。";Log(detail.Text);
                if(closeWhenDone)Environment.Exit(2);return;
            }
            Finish(proc,operation,ownGeneration);
        }
        DialogResult AskClose(){
            if(shutdownTestChoice.HasValue)return shutdownTestChoice.Value;
            using(var dialog=new Form {Text="关闭连接助手",FormBorderStyle=FormBorderStyle.FixedDialog,
                StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(520,170),MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,Font=Font}){
                var message=new Label {Text="后台仍在运行。等待退出最多 10 秒；立即退出会中断当前任务。\n只结束本助手启动的进程，未完成的任务和项目文件保留。",Left=18,Top=18,Width=485,Height=70};
                var wait=new Button {Text="等待退出（最多10秒）",Left=18,Top=108,Width=190,Height=38,DialogResult=DialogResult.Yes};
                var immediate=new Button {Text="立即退出",Left=220,Top=108,Width=130,Height=38,DialogResult=DialogResult.No};
                var back=new Button {Text="返回",Left=362,Top=108,Width=130,Height=38,DialogResult=DialogResult.Cancel};
                dialog.Controls.AddRange(new Control[]{message,wait,immediate,back});dialog.CancelButton=back;dialog.AcceptButton=wait;
                return dialog.ShowDialog(this);
            }
        }
        void BeginBoundedClose(){
            closeWhenDone=true;chainConnect=false;initialAction="";
            // Reserve two seconds for forced cleanup after eight seconds of graceful waiting.
            closeDeadline=DateTime.UtcNow.AddSeconds(8);closeTimer.Start();RequestStop();
        }
        void OnClosing(object sender,FormClosingEventArgs e){
            if(child==null)return;e.Cancel=true;
            if(closeWhenDone){ForceStop(true);return;}
            var choice=AskClose();
            if(choice==DialogResult.Yes)BeginBoundedClose();
            else if(choice==DialogResult.No)ForceStop(true);
        }
        public int ShutdownFixture(string mode,string marker){
            StartProcess(Application.ExecutablePath,"--shutdown-fixture-root \""+marker+"\"",mode,"{}");
            return child==null?0:child.Id;
        }
        public void BlockFixturePipe(){if(input!=null)SendInput(input,new string('x',1024*1024));}
        public bool ShutdownIdle {get{return child==null;}}
        public void TestForceStop(){ForceStop(false);}
        public void TestClose(bool immediate){shutdownTestChoice=immediate?DialogResult.No:DialogResult.Yes;Close();}
        // This checks the actual form layout and required controls without touching a user's desktop or credentials.
        public object LayoutCheck(){
            PerformLayout();
            using(var image=new Bitmap(Width,Height)){
                DrawToBitmap(image,new Rectangle(0,0,Width,Height));
                image.Save(Path.Combine(root,"gui-preview.png"),System.Drawing.Imaging.ImageFormat.Png);
            }
            return new {result="passed",title=Text,computer_name=nameBox.AccessibleName,connection_code=codeBox.AccessibleName,
                code_masked=codeBox.UseSystemPasswordChar,actions=new[]{connect.Text,login.Text,check.Text,start.Text,stop.Text,force.Text,install.Text,selftest.Text},
                minimum_width=MinimumSize.Width,minimum_height=MinimumSize.Height,model_called=false};
        }
        public bool SmokeFinished { get { return child==null&&operation=="status"; } }
        public bool SmokeSucceeded { get { return operationOk; } }
        public object SmokeReport { get { return new {result=operationOk?"passed":"failed",state=state.Text,
            private_pipe_utf8=true,child_exited=child==null,model_called=false,site_tasks_claimed=false}; } }
    }
    static class Program {
        [STAThread] static void Main(string[] args){
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            string action=args.Length>0?args[0].TrimStart('-'):"";
            if(action=="shutdown-fixture-leaf"){
                var self=Process.GetCurrentProcess();
                File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new {pid=self.Id,start=self.StartTime.ToUniversalTime().Ticks}));
                Thread.Sleep(60000);return;
            }
            if(action=="shutdown-fixture-root"){
                if(Console.OpenStandardInput().ReadByte()!='G')return;
                using(var leaf=Process.Start(new ProcessStartInfo(Application.ExecutablePath,"--shutdown-fixture-leaf \""+args[1]+"\""){UseShellExecute=false,CreateNoWindow=true}))Thread.Sleep(60000);
                return;
            }
            if(action=="shutdown-check"){
                string mode=args.Length>1?args[1]:"run";
                bool immediate=args.Length>2&&args[2]=="immediate",forceOnly=args.Length>2&&args[2]=="force",blocked=args.Length>3&&args[3]=="blocked";
                bool remainedOpen=false;
                string marker=Path.Combine(Path.GetTempPath(),"frame-shutdown-fixture-"+Guid.NewGuid().ToString("N")+".json");
                var elapsed=new Stopwatch();Process parent=null,leaf=null;int ticks=0;double largestGap=0;DateTime previous=DateTime.UtcNow;
                string testError="";
                using(var form=new Assistant("",true))using(var timer=new System.Windows.Forms.Timer()){
                    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-10000,-10000);form.ShowInTaskbar=false;
                    timer.Interval=50;var deadline=DateTime.UtcNow.AddSeconds(25);
                    timer.Tick += delegate {var now=DateTime.UtcNow;largestGap=Math.Max(largestGap,(now-previous).TotalMilliseconds);previous=now;ticks++;if(now>deadline)Environment.Exit(3);};
                    form.Shown += async delegate {
                        try{
                            int pid=form.ShutdownFixture(mode,marker);if(pid==0)throw new IOException();
                            parent=Process.GetProcessById(pid);var ph=parent.Handle;
                            for(int i=0;i<100&&!File.Exists(marker);i++)await Task.Delay(50);
                            var record=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(marker));
                            leaf=Process.GetProcessById(Convert.ToInt32(record["pid"]));var lh=leaf.Handle;
                            if(leaf.StartTime.ToUniversalTime().Ticks!=Convert.ToInt64(record["start"]))throw new IOException();
                            if(blocked)form.BlockFixturePipe();
                            await Task.Delay(350);elapsed.Start();
                            if(forceOnly){
                                form.TestForceStop();
                                for(int i=0;i<120&&!form.ShutdownIdle;i++)await Task.Delay(50);
                                remainedOpen=form.ShutdownIdle&&!form.IsDisposed;
                                if(!remainedOpen)throw new IOException();
                                form.Close();
                            }else form.TestClose(immediate);
                        }catch(Exception ex){testError=ex.GetType().Name;form.TestClose(true);}
                    };
                    timer.Start();Application.Run(form);timer.Stop();elapsed.Stop();
                }
                bool parentExited=parent!=null&&parent.WaitForExit(1500),leafExited=leaf!=null&&leaf.WaitForExit(1500);
                bool passed=testError==""&&parentExited&&leafExited&&ticks>=4&&largestGap<1500&&elapsed.ElapsedMilliseconds<(immediate?4500:forceOnly?7000:12000)&&(!forceOnly||remainedOpen);
                var report=new {result=passed?"passed":"failed",mode=mode,immediate=immediate,force_stop_only=forceOnly,window_remained_open=remainedOpen,blocked_pipe=blocked,
                    elapsed_ms=elapsed.ElapsedMilliseconds,ui_ticks=ticks,max_ui_gap_ms=largestGap,parent_exited=parentExited,descendant_exited=leafExited,
                    error_type=testError,model_called=false,personal_data_read=false};
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"shutdown-"+mode+"-"+(immediate?"immediate":forceOnly?"force":"bounded")+(blocked?"-blocked":"")+".json"),new JavaScriptSerializer().Serialize(report),new UTF8Encoding(false));
                if(parent!=null)parent.Dispose();if(leaf!=null)leaf.Dispose();try{File.Delete(marker);}catch{}
                Environment.ExitCode=passed?0:1;return;
            }
            if(action=="layout-check"){
                using(var form=new Assistant("",true)){
                    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-10000,-10000);
                    form.Show();Application.DoEvents();
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"gui-layout-report.json"),new JavaScriptSerializer().Serialize(form.LayoutCheck()),new UTF8Encoding(false));form.Close();
                }return;
            }
            if(action=="smoke-check"){
                using(var form=new Assistant(""))using(var timer=new System.Windows.Forms.Timer()){
                    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-10000,-10000);
                    var deadline=DateTime.UtcNow.AddSeconds(60);timer.Interval=200;
                    timer.Tick += delegate {
                        if(!form.SmokeFinished&&DateTime.UtcNow<deadline)return;
                        timer.Stop();File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"gui-smoke-report.json"),new JavaScriptSerializer().Serialize(form.SmokeReport),new UTF8Encoding(false));
                        if(form.SmokeFinished){form.LayoutCheck();form.Close();}else Environment.Exit(2);
                    };
                    timer.Start();Application.Run(form);
                }return;
            }
            bool created;using(var mutex=new Mutex(true,"Local\\FrameStudioGUI-"+WindowsIdentity.GetCurrent().User.Value,out created)){
                if(!created){MessageBox.Show("连接助手已经打开，请使用现有窗口。","帧间连接助手");return;}
                Application.Run(new Assistant(action));
            }
        }
    }
}
