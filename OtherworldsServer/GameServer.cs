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
    class GameServer : IMessage
    {
        Thread listenerThread;
        List<ClientHandler> clients;
        object clients_lock = new object();
        public readonly Queue<string> logQueue = new Queue<string>();

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
                while (true)
                {
                    Socket client = server.Accept();
                    ClientHandler handler = new ClientHandler(client);

                    lock (clients_lock)
                    {
                        clients.Add(handler);
                    }
                }
            });
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        public void Log(string message)
        {
            logQueue.Enqueue(message);
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
            lock (clients_lock)
            {
                foreach(ClientHandler handler in clients)
                {
                    if (handler.receiveQueue.Count > 0)
                    {
                        object output = handler.receiveQueue.Dequeue();
                        if(output is Message msg)
                        {
                            return msg;
                        }
                        else
                        {
                            Log(output.ToString());
                        }
                    }
                }
            }

            if (logQueue.Count > 0)
            {
                return new Message(logQueue.Dequeue());
            }
            return null;
        }
    }
}
