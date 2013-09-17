using System.Collections.Generic;

namespace HangFire.Client
{
    public class ClientContext
    {
        public ClientContext()
        {
            Items = new Dictionary<string, object>();
        }

        public ClientContext(ClientContext clientContext)
            : this()
        {
        }

        public IDictionary<string, object> Items { get; private set; }
    }
}