using System.Reflection;

namespace HangFire.Web
{
    public class CombinedResourceHandler : EmbeddedResourceHandler
    {
        private readonly Assembly _assembly;
        private readonly string _baseNamespace;
        private readonly string[] _resourceNames;

        public CombinedResourceHandler(
            Assembly assembly,
            string baseNamespace, 
            params string[] resourceNames)
        {
            _assembly = assembly;
            _baseNamespace = baseNamespace;
            _resourceNames = resourceNames;
        }

        protected override void WriteResponse()
        {
            foreach (var resourceName in _resourceNames)
            {
                WriteResource(
                    _assembly,
                    string.Format("{0}.{1}", _baseNamespace, resourceName));
            }
        }
    }
}
