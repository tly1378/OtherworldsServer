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
        IServer server;
        Thread receiverThread;

        string serverHost = "192.168.0.3";
        int serverPort = 21000;
        string clientHost = "49.189.121.181";
        int clientPort = 21000;

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
            if (!string.IsNullOrWhiteSpace(text))
            {
                Clipboard.SetText(text);
                inputBox.Text = text;
            }
        }
        #endregion

        #region winform tool
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
            while (server != null)
            {
                object _object = server.GetObject();
                if (_object != null)
                {
                    if(_object is Message msg)
                    {
                        Log("Message!!!");
                    }
                    else
                    {
                        Log($"{_object.GetType()}: {_object}");
                    }
                }
            }
        }
        #endregion

        #region cmd
        public void SetServerIP(string serverHost, string serverPort)
        {
            this.serverHost = serverHost;
            if(int.TryParse(serverPort, out int port))
                this.serverPort = port;
        }

        public void StartServer()
        {
            if (server == null)
            {
                try
                {
                    server = new GameServer(serverHost, serverPort);
                    receiverThread = new Thread(() => { ReceiveLoop(); });
                    receiverThread.IsBackground = true;
                    receiverThread.Start();

                    Log("服务器已开启");
                    Text = "服务器";
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

        public void SetClientIP(string clientHost, string clientPort)
        {
            this.clientHost = clientHost;
            if (int.TryParse(clientPort, out int port))
                this.clientPort = port;
        }

        public void StartClient()
        {
            if (server == null)
            {
                try
                {
                    Log("正在连接服务器……");
                    server = new TestClient(clientHost, clientPort);
                    receiverThread = new Thread(() => { ReceiveLoop(); });
                    receiverThread.IsBackground = true;
                    receiverThread.Start();
                    Log("客户端已开启");
                    Text = "客户端";
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

        public void SendString(string text)
        {
            if (server != null)
            {
                server.Send(text);
            }
        }

        int index = 0;
        public void SendMessage(string content)
        {
            if (server != null)
            {
                Message message = new Message(content);
                message.index = index++;
                server.Send(message);
            }
        }

        public void SendTo(string id, string message)
        {
            if (server != null)
            {
                server.SendTo(id, message);
            }
        }

        public void ShowIP()
        {
            Log($"服务端的部署IP：{serverHost}:{serverPort}");
            Log($"客户端的目标IP：{clientHost}:{clientPort}");
        }

        public void Clear()
        {
            outputBox.Items.Clear();
        }

        public void CloseServer()
        {
            server.Stop();
            server = null;
            Log("服务已终止");
            Text = "调试工具";
        }
        #endregion 
    }
}
