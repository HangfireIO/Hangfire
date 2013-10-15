namespace HangFire.Server
{
    internal class JobPayload
    {
        public JobPayload(
            string id, string queue, string type, string args)
        {
            Id = id;
            Queue = queue;
            Type = type;
            Args = args;
        }

        public string Id { get; private set; }
        public string Queue { get; private set; }
        public string Type { get; private set; }
        public string Args { get; private set; }
    }
}
