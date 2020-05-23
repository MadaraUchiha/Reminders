using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Reminders
{
    public class BetterFastPriorityQueue<T>: FastPriorityQueue<T>, IExposable, IEnumerable<T> where T : IExposable
    {

        public List<T> InnerList => innerList;

        public T Peek()
        {
            if (innerList.NullOrEmpty()) { return default; }
            return innerList[0];
        }

        public void Remove(T item)
        {
            innerList.Remove(item);
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref innerList, "innerList", LookMode.Deep);
        }

        public IEnumerator<T> GetEnumerator() => innerList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => innerList.GetEnumerator();
    }
}
