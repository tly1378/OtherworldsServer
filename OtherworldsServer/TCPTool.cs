using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace OtherworldsServer
{
    static class TCPTool
    {
        public static void Send(Socket socket, object pack)
        {
            Console.WriteLine(pack.GetType());

            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream mStream = new MemoryStream())
            {
                formatter.Serialize(mStream, pack);
                mStream.Flush();
                socket.Send(mStream.GetBuffer(), (int)mStream.Length, SocketFlags.None);
            }
        }

        public static object Receive(Socket socket)
        {
            byte[] buffer = new byte[1024];
            socket.Receive(buffer);
            using (MemoryStream mStream = new MemoryStream())
            {
                mStream.Write(buffer, 0, 1024);
                mStream.Flush();
                mStream.Seek(0, SeekOrigin.Begin);
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(mStream);
            }
        }
    }
}
