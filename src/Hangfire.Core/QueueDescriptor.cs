// This file is part of Hangfire.
// Copyright © 2026 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.

using System;
using Hangfire.States;

namespace Hangfire
{
    public sealed class QueueDescriptor
    {
        public QueueDescriptor(string name, int priority)
        {
            EnqueuedState.ValidateQueueName(nameof(name), name);
            if (priority <= 0) throw new ArgumentOutOfRangeException(nameof(priority), "Queue priority must be a positive integer.");

            Name = name;
            Priority = priority;
        }

        public string Name { get; }

        public int Priority { get; }
    }
}
