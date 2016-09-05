using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.EmailNotification
{
    public class SmtpConfiguration
    {
        public string Host{ get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public int Port { get; private set; }

        public SmtpConfiguration(string host, string username, string password, int port)
        {
            Host = host;
            Username = username;
            Password = password;
            Port = port;
        }
    }
}
