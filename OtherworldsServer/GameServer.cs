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
        object clients_lock = new object();
        Dictionary<string, ClientHandler> clients = new Dictionary<string, ClientHandler>();
        public readonly Queue<object> outputQueue = new Queue<object>();

        public GameServer(string host, int port)
        {
            IPAddress ip = IPAddress.Parse(host);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(ipe);
            server.Listen(0);

            listenerThread = new Thread(()=> 
            { 
                while (run)
                {
                    Socket client = server.Accept();
                    ClientHandler handler = new ClientHandler(client);
                    lock (clients_lock)
                    {
                        clients.Add((client.RemoteEndPoint as IPEndPoint).ToString(), handler);
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
                        foreach(string id in clients.Keys)
                        {
                            object receive = clients[id].GetNextOutput();
                            if(receive is Message msg && msg.type == Message.Type.Command)
                            {
                                Command(id, msg);
                            }
                            else if (receive is Message msg1 && msg1.type == Message.Type.Content)
                            {
                                outputQueue.Enqueue($"{id} {msg1.message}");
                            }
                            else if (receive != null)
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

        public void Remove(string id)
        {
            lock (clients_lock)
            {
                clients.Remove(id);
            }
        }

        public void Remove(ClientHandler handler)
        {
            lock (clients_lock)
            {
                List<KeyValuePair<string, ClientHandler>> keyValuePairs = clients.ToList();
                foreach(KeyValuePair<string, ClientHandler> keyValuePair in keyValuePairs)
                {
                    if(handler== keyValuePair.Value)
                    {
                        clients.Remove(keyValuePair.Key);
                    }
                }
            }
        }

        public void Send(object _object)
        {
            lock (clients_lock)
            {
                List<string> ids = clients.Keys.ToList();
                foreach (string id in ids)
                {
                    clients[id].sendQueue.Enqueue(_object);
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
                foreach (KeyValuePair<string, ClientHandler> keyValuePair in clients)
                {
                    keyValuePair.Value.Stop();
                }
            }
            clients.Clear();
            run = false;
        }

        #region cmds
        private void Command(string id, Message msg)
        {
            string[] cmds = msg.message.Split();
            var methodInfo = GetType().GetMethod(cmds[0]);
            if (methodInfo != null)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        string[] parameters = new string[] {id};
                        parameters = parameters.Concat(cmds.Skip(1)).ToArray();
                        methodInfo.Invoke(this, parameters);
                    }
                    catch (Exception e)
                    {
                        outputQueue.Enqueue($"指令{msg.message}调用失败：{e.Message}");
                    }
                });
                thread.Start();
            }
        }

        public void SetId(string oldId, string newId)
        {
            if (oldId == newId) return;
            ClientHandler handler = clients[oldId];
            lock (clients_lock)
            {
                clients.Add(newId, handler);
                clients.Remove(oldId);
            }
        }
        #endregion
    }
}
