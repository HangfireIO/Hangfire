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
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Common;
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