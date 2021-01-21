// This file is part of Hangfire.
// Copyright © 2020 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Text;
using Newtonsoft.Json;

namespace Hangfire
{
    public class ExceptionInfo
    {
        public ExceptionInfo(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            Message = exception.Message;
            Type = exception.GetType();

            if (exception.InnerException != null)
            {
                InnerException = new ExceptionInfo(exception.InnerException);
            }
        }

        [JsonConstructor]
        public ExceptionInfo(Type type, string message, ExceptionInfo innerException)
        {
            Type = type;
            Message = message;
            InnerException = innerException;
        }

        [JsonProperty("t")]
        public Type Type { get; }

        [JsonProperty("m", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; }

        [JsonProperty("i", NullValueHandling = NullValueHandling.Ignore)]
        public ExceptionInfo InnerException { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Type.FullName);
            sb.Append(": ");
            sb.Append(Message);

            if (InnerException != null)
            {
                sb.Append(" ---> ");
                sb.Append(InnerException.ToString());
            }
            else sb.Append("\n");

            return sb.ToString();
        }
    }
}
