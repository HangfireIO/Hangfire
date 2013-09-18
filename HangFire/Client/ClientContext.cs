using System.Collections.Generic;

namespace HangFire.Client
{
    public class ClientContext
    {
        internal ClientContext()
        {
            Items = new Dictionary<string, object>();
        }

        internal ClientContext(ClientContext clientContext)
        {
            Items = clientContext.Items;
        }

        public IDictionary<string, object> Items { get; private set; }
    }
}