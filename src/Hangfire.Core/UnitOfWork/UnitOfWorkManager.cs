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

namespace Hangfire.UnitOfWork
{
    public class UnitOfWorkManager : IUnitOfWorkManager
    {
        private static IUnitOfWorkManager _current = new UnitOfWorkManager();

        /// <summary>
        /// Gets or sets the current <see cref="IUnitOfWorkManager"/> instance 
        /// that will be used to manage unit of work context during job processing.
        /// </summary>
        public static IUnitOfWorkManager Current
        {
            get { return _current; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _current = value;
            }
        }

        public object Begin()
        {
            return new object();
        }

        public void End(object context, Exception ex = null)
        {
            return;
        }
    }
}