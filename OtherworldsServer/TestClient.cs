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
    class TestClient: IOutput
    {
        bool run = true;
        public Socket socket;
        public Thread receiverThread;
        public Thread senderThread;
        readonly Queue<string> sendQueue = new Queue<string>();
        readonly Queue<string> receiveQueue = new Queue<string>();

        public TestClient(string host, int port)
        {
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
            while (run)
            {
                byte[] buffer = new byte[1024];
                try
                {
                    socket.Receive(buffer);
                }
                catch (SocketException e)
                {
                    receiveQueue.Enqueue(e.Message);
                    run = false;
                    return;
                }
                string message = Encoding.ASCII.GetString(buffer);
                receiveQueue.Enqueue(message);
            }
        }

        void SendLoop()
        {
            while (run)
            {
                if (sendQueue.Count > 0)
                {
                    string message = sendQueue.Dequeue();
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    try
                    {
                        socket.Send(buffer);
                    }
                    catch (SocketException e)
                    {
                        receiveQueue.Enqueue(e.Message);
                        run = false;
                        return;
                    }
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

        public void Send(object pack)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            byte[] retbuff = new byte[1];
            using (MemoryStream mStream = new MemoryStream())
            {
                formatter.Serialize(mStream, pack);
                mStream.Flush();
                socket.Send(mStream.GetBuffer(), (int)mStream.Length, SocketFlags.None);
                socket.Receive(retbuff, 1, SocketFlags.OutOfBand);
                if (retbuff[0] == 0)
                {
                    Send(pack);
                }
            }
        }
    }
}
