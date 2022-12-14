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
using Message = OtherworldDataform.Message;

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
            MethodInfo[] methods = GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            Log("Help List:");
            for (int i = 0; i < methods.Length; i++)
            {
                var md = methods[i];
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
                        Log($"?????????????????????{e.Message}");
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
                    if (_object is string text)
                    {
                        Log(text);
                    }
                    else
                    {
                        Log($"[{_object.GetType()}]>>> {_object}");
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
                    server = new GameServer(serverHost, serverPort, ()=> { server = null; });
                    receiverThread = new Thread(() => { ReceiveLoop(); });
                    receiverThread.IsBackground = true;
                    receiverThread.Start();

                    Log("??????????????????");
                    Text = "?????????";
                }
                catch(Exception e)
                {
                    Log(e.Message);
                }
            }
            else
            {
                Log("???????????????????????????");
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
                    Log("???????????????????????????");
                    server = new TestClient(clientHost, clientPort, null ,() => { server = null; });
                    receiverThread = new Thread(() => { ReceiveLoop(); });
                    receiverThread.IsBackground = true;
                    receiverThread.Start();
                    Log("??????????????????");
                    Text = "?????????";
                }
                catch (Exception e)
                {
                    Log($"?????????????????????????????? {e.Message}");
                }
            }
            else
            {
                Log("???????????????????????????");
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
                Message message = new Message(content, Message.Type.Content);
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
            Log($"??????????????????IP???{serverHost}:{serverPort}");
            Log($"??????????????????IP???{clientHost}:{clientPort}");
            Log($"?????????????????????{(server as TestClient)?.ID}");
        }

        public void Clear()
        {
            outputBox.Items.Clear();
        }

        public void CloseServer()
        {
            server.Stop();
            server = null;
            Log("???????????????");
            Text = "????????????";
        }

        public void SetId(string id)
        {
            if (server is TestClient client)
            {
                client.ID = id;
            }
        }
        #endregion 
    }
}
