using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace OtherworldsServer
{
    class TestClient: IOutput
    {
        public Socket socket;
        public Thread receiverThread;
        public Thread senderThread;
        readonly Queue<string> sendQueue = new Queue<string>();
        readonly Queue<string> receiveQueue = new Queue<string>();

        public TestClient()
        {
            string host = "127.0.0.1";
            int port = 21000;

            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipe);


            receiverThread = new Thread(() => { ReceiveLoop(); });
            receiverThread.IsBackground = true;
            receiverThread.Start();

            senderThread = new Thread(() => { SendLoop(); });
            senderThread.IsBackground = true;
            senderThread.Start();

        }


        void ReceiveLoop()
        {
            while (true)
            {
                byte[] buffer = new byte[1024];
                socket.Receive(buffer);
                string message = Encoding.ASCII.GetString(buffer);
                receiveQueue.Enqueue(message);
            }
        }

        void SendLoop()
        {
            while (true)
            {
                if (sendQueue.Count > 0)
                {
                    string message = sendQueue.Dequeue();
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    socket.Send(buffer);
                }
            }
        }

        public string Receive()
        {
            if (receiveQueue.Count > 0)
                return receiveQueue.Dequeue();
            else
                return null;
        }

        public void Send(string message)
        {
            sendQueue.Enqueue(message);
        }

        public string GetOutput()
        {
            return Receive();
        }
    }
}
