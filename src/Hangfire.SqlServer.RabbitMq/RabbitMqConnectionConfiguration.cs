using System;
using RabbitMQ.Client;

namespace Hangfire.SqlServer.RabbitMQ
{
    public class RabbitMqConnectionConfiguration
    {
        public const string DefaultHost = "localhost";
        public const int DefaultPort = AmqpTcpEndpoint.UseDefaultPort;
        public const string DefaultUser = "guest";
        public const string DefaultPassword = "guest";
        public const string DefaultVirtualHost = "/";

        public RabbitMqConnectionConfiguration()
            : this(DefaultHost, DefaultPort, DefaultUser, DefaultPassword)
        {
        }

        public RabbitMqConnectionConfiguration(string host)
            : this(host, DefaultPort, DefaultUser, DefaultPassword)
        {
        }

        public RabbitMqConnectionConfiguration(string host, int port)
            : this(host, port, DefaultUser, DefaultPassword)
        {
        }

        public RabbitMqConnectionConfiguration(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException("uri");

            Uri = uri;
        }

        public RabbitMqConnectionConfiguration(string host, int port, string username, string password)
        {
            if (host == null) throw new ArgumentNullException("host");
            if (username == null) throw new ArgumentNullException("username");
            if (password == null) throw new ArgumentNullException("password");

            HostName = host;
            Username = username;
            Password = password;
            Port = port;
            VirtualHost = DefaultVirtualHost;
        }

        public string Username { get; set; }

        public string Password { get; set; }

        public string HostName { get; set; }

        public string VirtualHost { get; set; }

        public int Port { get; set; }

        public Uri Uri { get; set; }
    }
}