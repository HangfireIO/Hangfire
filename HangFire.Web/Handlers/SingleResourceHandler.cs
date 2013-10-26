using System.Reflection;

namespace HangFire.Web
{
    internal class SingleResourceHandler : EmbeddedResourceHandler
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;

        public SingleResourceHandler(Assembly assembly, string resourceName)
        {
            _assembly = assembly;
            _resourceName = resourceName;
        }

        protected override void WriteResponse()
        {
            WriteResource(_assembly, _resourceName);
        }
    }
}
