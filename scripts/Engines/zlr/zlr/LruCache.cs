/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
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

namespace ZLR.VM
{
    /// <summary>
    /// Implements a cache which discards the least recently used items.
    /// </summary>
    /// <typeparam name="K">The type of keys in the cache.</typeparam>
    /// <typeparam name="V">The type of values being cached.</typeparam>
    public class LruCache<K, V>
    {
        private struct Entry
        {
            public readonly K Key;
            public readonly V Value;
            public readonly int Size;

            public Entry(K key, V value, int size)
            {
                this.Key = key;
                this.Value = value;
                this.Size = size;
            }
        }

        private readonly Dictionary<K, LinkedListNode<Entry>> dict;
        private readonly LinkedList<Entry> llist;
        private int maxSize, currentSize, peakSize;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="cacheSize">The maximum total size that the cache can
        /// reach before it starts discarding items.</param>
        public LruCache(int cacheSize)
        {
            this.maxSize = cacheSize;

            dict = new Dictionary<K, LinkedListNode<Entry>>();
            llist = new LinkedList<Entry>();
        }

        public int Count
        {
            get { return dict.Count; }
        }

        public int CurrentSize
        {
            get { return currentSize; }
        }

        public int MaxSize
        {
            get { return maxSize; }
        }

        public int PeakSize
        {
            get { return peakSize; }
        }

        public IEnumerable<K> Keys
        {
            get { return dict.Keys; }
        }

        public IEnumerable<V> Values
        {
            get
            {
                foreach (Entry e in llist)
                    yield return e.Value;
            }
        }

        /// <summary>
        /// Stores a value into the cache.
        /// </summary>
        /// <param name="key">The cache key or address.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="size">The amount of cache space this value occupied by this value.</param>
        public void Add(K key, V value, int size)
        {
            if (dict.ContainsKey(key))
                throw new ArgumentException("Key already exists in cache", "key");

            LinkedListNode<Entry> node = new LinkedListNode<Entry>(new Entry(key, value, size));

            while (currentSize + size > maxSize && dict.Count > 0)
            {
                Entry lastEntry = llist.Last.Value;
                llist.RemoveLast();
                dict.Remove(lastEntry.Key);
                currentSize -= lastEntry.Size;
            }

            dict.Add(key, node);
            llist.AddFirst(node);
            currentSize += size;

            if (currentSize > peakSize)
                peakSize = currentSize;
        }

        /// <summary>
        /// Empties the cache.
        /// </summary>
        public void Clear()
        {
            dict.Clear();
            llist.Clear();
            currentSize = 0;
        }

        /// <summary>
        /// Attempts to read a value from the cache.
        /// </summary>
        /// <param name="key">The cache key or address to search for.</param>
        /// <param name="value">Set to the cached value, if it was found.</param>
        /// <returns><b>true</b> if the value was found in the cache.</returns>
        public bool TryGetValue(K key, out V value)
        {
            LinkedListNode<Entry> node;
            if (dict.TryGetValue(key, out node) == false)
            {
                value = default(V);
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
    }
}