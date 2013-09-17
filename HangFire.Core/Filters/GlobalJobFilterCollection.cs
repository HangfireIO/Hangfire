using System.Collections;
using System.Collections.Generic;

namespace HangFire.Filters
{
    public class GlobalJobFilterCollection : IEnumerable<IJobFilter>
    {
        private readonly List<IJobFilter> _filters = new List<IJobFilter>();

        public int Count
        {
            get
            {
                return _filters.Count;
            }
        }

        public void Add(IJobFilter filter)
        {
            _filters.Add(filter);
        }

        public void Clear()
        {
            _filters.Clear();
        }

        public bool Contains(IJobFilter filter)
        {
            return _filters.Contains(filter);
        }

        public void Remove(IJobFilter filter)
        {
            _filters.RemoveAll(x => x == filter);
        }

        public IEnumerator<IJobFilter> GetEnumerator()
        {
            return _filters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}