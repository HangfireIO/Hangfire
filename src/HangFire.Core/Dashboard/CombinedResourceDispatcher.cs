using System.Reflection;
using HangFire.Annotations;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    internal class CombinedResourceDispatcher : EmbeddedResourceDispatcher
    {
        private readonly Assembly _assembly;
        private readonly string _baseNamespace;
        private readonly string[] _resourceNames;

        public CombinedResourceDispatcher(
            [NotNull] string contentType, 
            [NotNull] Assembly assembly, 
            string baseNamespace, 
            params string[] resourceNames) : base(contentType, assembly, null)
        {
            _assembly = assembly;
            _baseNamespace = baseNamespace;
            _resourceNames = resourceNames;
        }

        protected override void WriteResponse(IOwinResponse response)
        {
            foreach (var resourceName in _resourceNames)
            {
                WriteResource(
                    response,
                    _assembly,
                    string.Format("{0}.{1}", _baseNamespace, resourceName));
            }
        }
    }
}
