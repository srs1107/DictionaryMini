﻿using System;
using System.Collections.Generic;

namespace DictionaryMini
{
    public class DictionaryMini<TKey, TValue>
    {
        //哈希中介
        private int[] _buckets;

        private Entry[] _entries;

        //当前空余的Entry数量
        private int _freeCount;

        //当前空余的Entry的索引
        private int _freeList;

        //当前字典的容量
        private int _count;

        //字典中数据存储的基础单元
        private struct Entry
        {
            public int HashCode;
            public int Next;
            public TKey Key;
            public TValue Value;
        }

        //针对TKey类型的对比器
        private readonly IEqualityComparer<TKey> _comparer;

        public DictionaryMini() : this(0)
        {
        }

        public DictionaryMini(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException();
            if (capacity > 0)
                Initialize(capacity);
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        //添加Item
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Insert(item.Key, item.Value, true);
        }

        //添加Item
        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }

        //清空所有元素
        public void Clear()
        {
            if (_count > 0)
            {
                for (int i = 0; i < _buckets.Length; i++) _buckets[i] = -1;
                Array.Clear(_entries, 0, _count);
                _freeList = -1;
                _count = 0;
                _freeCount = 0;
            }
        }

        //判断是否包含指定key
        public bool ContainsKey(TKey key)
        {
            return FindEntry(key) >= 0;
        }

        //通过key移除指定的item
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new Exception();

            if (_buckets != null)
            {
                int hashCode = _comparer.GetHashCode(key);
                int bucket = hashCode % _buckets.Length;
                int last = -1;
                for (int i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].Next)
                {
                    if (_entries[i].HashCode == hashCode && _comparer.Equals(_entries[i].Key, key))
                    {
                        if (last < 0)
                        {
                            _buckets[bucket] = _entries[i].Next;
                        }
                        else
                        {
                            _entries[last].Next = _entries[i].Next;
                        }

                        _entries[i].HashCode = -1;
                        _entries[i].Next = _freeList;
                        _entries[i].Key = default(TKey);
                        _entries[i].Value = default(TValue);
                        _freeList = i;
                        _freeCount++;
                        return true;
                    }
                }
            }

            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0) return _entries[i].Value;
                throw new Exception("给定的关键字不在字典中!");
            }
            set => Insert(key, value, false);
        }

        #region Private

        private void Initialize(int capacity)
        {
            int size = HashHelpersMini.GetPrime(capacity);
            _buckets = new int[size];
            for (int i = 0; i < _buckets.Length; i++)
            {
                _buckets[i] = -1;
            }

            _entries = new Entry[size];

            _freeList = -1;
        }

        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            //如果buckets为空,则重新初始化字典.
            if (_buckets == null) Initialize(0);
            //获取传入key的 哈希值
            var hashCode = _comparer.GetHashCode(key);
            //把hashCode%size的值作为目标Bucket的Index.
            var targetBucket = hashCode % _buckets.Length;
            //遍历判断传入的key对应的值是否已经添加字典中
            for (int i = _buckets[targetBucket]; i >= 0; i = _entries[i].Next)
            {
                if (_entries[i].HashCode == hashCode && _comparer.Equals(_entries[i].Key, key))
                {
                    //当add为true时,直接抛出异常,告诉给定的值已存在在字典中.
                    if (add)
                    {
                        throw new Exception("给定的关键字已存在!");
                    }
                    //当add为false时,重新赋值并退出.
                    _entries[i].Value = value;
                    return;
                }
            }
            //表示本次存储数据的数据在Entries中的索引
            int index;
            //当有数据被Remove时,freeCount会加1
            if (_freeCount > 0)
            {
                //freeList为上一个移除数据的Entries的索引,这样能尽量地让连续的Entries都利用起来.
                index = _freeList;
                _freeList = _entries[index].Next;
                _freeCount--;
            }
            else
            {
                //当已使用的Entry的数据等于Entries的长度时,说明字典里的数据已经存满了,需要对字典进行扩容,Resize.
                if (_count == _entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % _buckets.Length;
                }
                //默认取未使用的第一个
                index = _count;
                _count++;
            }
            //对Entries进行赋值
            _entries[index].HashCode = hashCode;
            _entries[index].Next = _buckets[targetBucket];
            _entries[index].Key = key;
            _entries[index].Value = value;
            //用buckets来登记数据在Entries中的索引.
            _buckets[targetBucket] = index;
        }

        private void Resize()
        {
            Resize(HashHelpersMini.GetPrime(_count), false);
        }

        private void Resize(int newSize, bool foreNewHashCodes)
        {
            var newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            var newEntries = new Entry[newSize];
            Array.Copy(_entries, 0, newEntries, 0, _count);
            if (foreNewHashCodes)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (newEntries[i].HashCode != -1)
                    {
                        newEntries[i].HashCode = _comparer.GetHashCode(newEntries[i].Key);
                    }
                }
            }

            for (int i = 0; i < _count; i++)
            {
                if (newEntries[i].HashCode > 0)
                {
                    int bucket = newEntries[i].HashCode % newSize;
                    newEntries[i].Next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }

            _buckets = newBuckets;
            _entries = newEntries;
        }

        private int FindEntry(TKey key)
        {
            if (_buckets != null)
            {
                int hashCode = _comparer.GetHashCode(key);
                for (int i = _buckets[hashCode % _buckets.Length]; i >= 0; i = _entries[i].Next)
                {
                    if (_entries[i].HashCode == hashCode && _comparer.Equals(_entries[i].Key, key)) return i;
                }
            }
            return -1;
        }

        #endregion Private
    }
}