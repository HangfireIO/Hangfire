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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Hangfire.Annotations;

namespace Hangfire.Client
{
    /// <summary>
    /// Provides the context for the <see cref="IClientFilter.OnCreated"/> 
    /// method of the <see cref="IClientFilter"/> interface.
    /// </summary>
    public class CreatedContext : CreateContext
    {
        public CreatedContext(
            [NotNull] CreateContext context, 
            [CanBeNull] BackgroundJob backgroundJob,
            bool canceled, 
            [CanBeNull] Exception exception)
            : base(context)
        {
            BackgroundJob = backgroundJob;
            Canceled = canceled;
            Exception = exception;
        }

        [CanBeNull]
        [Obsolete("Please use `BackgroundJob` property instead. Will be removed in 2.0.0.")]
        public string JobId => BackgroundJob?.Id;

        [CanBeNull]
        public BackgroundJob BackgroundJob { get; }
        
        public override IDictionary<string, object> Parameters => new ReadOnlyDictionary<string, object>(base.Parameters);

        /// <summary>
        /// Gets an exception that occurred during the creation of the job.
        /// </summary>
        [CanBeNull]
        public Exception Exception { get; }

        /// <summary>
        /// Gets a value that indicates that this <see cref="CreatedContext"/>
        /// object was canceled.
        /// </summary>
        public bool Canceled { get; }

        /// <summary>
        /// Gets or sets a value that indicates that this <see cref="CreatedContext"/>
        /// object handles an exception occurred during the creation of the job.
        /// </summary>
        public bool ExceptionHandled { get; set; }

        public void SetJobParameter([NotNull] string name, object value)
        {
            if (String.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            throw new InvalidOperationException("Could not set parameter for a created job.");
        }
    }
}