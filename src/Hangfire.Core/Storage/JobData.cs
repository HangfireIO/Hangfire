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
using System.Collections.Generic;
using Hangfire.Common;

namespace Hangfire.Storage
{
    public class JobData
    {
        public string State { get; set; }
        public Job Job { get; set; }
        public InvocationData InvocationData { get; set; }
        public DateTime CreatedAt { get; set; }
        public IReadOnlyDictionary<string, string> ParametersSnapshot { get; set; }

        public JobLoadException LoadException { get; set; }

        public void EnsureLoaded()
        {
            if (LoadException != null)
            {
                throw LoadException;
            }
        }
    }
}