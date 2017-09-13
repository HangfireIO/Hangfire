using System;
using System.Globalization;
using System.Resources;

namespace Hangfire
{
    /// <summary>
    /// Specifies a display name for a job method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class JobDisplayNameAttribute : Attribute
    {
        private readonly Lazy<ResourceManager> _resourceManager;

        public JobDisplayNameAttribute(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                throw new ArgumentException("Display name is empty", nameof(displayName));

            DisplayName = displayName;
            _resourceManager = new Lazy<ResourceManager>(() => InitResourceManager(ResourceType), true);
        }

        /// <summary>
        /// Gets display name for the job.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets or sets resource type to localize <see cref="DisplayName"/> string.
        /// </summary>
        public Type ResourceType { get; set; }

        private static ResourceManager InitResourceManager(Type type)
        {
            if (type == null) return null;

            return new ResourceManager(type);
        }
        
        internal string Format(params object[] arguments)
        {
            var format = _resourceManager.Value?.GetString(DisplayName, CultureInfo.CurrentUICulture);
            if (string.IsNullOrEmpty(format))
            {
                // failed to localize display name string, use it as is
                format = DisplayName;
            }
            
            return string.Format(CultureInfo.CurrentCulture, format, arguments);
        }
    }
}
