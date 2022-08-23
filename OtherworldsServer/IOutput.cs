using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OtherworldsServer
{
    interface IOutput
    {
        string GetOutput();
        void Send(string msg);
    }
}
