using BulletScreen;
using TouchSocket.Core.Config;
using TouchSocket.Core.Dependency;
using TouchSocket.Core.IO;
using TouchSocket.Core.Log;
using TouchSocket.Core.Plugins;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.Rpc;
using TouchSocket.Rpc.WebApi;
using TouchSocket.Sockets;

var service = new HttpService<MyTcpClient>();
service.Setup(new TouchSocketConfig()//加载配置
    .UsePlugin()
    .SetListenIPHosts(new IPHost[] { new IPHost(7789) })
    .ConfigureContainer(a =>
    {
        a.SetSingletonLogger<ConsoleLogger>();
        a.RegisterSingleton<HttpService<MyTcpClient>>(service);
    })
    .SetClearInterval(10 * 60 * 1000)
    .ConfigureRpcStore(a =>
    {
        a.RegisterServer<MyServer>();
    })
    .ConfigurePlugins(a =>
    {
        a.Add<WebApiParserPlugin>();
        a.Add<WebSocketServerPlugin>()//添加WebSocket功能
               .SetWSUrl("/ws")
               .SetCallback(WSCallback);//WSCallback回调函数是在WS收到数据时触发回调的。
        a.Add<MyWebSocketPlugin>();
    }))
    .Start();
Container container = new Container();
container.RegisterSingleton<HttpService<MyTcpClient>>();
void WSCallback(ITcpClientBase client, WSDataFrameEventArgs e)
{
    if (client is MyTcpClient socketClient && socketClient.Service is HttpService<MyTcpClient> service)
    {
        var clients = service.GetClients();
        foreach (var item in clients)
        {
            if (item.ID == socketClient.ID) continue;//不发送给自己
            if (item.UserId != socketClient.UserId) continue;//不发送给其他人

            item.SendWithWS(e.DataFrame.ToText());
        }
    }
}


var back = Console.BackgroundColor;
var forCol = Console.ForegroundColor;
Console.CursorVisible = false;
Console.ForegroundColor = back;
ConsoleAction consoleAction = new ConsoleAction("h|help|?");//设置帮助命令
Console.Clear();
Console.ForegroundColor = forCol;
Console.CursorVisible = true;
consoleAction.Add("li|list", "查看列表", () =>
{
    var list = service.GetClients();
    var byList = list.GroupBy(m => m.UserId).ToList();
    if(byList.Count == 0)
    {
        Console.WriteLine("一个连接都木有");
    }
    foreach (var byItem in byList)
    {
        Console.WriteLine("唯一标签：" + byItem.Key);
        foreach (var item in byItem)
        {
            Console.WriteLine("\tIp:" + item.IP + ":" + item.Port);
        }
    }
});
consoleAction.OnException += ex =>
{
    Console.WriteLine("错误：" + ex.Message);
};//订阅执行异常输出
//consoleAction.Add("sp|shareProxy", "分享代理", ShareProxy);//示例命令
//consoleAction.Add("ssp|stopShareProxy", "停止分享代理", StopShareProxy);//示例命令
//consoleAction.Add("ga|getAll", "获取所有客户端信息", GetAll);//示例命令
Console.WriteLine("Ws服务器已启动");
Console.WriteLine("请输入“h|help|?”获得帮助。");
while (true)
{
    if (!consoleAction.Run(Console.ReadLine()))
    {
        Console.WriteLine("命令不正确，请输入“h|help|?”获得帮助。");
    }
}