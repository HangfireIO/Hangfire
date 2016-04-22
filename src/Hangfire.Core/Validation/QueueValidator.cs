
using System;
using System.Text.RegularExpressions;

namespace Hangfire.Validation
{
    public static class QueueValidator
    {
        public static void ValidateName(string queueName)
        {
            const string parameterName = "queue";

            if (String.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException(parameterName);
            }

            if (!Regex.IsMatch(queueName, @"^[a-z0-9_]+$"))
            {
                throw new ArgumentException(
                    String.Format(
                        "The queue name must consist of lowercase letters, digits and underscore characters only. Given: '{0}'.",
                        queueName),
                    parameterName);
            }
        }
    }
}
