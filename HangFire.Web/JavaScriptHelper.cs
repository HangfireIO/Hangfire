using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace HangFire.Web
{
    class JavaScriptHelper
    {
        static string _styleSheetHash;
        static readonly ReadOnlyCollection<string> _javaScriptResourceNames
            = Array.AsReadOnly(new[] { "jquery-1.8.2.min.js", "hangfire.js" });

        public static string JavaScriptHash
        {
            get { return _styleSheetHash ?? (_styleSheetHash = CalculateHash()); }
        }

        public static IEnumerable<string> JavaScriptResourceNames
        {
            get { return _javaScriptResourceNames; }
        }

        private static string CalculateHash()
        {
            var memoryStream = new MemoryStream();
            foreach (var resourceName in _javaScriptResourceNames)
                ManifestResourceHelper.WriteResourceToStream(memoryStream, resourceName);

            return MD5.Create()
                      .ComputeHash(memoryStream)
                      .Select(b => b.ToString("x2"))
                      .ToDelimitedString(string.Empty);
        }
    }
}
