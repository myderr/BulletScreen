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
service.Setup(new TouchSocketConfig()//��������
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
        a.Add<WebSocketServerPlugin>()//���WebSocket����
               .SetWSUrl("/ws")
               .SetCallback(WSCallback);//WSCallback�ص���������WS�յ�����ʱ�����ص��ġ�
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
            if (item.ID == socketClient.ID) continue;//�����͸��Լ�
            if (item.UserId != socketClient.UserId) continue;//�����͸�������

            item.SendWithWS(e.DataFrame.ToText());
        }
    }
}


var back = Console.BackgroundColor;
var forCol = Console.ForegroundColor;
Console.CursorVisible = false;
Console.ForegroundColor = back;
ConsoleAction consoleAction = new ConsoleAction("h|help|?");//���ð�������
Console.Clear();
Console.ForegroundColor = forCol;
Console.CursorVisible = true;
consoleAction.Add("li|list", "�鿴�б�", () =>
{
    var list = service.GetClients();
    var byList = list.GroupBy(m => m.UserId).ToList();
    if(byList.Count == 0)
    {
        Console.WriteLine("һ�����Ӷ�ľ��");
    }
    foreach (var byItem in byList)
    {
        Console.WriteLine("Ψһ��ǩ��" + byItem.Key);
        foreach (var item in byItem)
        {
            Console.WriteLine("\tIp:" + item.IP + ":" + item.Port);
        }
    }
});
consoleAction.OnException += ex =>
{
    Console.WriteLine("����" + ex.Message);
};//����ִ���쳣���
//consoleAction.Add("sp|shareProxy", "�������", ShareProxy);//ʾ������
//consoleAction.Add("ssp|stopShareProxy", "ֹͣ�������", StopShareProxy);//ʾ������
//consoleAction.Add("ga|getAll", "��ȡ���пͻ�����Ϣ", GetAll);//ʾ������
Console.WriteLine("Ws������������");
Console.WriteLine("�����롰h|help|?����ð�����");
while (true)
{
    if (!consoleAction.Run(Console.ReadLine()))
    {
        Console.WriteLine("�����ȷ�������롰h|help|?����ð�����");
    }
}