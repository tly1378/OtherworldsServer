using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtherworldsServer
{
    interface IServer
    {
        object GetObject();
        void Send(object _object);
        void SendTo(string id, object message);
        void Stop();
    }
}
