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

        public MainForm()
        {
            CheckForIllegalCrossThreadCalls = false;
            InitializeComponent();
            MethodInfo[] info = GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            outputBox.Items.Add("Help List:");
            for (int i = 0; i < info.Length; i++)
            {
                var md = info[i];
                string mothodName = md.Name;
                ParameterInfo[] paramInfos = md.GetParameters();
                outputBox.Items.Add($"\\{mothodName}{GetParamNames(paramInfos)}");
            }
            outputBox.Items.Add("");
        }

        #region winform component
        void execute_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(inputBox.Text))
                outputBox.Items.Add(inputBox.Text);

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
            if(methodInfo!=null)
                methodInfo.Invoke(this, cmds.Skip(1).ToArray());
        }

        void LogError(string info)
        {
            outputBox.Items.Add($"ERROR: {info}");
        }

        void ReceiveLoop()
        {
            while (true)
            {
                string message = server.GetOutput();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    outputBox.Items.Add($"{message}");//wuxiao
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
                    server = new GameServer();
                    outputBox.Items.Add("服务器已开启");
                }
                catch
                {
                    LogError("服务器开启失败");
                }
            }
            else
            {
                outputBox.Items.Add("已有正在运行的服务");
            }

            receiverThread = new Thread(() => { ReceiveLoop(); });
            receiverThread.IsBackground = true;
            receiverThread.Start();
        }

        public void StartClient()
        {
            if (server == null)
            {
                try
                {
                    server = new TestClient();
                    outputBox.Items.Add("客户端已开启");
                }
                catch (Exception e)
                {
                    LogError($"客户端开启失败 {e.Message}");
                }
            }
            else
            {
                outputBox.Items.Add("已有正在运行的服务");
            }

            receiverThread = new Thread(() => { ReceiveLoop(); });
            receiverThread.IsBackground = true;
            receiverThread.Start();
        }

        public void Send(string message)
        {
            if (server != null)
            {
                server.Send(message);
            }
        }
        #endregion 
    }
}
