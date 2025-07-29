// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Storage;

namespace Hangfire.SqlServer
{
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This interface represents a persistent queue by design.")]
    public interface IPersistentJobQueue
    {
        [NotNull]
        IFetchedJob Dequeue([NotNull] string[] queues, CancellationToken cancellationToken);

#if FEATURE_TRANSACTIONSCOPE
        void Enqueue([NotNull] System.Data.IDbConnection connection, [NotNull] string queue, [NotNull] string jobId);
#else
        void Enqueue(
            [NotNull] System.Data.Common.DbConnection connection, 
            [CanBeNull] System.Data.Common.DbTransaction? transaction, 
            [NotNull] string queue, 
            [NotNull] string jobId);
#endif
    }
}