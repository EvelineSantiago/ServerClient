using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SD
{
    public class Client
    {
        public Type ServerType;
        public Type? SystemType;
        public Client(Type serverType, Type systemType)
        {
            ServerType = serverType;
            SystemType = systemType;
        }
        public Client(Type serverType)
        {
            ServerType = serverType;
        }
        public void MakeRequest(string method, string? message = null)
        {
            int port;

            if (SystemType == null)
                port = RequestConfig.GetRequestPort(method, ServerType);
            else
                port = RequestConfig.GetRequestPort(method, ServerType, SystemType);

            InternMakeRequest(port, message);
        }

        private static void InternMakeRequest(int port, string? message)
        {
            IPEndPoint ipEndPoint = new(IPAddress.Parse("192.168.100.95"), port);
            using Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            client.Connect(ipEndPoint);

            if (message != null)
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                int bytes = client.Send(messageBytes);
            }

            string response = "";
            while (true)
            {
                byte[] buffer = new byte[1024];
                int resBytes = client.Receive(buffer);
                if (resBytes == 0) break;
                response += Encoding.UTF8.GetString(buffer, 0, resBytes);
            }

            Console.WriteLine(response);
            client.Close();
        }

        public void MakeRequestUdp()
        {
            // int port = RequestConfig.GetRequestPort(method, typeof(Udp<Pessoa>));
            var host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress localIp = IPAddress.Parse(host.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork).ToString());
            EndPoint localEndpoint = new IPEndPoint(localIp, 1);
            using Socket client = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            client.Bind(localEndpoint);
            MulticastOption multicastOption = new(IPAddress.Parse("224.168.100.2"), localIp);
            client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);

            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            Console.WriteLine(remoteIp);
            Console.WriteLine(localEndpoint);
            string response = "";
            while (true)
            {
                byte[] buffer = new byte[1024];
                Console.WriteLine("enter");
                int resBytes = client.ReceiveFrom(buffer, ref remoteIp);
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
                if (resBytes == 0) break;
                response += Encoding.UTF8.GetString(buffer, 0, resBytes);
            }

            Console.WriteLine(response);
            client.Close();
            return;
        }
    }
}