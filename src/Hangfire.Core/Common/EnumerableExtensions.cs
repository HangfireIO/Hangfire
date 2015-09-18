using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Common
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Get the value associated with the specified key, or, if no value is associated, then returns a default value 
        /// (of default(TValue) if no other specified.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to search.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="defaultValue">The default value to return if the key is not currently associated with any value.</param>
        /// <returns>The value associated with the key, or the specified default value.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        // Thanks to http://stackoverflow.com/a/7470048/237091
        public static IEnumerable<IGrouping<TKey, TSource>> ChunkBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return source.ChunkBy(keySelector, EqualityComparer<TKey>.Default);
        }

        public static IEnumerable<IGrouping<TKey, TSource>> ChunkBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            const bool noMoreSourceElements = true;

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    yield break;

                Chunk<TKey, TSource> current;
                while (true)
                {
                    var key = keySelector(enumerator.Current);
                    current = new Chunk<TKey, TSource>(key, enumerator, value => comparer.Equals(key, keySelector(value)));

                    yield return current;

                    if (current.CopyAllChunkElements() == noMoreSourceElements)
                        yield break;
                }
            }
        }

        class Chunk<TKey, TSource> : IGrouping<TKey, TSource>, IDisposable
        {
            class ChunkItem
            {
                public ChunkItem(TSource value)
                {
                    Value = value;
                }
                public readonly TSource Value;
                public ChunkItem Next;
            }

            private readonly TKey _key;
            private IEnumerator<TSource> _enumerator;
            private Func<TSource, bool> _predicate;
            private readonly ChunkItem _head;
            private ChunkItem _tail;
            private bool _isLastSourceElement;
            private readonly object _mLock;

            public Chunk(TKey key, IEnumerator<TSource> enumerator, Func<TSource, bool> predicate)
            {
                _key = key;
                _enumerator = enumerator;
                _predicate = predicate;

                _head = new ChunkItem(enumerator.Current);

                _tail = _head;

                _mLock = new object();
            }

            private bool DoneCopyingChunk { get { return _tail == null; } }

            private void CopyNextChunkElement()
            {
                _isLastSourceElement = !_enumerator.MoveNext();

                if (_isLastSourceElement || !_predicate(_enumerator.Current))
                {
                    _enumerator = null;
                    _predicate = null;
                }
                else
                {
                    _tail.Next = new ChunkItem(_enumerator.Current);
                }

                _tail = _tail.Next;
            }

            internal bool CopyAllChunkElements()
            {
                while (true)
                    lock (_mLock)
                    {
                        if (DoneCopyingChunk)
                            return _isLastSourceElement;

                        CopyNextChunkElement();
                    }
            }

            public TKey Key { get { return _key; } }

            public IEnumerator<TSource> GetEnumerator()
            {
                ChunkItem current = _head;

                while (current != null)
                {
                    yield return current.Value;

                    lock (_mLock)
                        if (current == _tail)
                            CopyNextChunkElement();

                    current = current.Next;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #region Implementation of IDisposable

            public void Dispose()
            {
                if (null != _enumerator)
                    _enumerator.Dispose();

            }

            #endregion
        }
    }
}
