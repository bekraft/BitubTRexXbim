using System;
using System.Collections;
using System.Collections.Generic;

using Xbim.Common;

namespace Bitub.Xbim.Ifc.Transform
{
    public sealed class ExpressEnumerableDelegate<T> : IExpressEnumerable, IEnumerable<T>, IList
    {
        private List<T> items;

        public ExpressEnumerableDelegate(IEnumerable<T> otherEnumerable)
        {
            items = new List<T>(otherEnumerable);
        }

        public object this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public int Count => items.Count;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        public int Add(object value) => throw new InvalidOperationException(); 

        public void Clear() => throw new InvalidOperationException();

        public bool Contains(object value) 
        {
            if (value is T t)
                return items.Contains(t);
            else
                return false;
        }

        public void CopyTo(Array array, int index) => throw new NotImplementedException();

        public IEnumerator GetEnumerator() => items?.GetEnumerator();

        public int IndexOf(object value)
        {
            if (value is T t)
                return items.IndexOf(t);
            else
                return -1;
        }

        public void Insert(int index, object value) => throw new NotImplementedException();       

        public void Remove(object value) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => items.GetEnumerator();
    }
}
