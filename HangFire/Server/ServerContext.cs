namespace HangFire.Server
{
    public class ServerContext
    {
        internal ServerContext(ServerContext context)
            : this(context.ServerName, context.Activator, context.Performer)
        {
        }

        internal ServerContext(
            string serverName,
            JobActivator activator,
            JobPerformer performer)
        {
            ServerName = serverName;

            Activator = activator;
            Performer = performer;
        }

        public string ServerName { get; private set; }

        internal JobActivator Activator { get; private set; }
        internal JobPerformer Performer { get; private set; }
    }
}
