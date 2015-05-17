﻿// This file is part of Hangfire.
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
using System.Collections.Generic;
using System.Linq;
using Hangfire.Common;

namespace Hangfire.Client
{
    /// <summary>
    /// Provides the context for the <see cref="IClientFilter.OnCreating"/>
    /// method of the <see cref="IClientFilter"/> interface.
    /// </summary>
    public class CreatingContext : CreateContext
    {
        private readonly IDictionary<string, string> _parameters
            = new Dictionary<string, string>();

        public CreatingContext(CreateContext context)
            : base(context)
        {
        }

        public IDictionary<string, string> Parameters
        {
            get
            {
                return _parameters.ToDictionary(x => x.Key, x => x.Value);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="CreatingContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Sets the job parameter of the specified <paramref name="name"/>
        /// to the corresponding <paramref name="value"/>. The value of the
        /// parameter is being serialized to a JSON string.
        /// </summary>
        /// 
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// 
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is null or empty.</exception>
        public virtual void SetJobParameter(string name, object value)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            var serializedValue = JobHelper.ToJson(value);

            if (!_parameters.ContainsKey(name))
            {
                _parameters.Add(name, serializedValue);
            }
            else
            {
                _parameters[name] = serializedValue;
            }
        }

        /// <summary>
        /// Gets the job parameter of the specified <paramref name="name"/>
        /// if it exists. The parameter is being deserialized from a JSON 
        /// string value to the given type <typeparamref name="T"/>.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>The value of the given parameter if it exists or null otherwise.</returns>
        /// 
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is null or empty.</exception>
        /// <exception cref="NotSupportedException">Could not deserialize the parameter value to the type <typeparamref name="T"/>.</exception>
        public T GetJobParameter<T>(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");

            try
            {
                return _parameters.ContainsKey(name)
                    ? JobHelper.FromJson<T>(_parameters[name])
                    : default(T);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(String.Format(
                    "Could not get a value of the job parameter `{0}`. See inner exception for details.",
                    name), ex);
            }
        }
    }
}