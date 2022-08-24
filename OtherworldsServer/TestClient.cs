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

namespace OtherworldsServer
{
    class TestClient: IMessage
    {
        bool run = true;
        public Socket server;
        public Thread receiverThread;
        public Thread senderThread;
        readonly Queue<object> sendQueue = new Queue<object>();
        readonly Queue<object> receiveQueue = new Queue<object>();

        public TestClient(string host, int port)
        {
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Connect(ipe);

            receiverThread = new Thread(() => { ReceiveLoop(); });
            receiverThread.IsBackground = true;
            receiverThread.Start();

            senderThread = new Thread(() => { SendLoop(); });
            senderThread.IsBackground = true;
            senderThread.Start();
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
                    catch (SocketException e)
                    {
                        receiveQueue.Enqueue(new Message(e.Message, Message.Type.Disconnect));
                        run = false;
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
                    receiveQueue.Enqueue(new Message(e.Message, Message.Type.Disconnect));
                    run = false;
                    return;
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
    }
}
