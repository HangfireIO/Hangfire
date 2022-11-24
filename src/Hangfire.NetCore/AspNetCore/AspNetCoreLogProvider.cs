﻿// This file is part of Hangfire. Copyright © 2016 Hangfire OÜ.
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
using Hangfire.Annotations;
using Hangfire.Logging;
using Microsoft.Extensions.Logging;

namespace Hangfire.AspNetCore
{
    public class AspNetCoreLogProvider : ILogProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        public AspNetCoreLogProvider([NotNull] ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _loggerFactory = loggerFactory;
        }

        public ILog GetLogger(string name)
        {
            return new AspNetCoreLog(_loggerFactory.CreateLogger(name));
        }
    }
}