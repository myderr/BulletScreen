using TouchSocket.Core;
using TouchSocket.Core.Log;
using TouchSocket.Core.Plugins;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.Http.WebSockets.Plugins;
using TouchSocket.Rpc.WebApi;
using TouchSocket.Rpc;
using TouchSocket.Sockets;

namespace BulletScreen
{

    public class MyWebSocketPlugin : WebSocketPluginBase<MyTcpClient>
    {
        protected override void OnHandshaking(MyTcpClient client, HttpContextEventArgs e)
        {
            var userids = e.Context.Request.Query.GetValues("userid");
            var userid = "";
            if (userids is not null && userids.Length > 0)
            {
                userid = userids[0].Trim();
            }

            if (string.IsNullOrEmpty(userid))
            {
                e.IsPermitOperation = false;
            }
            else
            {
                client.UserId = userid;
            }

            base.OnHandshaking(client, e);
        }

    }

    public class MyServer : RpcServer
    {
        private readonly ILog m_logger;
        private readonly HttpService<MyTcpClient> _httpService;

        public MyServer(ILog logger, HttpService<MyTcpClient> httpService)
        {
            this.m_logger = logger;
            _httpService = httpService;
        }

        [Router("/send")]
        [WebApi(HttpMethodType.POST)]
        public Task send(SendData input)
        {
            var sendText = input.Msg;
            if (string.IsNullOrEmpty(input.Id)) return Task.CompletedTask;
            if (string.IsNullOrEmpty(sendText)) return Task.CompletedTask;

            if(sendText.Length > 15)
            {
                sendText = sendText[..15];
            }
            sendText = sendText.Replace("\r\n", " ");
            sendText = sendText.Trim();

            var sendClients = _httpService.GetClients().Where(m => m.UserId == input.Id).ToList();

            sendClients.ForEach(client =>
            {
                client.SendWithWS(input.Msg);
            });
            return Task.CompletedTask;
        }

        public class SendData
        {
            public string? Id { get; set; }
            public string? Msg { get; set; }
        }
    }
}