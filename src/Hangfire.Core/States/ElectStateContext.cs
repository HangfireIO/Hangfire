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
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Profiling;
using Hangfire.Storage;

namespace Hangfire.States
{
#pragma warning disable 618
    public class ElectStateContext : StateContext
#pragma warning restore 618
    {
        private readonly IList<IState> _traversedStates = new List<IState>();
        private IState _candidateState;

        public ElectStateContext([NotNull] ApplyStateContext applyContext)
        {
            if (applyContext == null) throw new ArgumentNullException(nameof(applyContext));
            
            BackgroundJob = applyContext.BackgroundJob;
            _candidateState = applyContext.NewState;

            Storage = applyContext.Storage;
            Connection = applyContext.Connection;
            Transaction = applyContext.Transaction;
            CurrentState = applyContext.OldStateName;
            Profiler = applyContext.Profiler;
        }
        
        public override BackgroundJob BackgroundJob { get; }

        [NotNull]
        public JobStorage Storage { get; }

        [NotNull]
        public IStorageConnection Connection { get; }

        [NotNull]
        public IWriteOnlyTransaction Transaction { get; }

        [NotNull]
        public IState CandidateState
        {
            get { return _candidateState; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "The CandidateState property can not be set to null.");
                }

                if (_candidateState != value)
                {
                    _traversedStates.Add(_candidateState);
                    _candidateState = value;
                }
            }
        }

        [CanBeNull]
        public string CurrentState { get; private set; }

        [NotNull]
        public IState[] TraversedStates => _traversedStates.ToArray();

        [NotNull]
        internal IProfiler Profiler { get; }

        public void SetJobParameter<T>(string name, T value)
        {
            Connection.SetJobParameter(BackgroundJob.Id, name, SerializationHelper.Serialize(value, SerializationOption.User));
        }

        public T GetJobParameter<T>(string name)
        {
            return SerializationHelper.Deserialize<T>(
                Connection.GetJobParameter(BackgroundJob.Id, name),
                SerializationOption.User);
        }
    }
}