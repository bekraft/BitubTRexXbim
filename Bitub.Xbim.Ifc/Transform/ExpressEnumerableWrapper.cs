using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Xbim.Common;

namespace Bitub.Xbim.Ifc.Transform
{
    public class ExpressEnumerableWrapper<T> : IExpressEnumerable, IEnumerable<T>
    {
        private IEnumerable<T> wrapped;

        public ExpressEnumerableWrapper(IEnumerable<T> otherEnumerable)
        {
            wrapped = otherEnumerable;
        }

        public IEnumerator GetEnumerator() => wrapped?.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => wrapped?.GetEnumerator();
    }
}
