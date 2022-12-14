using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OtherworldDataform;

namespace OtherworldsServer
{
    class ClientHandler
    {
        bool run = true;

        public Socket socket;
        public Thread receiverThread;
        public Thread senderThread;
        public readonly Queue<object> sendQueue = new Queue<object>();
        public readonly Queue<object> receiveQueue = new Queue<object>();


        public ClientHandler(Socket socket)
        {
            this.socket = socket;

            receiverThread = new Thread(() => { ReceiveLoop(); });
            receiverThread.IsBackground = true;
            receiverThread.Start();

            senderThread = new Thread(() => { SendLoop(); });
            senderThread.IsBackground = true;
            senderThread.Start();
        }

        Action callbackAction;
        public void SetCallback(Action action)
        {
            callbackAction = action;
        }

        public void SetId(string id)
        {
            throw new NotImplementedException();
        }

        void ReceiveLoop()
        {
            while (run)
            {
                try
                {
                    object _object = TCPTool.Receive(socket);
                    receiveQueue.Enqueue(_object);
                }
                catch(Exception e)
                {
                    receiveQueue.Enqueue(new Message($"{socket.LocalEndPoint as IPEndPoint} {e.Message}", Message.Type.Disconnect));
                    Stop();
                }
            }
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
                        TCPTool.Send(socket, _object);
                    }
                    catch (InsufficientBufferingException e)
                    {
                        receiveQueue.Enqueue(new Message($"{socket.LocalEndPoint as IPEndPoint} {e.Message}", Message.Type.Disconnect));
                    }
                    catch(SocketException e)
                    {
                        receiveQueue.Enqueue(new Message($"{socket.LocalEndPoint as IPEndPoint} {e.Message}", Message.Type.Disconnect));
                        Stop();
                    }
                }
            }
        }

        public void Stop()
        {
            run = false;
            TCPTool.Close(socket);
            callbackAction();
        }

        public object GetNextOutput()
        {
            if (receiveQueue.Count > 0)
                return receiveQueue.Dequeue();
            else
                return null;
        }
    }
}
