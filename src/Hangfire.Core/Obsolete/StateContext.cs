// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using System;
using Hangfire.Annotations;
using Hangfire.Common;

// ReSharper disable once CheckNamespace
namespace Hangfire.States
{
    /// <exclude />
    [Obsolete("This class is here for compatibility reasons. Will be removed in 2.0.0.")]
    public abstract class StateContext
    {
        [NotNull]
        public abstract BackgroundJob BackgroundJob { get; }

        [NotNull]
        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public string JobId => BackgroundJob.Id;

        [CanBeNull]
        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public Job Job => BackgroundJob.Job;

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public DateTime CreatedAt => BackgroundJob.CreatedAt;
    }
}