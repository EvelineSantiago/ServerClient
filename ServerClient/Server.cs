using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SD
{
    public class Server<T> : ServerBase
    {
        protected List<T> Data = new();

        [Request(Port = 2)]
        public virtual void ReadRequest(Socket handler, CancellationToken cancellationToken)
        {
            Console.WriteLine("Connected");

            string encodedString = JsonSerializer.Serialize(Data, RequestConfig.JsonOptions);
            if (cancellationToken.IsCancellationRequested) return;
            handler.Send(Encoding.UTF8.GetBytes(encodedString));
            handler.Close();
            Console.WriteLine("Sent");
        }

        [Request(Port = 1)]
        public virtual void WriteRequest(Socket handler, CancellationToken cancellationToken)
        {
            string response = "";
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) return;
                byte[] buffer = new byte[1024];
                if (handler.Available == 0) break;
                int bytes = handler.Receive(buffer);
                Console.WriteLine(bytes);
                response += Encoding.UTF8.GetString(buffer, 0, bytes);
            }
            Console.WriteLine(response);
            List<T>? data = JsonSerializer.Deserialize<List<T>>(response, RequestConfig.JsonOptions);
            if (data == null) return;
            if (cancellationToken.IsCancellationRequested) return;
            Data.AddRange(data);
            Console.WriteLine(typeof(T).Name + " adicionadas: " + data.Count.ToString());
            handler.Send(Encoding.UTF8.GetBytes("Data received"));
            handler.Close();
        }
    }

    public class ServerBase
    {
        class ServerParams
        {
            public required Type? SystemType;
            public required MethodInfo Method;
        }
        public int Timeout = -1;

        public void InstanceEndpoint(object? param)
        {
            ServerParams serverParams = (ServerParams)param!;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            string ip = host.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString();

            int port = serverParams.SystemType == null ?
                RequestConfig.GetRequestPort(serverParams.Method.Name, GetType()) :
                RequestConfig.GetRequestPort(serverParams.Method.Name, GetType(), serverParams.SystemType);
            IPEndPoint localEndPoint = new(IPAddress.Parse(ip), port);
            Console.WriteLine(GetType().Name + serverParams.Method.Name + " endpoint at " + localEndPoint);

            Socket server = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(localEndPoint);
            server.Listen();

            CancellationTokenSource cts = new(Timeout);
            HandleConnect(server, serverParams.Method, cts.Token);
        }

        public void HandleConnect(Socket server, MethodInfo method, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) { return; }
                try
                {
                    var res = server.AcceptAsync(cancellationToken).AsTask();
                    res.Wait(cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;
                    if (!res.IsCompletedSuccessfully) return;

                    Socket handler = res.Result;
                    Thread thread = new(() => method.Invoke(this, new object?[] { handler, cancellationToken }));
                    thread.Start();
                }
                catch
                {
                    Console.WriteLine(GetType().Name + " timed out");
                    return;
                }
            }
        }

        public void Setup(Type? systemType = null)
        {
            RequestConfig.ResolveRequestMethods((method) =>
            {
                try
                {
                    Thread endpoint = new(new ParameterizedThreadStart(InstanceEndpoint));
                    endpoint.Start(new ServerParams() { Method = method, SystemType = systemType });
                }
                catch (Exception e) { Console.WriteLine("erro"); Console.WriteLine(e); return; }
            }, GetType());
        }
    }
}