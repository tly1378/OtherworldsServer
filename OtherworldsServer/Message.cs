using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OtherworldsServer
{
    [Serializable]
    class Message
    {
        public enum Type
        {
            Disconnect,
            Content,
            Command
        }

        public int index;
        public string message;
        public Type type;

        public Message(string message, Type type = Type.Content)
        {
            this.message = message;
            this.type = type;
        }

        public override string ToString()
        {
            return $"[{type}]: {message}";
        }
    }
}
