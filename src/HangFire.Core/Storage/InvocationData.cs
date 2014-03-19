namespace HangFire.Storage
{
    public class InvocationData
    {
        public InvocationData(string type, string method, string parameterTypes)
        {
            Type = type;
            Method = method;
            ParameterTypes = parameterTypes;
        }

        public string Type { get; private set; }
        public string Method { get; private set; }
        public string ParameterTypes { get; private set; }
    }
}
