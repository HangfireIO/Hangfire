using System;

namespace Hangfire.Common
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Returns the deserialized JSON <paramref name="text"/> if the text is a serialized JSON string,
        /// or the specified text without any transformation if the text is not a serialized JSON string.
        /// </summary>
        /// <param name="text">The text to prettify.</param>
        public static string PrettyJsonString(this string text)
        {
            if (text.StartsWith("\"", StringComparison.Ordinal))
            {
                try
                {
                    return SerializationHelper.Deserialize<string>(text, SerializationOption.User);
                }
                catch
                {
                    // Ignore
                }
            }

            return text;
        }
    }
}