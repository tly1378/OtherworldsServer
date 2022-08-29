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
    class InsufficientBufferingException: Exception
    {
        public InsufficientBufferingException(string message) : base(message) { }
    }

    static class TCPTool
    {
        public const int BUFFERSIZE = 1024;

        public static void Send(Socket socket, object pack)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream mStream = new MemoryStream())
            {
                formatter.Serialize(mStream, pack);
                mStream.Flush();
                byte[] buffer;
                if (mStream.Length > BUFFERSIZE)
                {
                    buffer = Encoding.UTF8.GetBytes($"{1}");
                    socket.Send(buffer, buffer.Length, SocketFlags.None);
                    throw new InsufficientBufferingException($"数据大小为{mStream.Length}字节；超过了上限{BUFFERSIZE}字节");
                }
                buffer = mStream.GetBuffer();
                socket.Send(buffer, (int)mStream.Length, SocketFlags.None);
                Console.WriteLine($"数据大小为{mStream.Length}字节");
            }
        }

        public static object Receive(Socket socket)
        {
            byte[] buffer = new byte[BUFFERSIZE];
            socket.Receive(buffer);
            using (MemoryStream mStream = new MemoryStream())
            {
                mStream.Write(buffer, 0, BUFFERSIZE);
                mStream.Flush();
                mStream.Seek(0, SeekOrigin.Begin);
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(mStream);
            }
        }

        public static void Close(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
