/***************************************************************************
 *
 *   ZLR                     : May 1, 2007
 *   implementation          : (C) 2007-2023 Tara McGrew
 *   repository url          : https://foss.heptapod.net/zilf/zlr
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ZLR.VM
{
    /// <summary>
    /// Implements a cache which discards the least recently used items.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values being cached.</typeparam>
    public class LruCache<TKey, TValue>
    {
        private struct Entry
        {
            public readonly TKey Key;
            public readonly TValue Value;
            public readonly int Size;

            public Entry(TKey key, TValue value, int size)
            {
                Key = key;
                Value = value;
                Size = size;
            }
        }

        [NotNull] private readonly Dictionary<TKey, LinkedListNode<Entry>> dict;
        [NotNull] private readonly LinkedList<Entry> llist;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="cacheSize">The maximum total size that the cache can
        /// reach before it starts discarding items.</param>
        public LruCache(int cacheSize)
        {
            MaxSize = cacheSize;

            dict = new Dictionary<TKey, LinkedListNode<Entry>>();
            llist = new LinkedList<Entry>();
        }

        public int Count => dict.Count;

        public int CurrentSize { get; private set; }

        public int MaxSize { get; }

        public int PeakSize { get; private set; }

        [NotNull]
        public IEnumerable<TKey> Keys => dict.Keys;

        [NotNull]
        public IEnumerable<TValue> Values => llist.Select(e => e.Value);

        /// <summary>
        /// Stores a value into the cache.
        /// </summary>
        /// <param name="key">The cache key or address.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="size">The amount of cache space this value occupied by this value.</param>
        public void Add(TKey key, TValue value, int size)
        {
            if (dict.ContainsKey(key))
                throw new ArgumentException("Key already exists in cache", nameof(key));

            var node = new LinkedListNode<Entry>(new Entry(key, value, size));

            while (CurrentSize + size > MaxSize && dict.Count > 0)
            {
                var lastEntry = llist.Last.Value;
                llist.RemoveLast();
                dict.Remove(lastEntry.Key);
                CurrentSize -= lastEntry.Size;
            }

            dict.Add(key, node);
            llist.AddFirst(node);
            CurrentSize += size;

            if (CurrentSize > PeakSize)
                PeakSize = CurrentSize;
        }

        /// <summary>
        /// Empties the cache.
        /// </summary>
        public void Clear()
        {
            dict.Clear();
            llist.Clear();
            CurrentSize = 0;
        }

        /// <summary>
        /// Attempts to read a value from the cache.
        /// </summary>
        /// <param name="key">The cache key or address to search for.</param>
        /// <param name="value">Set to the cached value, if it was found.</param>
        /// <returns><b>true</b> if the value was found in the cache.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (dict.TryGetValue(key, out var node) == false)
            {
                value = default;
                return false;
            }

            if (node != llist.First)
            {
                llist.Remove(node);
                llist.AddFirst(node);
            }

            value = node.Value.Value;
            return true;
        }

        /// <summary>
        /// Checks whether the cache contains a key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns><b>true</b> if the key is present in the cache.</returns>
        public bool ContainsKey(TKey key)
        {
            return dict.ContainsKey(key);
        }
    }
}