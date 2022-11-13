using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using TouchSocket.Sockets.Plugins;

namespace BulletScreen
{
    public class MyTcpClient : HttpSocketClient
    {
        public string? UserId { get; set; }
    }
}
