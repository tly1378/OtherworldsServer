using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Message = OtherworldDataform.Message;

namespace OtherworldsServer
{
    class TestClient: IServer
    {
        bool run = true;

        public Socket server;
        public Thread receiverThread;
        public Thread senderThread;

        readonly Queue<object> sendQueue = new Queue<object>();
        readonly Queue<object> receiveQueue = new Queue<object>();

        private string id;
        private Action stopCallback;

        public string ID 
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                Send(new Message(Message.Type.Command, nameof(GameServer.Command_SetId),  id));
            } 
        }

        public TestClient(string host, int port, string id = null)
        {
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Connect(ipe);

            if (id == null)
                id = ipe.ToString();
            else
                ID = id;

            receiverThread = new Thread(() => { ReceiveLoop(); });
            receiverThread.IsBackground = true;
            receiverThread.Start();

            senderThread = new Thread(() => { SendLoop(); });
            senderThread.IsBackground = true;
            senderThread.Start();
        }

        public TestClient(string host, int port, string id = null, Action stopCallback = null) : this(host, port, id)
        {
            this.stopCallback = stopCallback;
        }

        void SendLoop()
        {
            while (run)
            {
                if (sendQueue.Count > 0)
                {
                    try
                    {
                        object _object = sendQueue.Dequeue();
                        TCPTool.Send(server, _object);
                    }
                    catch (InsufficientBufferingException e)
                    {
                        receiveQueue.Enqueue(new Message($"{server.LocalEndPoint as IPEndPoint} {e.Message}", Message.Type.Disconnect));
                    }
                    catch (SocketException e)
                    {
                        receiveQueue.Enqueue(new Message($"{server.LocalEndPoint as IPEndPoint} {e.Message}", Message.Type.Disconnect));
                        Stop();
                        return;
                    }
                }
            }
        }

        void ReceiveLoop()
        {
            while (run)
            {
                try
                {
                    object _object = TCPTool.Receive(server);
                    receiveQueue.Enqueue(_object);
                }
                catch (SocketException e)
                {
                    receiveQueue.Enqueue(new Message($"{server.LocalEndPoint as IPEndPoint} {e.Message}", Message.Type.Disconnect));
                    Stop();
                }
            }
        }

        public void Send(object _object)
        {
            sendQueue.Enqueue(_object);
        }

        public object GetObject()
        {
            if (receiveQueue.Count > 0)
                return receiveQueue.Dequeue();
            else
                return null;
        }

        public void SendTo(string id, object message)
        {
            Send(new Message(Message.Type.Command, nameof(GameServer.Command_SendTo), id, message));
        }

        public void Stop()
        {
            run = false;
            TCPTool.Close(server);
            stopCallback.Invoke();
        }
    }
}
