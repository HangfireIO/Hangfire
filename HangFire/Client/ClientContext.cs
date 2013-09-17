using System.Collections.Generic;

namespace HangFire.Client
{
    public class ClientContext
    {
        public ClientContext()
        {
            Items = new Dictionary<string, object>();
        }

        public IDictionary<string, object> Items { get; private set; }
    }
}