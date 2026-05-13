// This file is part of Hangfire.
// Copyright © 2026 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hangfire.States;

namespace Hangfire
{
    public sealed class QueuePriorityCollection : IDictionary<string, int>
    {
        private readonly Dictionary<string, int> _items;

        public QueuePriorityCollection()
        {
            _items = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        public QueuePriorityCollection(IEnumerable<string> queues)
            : this()
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            var priority = 1;
            foreach (var queue in queues)
            {
                Add(queue, priority++);
            }
        }

        public QueuePriorityCollection(IDictionary<string, int> queues)
            : this()
        {
            if (queues == null) throw new ArgumentNullException(nameof(queues));

            foreach (var queue in queues)
            {
                Add(queue.Key, queue.Value);
            }
        }

        public int this[string key]
        {
            get => _items[key];
            set
            {
                Validate(key, value);
                _items[key] = value;
            }
        }

        public string this[int index]
        {
            get { return ToQueueArray()[index]; }
        }

        public ICollection<string> Keys => _items.Keys;

        public ICollection<int> Values => _items.Values;

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public void Add(string key, int value)
        {
            Validate(key, value);
            _items.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _items.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _items.Remove(key);
        }

        public bool TryGetValue(string key, out int value)
        {
            return _items.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, int> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(KeyValuePair<string, int> item)
        {
            return ((ICollection<KeyValuePair<string, int>>)_items).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, int>>)_items).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, int> item)
        {
            return ((ICollection<KeyValuePair<string, int>>)_items).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public QueueDescriptor[] ToDescriptors()
        {
            if (_items.Count == 0) throw new InvalidOperationException("At least one queue must be specified.");

            return _items
                .Select(static item => new QueueDescriptor(item.Key, item.Value))
                .OrderBy(static item => item.Priority)
                .ThenBy(static item => item.Name, StringComparer.Ordinal)
                .ToArray();
        }

        public string[] ToQueueArray()
        {
            return ToDescriptors().Select(static item => item.Name).ToArray();
        }

        public static implicit operator QueuePriorityCollection(string[] queues)
        {
            return new QueuePriorityCollection(queues);
        }

        private static void Validate(string queue, int priority)
        {
            EnqueuedState.ValidateQueueName(nameof(queue), queue);
            if (priority <= 0) throw new ArgumentOutOfRangeException(nameof(priority), "Queue priority must be a positive integer.");
        }
    }
}
