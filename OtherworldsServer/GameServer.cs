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
using OtherworldDataform;

namespace OtherworldsServer
{
    class GameServer : IServer
    {
        bool run = true;
        Thread listenerThread;
        Thread handerThread;
        object clients_lock = new object();
        Dictionary<string, ClientHandler> clients = new Dictionary<string, ClientHandler>();
        private Action stopCallback;
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
                            if(receive is Message msg)
                            {
                                if(msg.type == Message.Type.Command)
                                {
                                    Command(id, msg);
                                }
                                else if (msg.type == Message.Type.Content)
                                {
                                    outputQueue.Enqueue($"{id}: {msg.message}");
                                }
                                else if(msg.type == Message.Type.Disconnect)
                                {
                                    outputQueue.Enqueue($"{id} 下线，IP: {clients[id].socket.LocalEndPoint as IPEndPoint}");
                                }
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

        public GameServer(string host, int port, Action stopCallback) : this(host, port)
        {
            this.stopCallback = stopCallback;
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
            clients[id].sendQueue.Enqueue(message);
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
            stopCallback.Invoke();
        }

        #region cmds
        private void Command(string id, Message msg)
        {
            object[] parameters = msg.packages;
            string cmd = parameters[0] as string;
            var methodInfo = GetType().GetMethod(cmd);
            if (methodInfo != null)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        object[] id_parameters = new object[] {id};
                        id_parameters = id_parameters.Concat(parameters.Skip(1)).ToArray();
                        Console.WriteLine(methodInfo.Name);
                        Console.WriteLine(methodInfo.GetParameters().Length);
                        Console.WriteLine(id_parameters.Length);
                        methodInfo.Invoke(this, id_parameters);
                    }
                    catch (Exception e)
                    {
                        outputQueue.Enqueue($"指令{parameters[0]}调用失败：{e.Message}");
                    }
                });
                thread.Start();
            }
        }

        public void Command_SetId(string _id_, string newId)
        {
            if (_id_ == newId) 
            {
                clients[_id_].sendQueue.Enqueue("新id不能与原id一致");
                return;
            }
            if(clients.ContainsKey(newId))
            {
                clients[_id_].sendQueue.Enqueue("id重复");
                return;
            }
            lock (clients_lock)
            {
                ClientHandler handler = clients[_id_];
                clients.Remove(_id_);
                clients.Add(newId, handler);
            }
            clients[newId].sendQueue.Enqueue($"id变更为{newId}");
        }

        public void Command_SendTo(string _id_, string targetId, string message)
        {
            SendTo(targetId, $"{_id_}: {message}");
        }
        #endregion
    }
}
