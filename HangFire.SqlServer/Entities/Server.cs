using System;

namespace HangFire.SqlServer.Entities
{
    class Server
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public DateTime? LastHeartbeat { get; set; }
    }
}
