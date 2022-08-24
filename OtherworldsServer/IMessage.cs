using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtherworldsServer
{
    interface IMessage
    {
        object GetObject();
        void Send(object _object);
    }
}
