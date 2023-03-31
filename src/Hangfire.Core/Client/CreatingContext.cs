// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

namespace Hangfire.Client
{
    /// <summary>
    /// Provides the context for the <see cref="IClientFilter.OnCreating"/>
    /// method of the <see cref="IClientFilter"/> interface.
    /// </summary>
    public class CreatingContext : CreateContext
    {
        public CreatingContext(CreateContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="CreatingContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Sets the job parameter of the specified <paramref name="name"/>
        /// to the corresponding <paramref name="value"/>. The value of the
        /// parameter is serialized to a JSON string.
        /// </summary>
        /// 
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// 
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is null or empty.</exception>
        public void SetJobParameter(string name, object value)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            Parameters[name] = value;
        }

        /// <summary>
        /// Gets the job parameter of the specified <paramref name="name"/>
        /// if it exists. The parameter is deserialized from a JSON 
        /// string value to the given type <typeparamref name="T"/>.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>The value of the given parameter if it exists or null otherwise.</returns>
        /// 
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Could not deserialize the parameter value to the type <typeparamref name="T"/>.</exception>
        public T GetJobParameter<T>(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            try
            {
                return Parameters.ContainsKey(name)
                    ? (T)Parameters[name]
                    : default(T);
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                throw new InvalidOperationException(
                    $"Could not get a value of the job parameter `{name}`. See inner exception for details.", ex);
            }
        }
    }
}