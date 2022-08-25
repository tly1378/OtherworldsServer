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
    class GameServer : IServer
    {
        bool run = true;
        Thread listenerThread;
        Thread handerThread;
        List<ClientHandler> clients;
        object clients_lock = new object();
        public readonly Queue<object> outputQueue = new Queue<object>();

        public GameServer(string host, int port)
        {
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipe);
            server.Listen(0);

            clients = new List<ClientHandler>();
            listenerThread = new Thread(()=> 
            { 
                while (run)
                {
                    Socket client = server.Accept();
                    ClientHandler handler = new ClientHandler(client);
                    lock (clients_lock)
                    {
                        clients.Add(handler);
                    }
                    handler.SetCallback(()=> { Remove(handler); });
                    outputQueue.Enqueue(client.RemoteEndPoint as IPEndPoint);
                }
            });
            listenerThread.IsBackground = true;
            listenerThread.Start();

            handerThread = new Thread(() =>
            {
                while (run)
                {
                    lock (clients_lock)
                    {
                        for (int i = 0; i < clients.Count; i++)
                        {
                            object receive = clients[i].GetNextOutput();
                            if (receive != null)
                            {
                                outputQueue.Enqueue(receive);
                            }
                        }
                    }
                }
            });
            handerThread.IsBackground = true;
            handerThread.Start();
        }

        public void Remove(ClientHandler handler)
        {
            lock (clients_lock)
            {
                clients.Remove(handler);
            }
        }

        //public void Send(Socket socket, string message)
        //{
        //    lock (clients_lock)
        //    {
        //        ClientHandler client = clients.Find((s) => { return s.socket == socket; });
        //        client.sendQueue.Enqueue(message);
        //    }
        //}

        public void Send(object _object)
        {
            lock (clients_lock)
            {
                foreach (ClientHandler handler in clients)
                {
                    handler.sendQueue.Enqueue(_object);
                }
            }
        }

        public object GetObject()
        {
            if (outputQueue.Count > 0)
            {
                return outputQueue.Dequeue();
            }
            return null;
        }

        public void SendTo(string id, object message)
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            lock (clients_lock)
            {
                foreach (ClientHandler handler in clients)
                {
                    handler.Stop();
                }
            }
            clients.Clear();
            run = false;
        }
    }
}
