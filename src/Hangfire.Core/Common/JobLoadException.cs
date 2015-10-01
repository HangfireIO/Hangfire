// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
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
using System.Runtime.Serialization;

namespace Hangfire.Common
{
    /// <summary>
    /// The exception that is thrown when a job could not
    /// be loaded from the storage due to missing or incorrect 
    /// information about its type or method.
    /// </summary>
    public class JobLoadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobLoadException"/>
        /// class with a given message and information about inner exception.
        /// </summary>
        public JobLoadException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}