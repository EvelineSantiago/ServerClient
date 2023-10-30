using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SD
{
    public class Client
    {
        public Type ServerType;
        public Type? SystemType = null;
        public Client(Type serverType, Type systemType)
        {
            ServerType = serverType;
            SystemType = systemType;
        }
        public Client(Type serverType)
        {
            ServerType = serverType;
        }
        public string MakeRequest(string method, string? message = null)
        {
            int port;

            if (SystemType == null)
                port = RequestConfig.GetRequestPort(method, ServerType);
            else
                port = RequestConfig.GetRequestPort(method, ServerType, SystemType);

            return InternMakeRequest(port, message);
        }

        private static string InternMakeRequest(int port, string? message)
        {
            IPEndPoint ipEndPoint = new(IPAddress.Parse("192.168.100.125"), port);
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
            return response;
        }
    }
}