using System;
using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>生成と破棄をくり返さず、非表示にして使い回す。</summary>
    public sealed class Pool<T> where T : Component
    {
        readonly Stack<T> free = new Stack<T>();
        readonly Func<T> create;

        public Pool(Func<T> create)
        {
            this.create = create;
        }

        public T Get()
        {
            var item = free.Count > 0 ? free.Pop() : create();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            item.gameObject.SetActive(false);
            free.Push(item);
        }
    }
}
