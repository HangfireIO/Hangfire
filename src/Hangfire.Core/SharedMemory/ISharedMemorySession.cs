// This file is part of Hangfire.
// Copyright © 2018 Sergey Odinokov.
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

namespace Hangfire.SharedMemory
{
    public interface ISharedMemorySession
    {
        Guid SessionId { get; }

        string Type { get; }
        byte[] Data { get; }

        TimeSpan Timeout { get; }
        TimeSpan Deadline { get; }

        DateTime HeartbeatAt { get; }
        TimeSpan HeartbeatAgo { get; }
        DateTime? RemoveAt { get; }
        TimeSpan? RemoveIn { get; }
    }
}