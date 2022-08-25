using System;
using System.Collections.Generic;
using System.Linq;
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
            Content
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
            return $"[{index}][Message.{type}] {message}";
        }
    }
}
