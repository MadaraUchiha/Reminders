using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Reminders
{
    public class BetterFastPriorityQueue<T> : FastPriorityQueue<T>, IExposable, IEnumerable<T> where T : IExposable
    {

        public List<T> InnerList => innerList;
        private HashSet<T> deletedItems = new HashSet<T>();

        public T Peek()
        {
            if (innerList.NullOrEmpty()) { return default; }
            return innerList.Find(item => !deletedItems.Contains(item));
        }

        public void Remove(T item)
        {
            deletedItems.Add(item);
        }

        public new T Pop()
        {
            T item = default;
            do
            {
                item = base.Pop();
                if (!deletedItems.Remove(item))
                {
                    return item;
                }
            } while (item != null);
            return default;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref innerList, "innerList", LookMode.Deep);
        }

        public IEnumerator<T> GetEnumerator() => innerList.Where(item => !deletedItems.Contains(item)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => innerList.Where(item => !deletedItems.Contains(item)).GetEnumerator();
    }
}
