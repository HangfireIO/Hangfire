using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire
{
    public static class Net40CompatibilityHelper
    {
        /// <summary>
        /// Adds Timeout.InfiniteTimeSpan which is not available in .NET 4.0 
        /// </summary>
        public static class Timeout
        {
            public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, -1);
        }
        /// <summary>
        /// Calls Task.FromResult in .NET 4.5 or replicates this method in .NET 4.0
        /// </summary>
        public static class Task
        {
            public static Task<T> FromResult<T>(T value)
            {
#if NET45
                return System.Threading.Tasks.Task.FromResult(value);
#else
                var tcs = new TaskCompletionSource<T>();
                tcs.SetResult(value);
                return tcs.Task;
#endif
            }
        }
#if NET40
        /// <summary>
        /// Extension method to add OwinRequest.ReadFormAsync which is not available in .NET 4.0
        /// </summary>
        public static async Task<IFormCollection> ReadFormAsync(this IOwinRequest owinRequest)
        {
            var form = owinRequest.Get<IFormCollection>("Microsoft.Owin.Form#collection");
            if (form == null)
            {
                string text;
                using (var reader = new StreamReader(owinRequest.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4 * 1024))
                {
                    text = await reader.ReadToEndAsync();
                }
                form = OwinHelpers.GetForm(text);
                owinRequest.Set("Microsoft.Owin.Form#collection", form);
            }

            return form;
        }
        private class OwinHelpers
        {
            internal static IFormCollection GetForm(string text)
            {
                IDictionary<string, string[]> form = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                ParseDelimited(text, new[] { '&' }, AppendItemCallback, accumulator);
                foreach (var kv in accumulator)
                {
                    form.Add(kv.Key, kv.Value.ToArray());
                }
                return new FormCollection(form);
            }

            private static readonly Action<string, string, object> AppendItemCallback = (name, value, state) =>
            {
                var dictionary = (IDictionary<string, List<String>>)state;

                List<string> existing;
                if (!dictionary.TryGetValue(name, out existing))
                {
                    dictionary.Add(name, new List<string>(1) { value });
                }
                else
                {
                    existing.Add(value);
                }
            };
            internal static void ParseDelimited(string text, char[] delimiters, Action<string, string, object> callback, object state)
            {
                int textLength = text.Length;
                int equalIndex = text.IndexOf('=');
                if (equalIndex == -1)
                {
                    equalIndex = textLength;
                }
                int scanIndex = 0;
                while (scanIndex < textLength)
                {
                    int delimiterIndex = text.IndexOfAny(delimiters, scanIndex);
                    if (delimiterIndex == -1)
                    {
                        delimiterIndex = textLength;
                    }
                    if (equalIndex < delimiterIndex)
                    {
                        while (scanIndex != equalIndex && char.IsWhiteSpace(text[scanIndex]))
                        {
                            ++scanIndex;
                        }
                        string name = text.Substring(scanIndex, equalIndex - scanIndex);
                        string value = text.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                        callback(
                            Uri.UnescapeDataString(name.Replace('+', ' ')),
                            Uri.UnescapeDataString(value.Replace('+', ' ')),
                            state);
                        equalIndex = text.IndexOf('=', delimiterIndex);
                        if (equalIndex == -1)
                        {
                            equalIndex = textLength;
                        }
                    }
                    scanIndex = delimiterIndex + 1;
                }
            }
        }
#endif
    }
}
