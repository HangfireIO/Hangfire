using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace HangFire.Web
{
    public static class StyleSheetHelper
    {
        static string _styleSheetHash;
        static readonly ReadOnlyCollection<string> _styleSheetResourceNames 
            = Array.AsReadOnly(new[] { "bootstrap.min.css", "hangfire.css" });

        public static string StyleSheetHash
        {
            get { return _styleSheetHash ?? (_styleSheetHash = CalculateHash()); }
        }

        public static IEnumerable<string> StyleSheetResourceNames
        {
            get { return _styleSheetResourceNames; }
        }

        private static string CalculateHash()
        {
            var memoryStream = new MemoryStream();
            foreach (var resourceName in _styleSheetResourceNames)
                ManifestResourceHelper.WriteResourceToStream(memoryStream, resourceName);

            return MD5.Create()
                      .ComputeHash(memoryStream)
                      .Select(b => b.ToString("x2"))
                      .ToDelimitedString(string.Empty);
        }
    }
}
