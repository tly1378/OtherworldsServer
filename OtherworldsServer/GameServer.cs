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
    class ClientHandler
    {
        GameServer server;
        bool run = true;

        public Socket socket;
        public Thread receiverThread;
        public Thread senderThread;
        public readonly Queue<string> sendQueue = new Queue<string>();
        public readonly Queue<string> receiveQueue = new Queue<string>();

        public ClientHandler(Socket socket, GameServer server)
        {
            this.socket = socket;
            this.server = server;

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
                catch(SocketException e)
                {
                    server.Remove(this);
                    server.Log(e.Message);
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
                        server.Remove(this);
                        server.Log(e.Message);
                        run = false;
                        return;
                    }
                }
            }
        }
    }

    class GameServer : IOutput
    {

        Thread listenerThread;
        List<ClientHandler> clients;
        object clients_lock = new object();
        public readonly Queue<string> queue = new Queue<string>();

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
                    Socket remote = server.Accept();
                    ClientHandler client = new ClientHandler(remote, this);

                    lock (clients_lock)
                    {
                        clients.Add(client);
                    }
                }
            });
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        public void Remove(ClientHandler handler)
        {
            lock (clients_lock)
            {
                clients.Remove(handler);
            }
        }

        public string GetOutput()
        {
            lock (clients_lock)
            {
                foreach(ClientHandler handler in clients)
                {
                    if (handler.receiveQueue.Count > 0)
                    {
                        return handler.receiveQueue.Dequeue();
                    }
                }
            }
            if (queue.Count > 0)
            {
                return queue.Dequeue();
            }
            return null;
        }

        public string Receive(Socket socket)
        {
            lock (clients_lock)
            {
                var client = clients.Find((s) => { return s.socket == socket; });
                return client.receiveQueue.Dequeue();
            }
        }

        public void Send(Socket socket, string message)
        {
            lock (clients_lock)
            {
                var client = clients.Find((s) => { return s.socket == socket; });
                client.sendQueue.Enqueue(message);
            }
        }

        public void Send(string message)
        {
            lock (clients_lock)
            {
                clients.ForEach((s) => { Send(s.socket, message); });
            }
        }

        public void Log(string message)
        {
            queue.Enqueue(message);
        }
    }
}
