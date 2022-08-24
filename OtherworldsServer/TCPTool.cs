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
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream mStream = new MemoryStream())
            {
                formatter.Serialize(mStream, pack);
                mStream.Flush();
                socket.Send(mStream.GetBuffer(), (int)mStream.Length, SocketFlags.None);
                //byte[] retbuff = new byte[1];
                //socket.Receive(retbuff, 1, SocketFlags.OutOfBand);
                //if (retbuff[0] == 0)
                //{
                //    Send(socket, pack);
                //}
            }
        }

        public static object Receive(Socket socket)
        {
            byte[] buffer = new byte[1024];
            Console.WriteLine("等待数据传入");
            socket.Receive(buffer);
            object result;
            using (MemoryStream mStream = new MemoryStream())
            {
                mStream.Write(buffer, 0, 1024);
                mStream.Flush();
                mStream.Seek(0, SeekOrigin.Begin);
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    object pack = formatter.Deserialize(mStream);
                    Console.WriteLine("接收到了可反序列化为object的数据");
                    result = pack;
                }
                catch
                {
                    string message = Encoding.ASCII.GetString(buffer);
                    Console.WriteLine("接收到了不可反序列化为object的数据");
                    result = message;
                }
            }
            return result;
        }
    }
}
