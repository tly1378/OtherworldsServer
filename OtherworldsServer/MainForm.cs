using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;

namespace OtherworldsServer
{
    public partial class MainForm : Form
    {
        IOutput server;
        Thread receiverThread;

        string serverHost = "127.0.0.1";
        int serverPort = 21000;

        public MainForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            MethodInfo[] info = GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            Log("Help List:");
            for (int i = 0; i < info.Length; i++)
            {
                var md = info[i];
                string mothodName = md.Name;
                ParameterInfo[] paramInfos = md.GetParameters();
                Log($"\\{mothodName}{GetParamNames(paramInfos)}");
            }
            Log("");
        }

        #region winform component
        void execute_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(inputBox.Text))
                Log(inputBox.Text);

            if (inputBox.Text.StartsWith("\\"))
            {
                string line = inputBox.Text.Substring(1);
                Execute(line.Split(' '));
            }
        }

        private void outputBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = outputBox.SelectedItem?.ToString();
            if(!string.IsNullOrWhiteSpace(text))
                Clipboard.SetText(text);
        }
        #endregion

        #region 
        string GetParamNames(ParameterInfo[] paramInfos)
        {
            string names = "";
            foreach(ParameterInfo parameter in paramInfos)
            {
                names += " " + parameter.Name;
            }
            return names;
        }

        void Execute(params string[] cmds)
        {
            var methodInfo = GetType().GetMethod(cmds[0]);
            if (methodInfo != null)
            {
                Thread thread = new Thread(()=> 
                {
                    try
                    {
                        methodInfo.Invoke(this, cmds.Skip(1).ToArray()); 
                    }
                    catch(Exception e)
                    {
                        Log($"指令调用失败：{e.Message}");
                    }
                });
                thread.Start();
            }
        }

        void Log(string info)
        {
            outputBox.Items.Add(info);
        }

        void ReceiveLoop()
        {
            while (true)
            {
                string message = server.GetOutput();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Log($"{message}");
                }
            }
        }
        #endregion

        #region 
        public void StartServer()
        {
            if (server == null)
            {
                try
                {
                    server = new GameServer(serverHost, serverPort);
                    Log("服务器已开启");
                    receiverThread = new Thread(() => { ReceiveLoop(); });
                    receiverThread.IsBackground = true;
                    receiverThread.Start();
                }
                catch(Exception e)
                {
                    Log(e.Message);
                }
            }
            else
            {
                Log("已有正在运行的服务");
            }
        }

        public void StartClient()
        {
            if (server == null)
            {
                try
                {
                    Log("开始寻找服务器");
                    server = new TestClient(serverHost, serverPort);
                    Log("客户端已开启");
                    receiverThread = new Thread(() => { ReceiveLoop(); });
                    receiverThread.IsBackground = true;
                    receiverThread.Start();
                }
                catch (Exception e)
                {
                    Log($"错误：客户端开启失败 {e.Message}");
                }
            }
            else
            {
                Log("已有正在运行的服务");
            }
        }

        public void Send(string message)
        {
            if (server != null)
            {
                server.Send(message);
            }
        }

        public void SetIP(string serverHost, string serverPort)
        {
            this.serverHost = serverHost;
            if(int.TryParse(serverPort, out int port))
                this.serverPort = port;
        }

        public void ShowIP()
        {
            Log($"{serverHost}:{serverPort}");
        }
        #endregion 
    }
}
