#region License and Terms
// MoreLINQ - Extensions to LINQ to Objects
// Copyright (c) 2008 Jonathan Skeet. All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

namespace MoreLinq
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    static partial class MoreEnumerable
    {
        /// <summary>
        /// Returns a sequence resulting from applying a function to each 
        /// element in the source sequence and its 
        /// predecessor, with the exception of the first element which is 
        /// only returned as the predecessor of the second element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the element of the returned sequence.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="resultSelector">A transform function to apply to 
        /// each pair of sequence.</param>
        /// <returns>
        /// Returns the resulting sequence.
        /// </returns>
        /// <remarks>
        /// This operator uses deferred execution and streams its results.
        /// </remarks>
        /// <example>
        /// <code>
        /// int[] numbers = { 123, 456, 789 };
        /// IEnumerable&lt;int&gt; result = numbers.Pairwise(5, (a, b) => a + b);
        /// </code>
        /// The <c>result</c> variable, when iterated over, will yield 
        /// 579 and 1245, in turn.
        /// </example>
        public static IEnumerable<TResult> Pairwise<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            return PairwiseImpl(source, resultSelector);
        }

        private static IEnumerable<TResult> PairwiseImpl<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
        {
            Debug.Assert(source != null);
            Debug.Assert(resultSelector != null);

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    yield break;

                var previous = e.Current;
                while (e.MoveNext())
                {
                    yield return resultSelector(previous, e.Current);
                    previous = e.Current;
                }
            }
        }
    }
}