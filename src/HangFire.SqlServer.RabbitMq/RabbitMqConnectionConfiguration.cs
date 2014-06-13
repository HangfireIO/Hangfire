using RabbitMQ.Client;

namespace HangFire.SqlServer.RabbitMQ
{
    public class RabbitMqConnectionConfiguration
    {
        public const int DefaultPort = AmqpTcpEndpoint.UseDefaultPort;
        public const string DefaultUser = "guest";
        public const string DefaultPassword = "guest";
        public const string DefaultVirtualHost = "/";

        public RabbitMqConnectionConfiguration()
            : this("localhost", DefaultPort, DefaultUser, DefaultPassword)
        {
            VirtualHost = DefaultVirtualHost;
        }

        public RabbitMqConnectionConfiguration(string host)
            : this(host, DefaultPort, DefaultUser, DefaultPassword)
        {
            HostName = host;
        }

        public RabbitMqConnectionConfiguration(string host, int port)
            : this(host, port, DefaultUser, DefaultPassword)
        {
            HostName = host;
        }

        public RabbitMqConnectionConfiguration(string host, int port, string userName, string password)
        {
            HostName = host;
            UserName = userName;
            Password = password;
            Port = port;
            VirtualHost = DefaultVirtualHost;
        }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string HostName { get; set; }

        public string VirtualHost { get; set; }

        public int Port { get; set; }
    }
}