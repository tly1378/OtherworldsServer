using System;

namespace OtherworldDataform
{
    [Serializable]
    public class Message
    {
        public enum Type
        {
            Disconnect,
            Content,
            Command
        }

        public int index;
        public string message;
        public object[] packages;
        public Type type;

        public Message(string message, Type type = Type.Content)
        {
            this.message = message;
            this.type = type;
        }

        public Message(Type type = Type.Command, params object[] packages)
        {
            this.packages = packages;
            this.type = type;
        }

        public override string ToString()
        {
            return $"[{type}]: {message} [with packages: {packages == null}]";
        }
    }
}
