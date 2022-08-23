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
        public Socket socket;
        public Thread receiverThread;
        public Thread senderThread;
        public readonly Queue<string> sendQueue = new Queue<string>();
        public readonly Queue<string> receiveQueue = new Queue<string>();

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
    }

    class GameServer : IOutput
    {

        Thread listenerThread;
        List<ClientHandler> clients;
        object clients_lock = new object();

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
                    ClientHandler client = new ClientHandler(remote);

                    lock (clients_lock)
                    {
                        clients.Add(client);
                    }
                }
            });
            listenerThread.IsBackground = true;
            listenerThread.Start();
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
    }
}
