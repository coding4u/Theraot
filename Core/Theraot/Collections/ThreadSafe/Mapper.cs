// Needed for NET40

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Theraot.Collections.ThreadSafe
{
    [Serializable]
    public class Mapper<T> : IEnumerable<T>
    {
        private const int INT_MaxOffset = 32;
        private const int INT_OffsetStep = 4;
        private readonly Branch _root;
        private int _count;

        public Mapper()
        {
            _count = 0;
            _root = new Branch();
        }

        /// <summary>
        /// Gets the number of items actually contained.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Copies the items to a compatible one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        /// <exception cref="System.ArgumentNullException">array</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex;Non-negative number is required.</exception>
        /// <exception cref="System.ArgumentException">array;The array can not contain the number of elements.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", "Non-negative number is required.");
            }
            if (_count > array.Length - arrayIndex)
            {
                throw new ArgumentException("The array can not contain the number of elements.", "array");
            }
            try
            {
                foreach (var entry in _root)
                {
                    array[arrayIndex] = (T)entry;
                    arrayIndex++;
                }
            }
            catch (IndexOutOfRangeException exception)
            {
                throw new ArgumentOutOfRangeException("array", exception.Message);
            }
        }

        /// <summary>
        /// Sets the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="previous">The previous item in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the item was new; otherwise, <c>false</c>.
        /// </returns>
        public bool Exchange(int index, T item, out T previous)
        {
            previous = default(T);
            object previousObject;
            if (_root.Exchange(unchecked((uint)index), item, out previousObject))
            {
                Interlocked.Increment(ref _count);
                return true;
            }
            previous = (T)previousObject;
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _root.Select(item => (T)item).GetEnumerator();
        }

        /// <summary>
        /// Inserts the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the item was inserted; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The insertion can fail if the index is already used or is being written by another thread.
        /// If the index is being written it can be understood that the insert operation happened before but the item was overwritten or removed.
        /// </remarks>
        public bool Insert(int index, T item)
        {
            object previous;
            if (_root.Insert(unchecked((uint)index), item, out previous))
            {
                Interlocked.Increment(ref _count);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Inserts the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="previous">The previous item in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the item was inserted; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The insertion can fail if the index is already used or is being written by another thread.
        /// If the index is being written it can be understood that the insert operation happened before but the item was overwritten or removed.
        /// </remarks>
        public bool Insert(int index, T item, out T previous)
        {
            previous = default(T);
            object previousObject;
            if (_root.Insert(unchecked((uint)index), item, out previousObject))
            {
                Interlocked.Increment(ref _count);
                return true;
            }
            previous = (T)previousObject;
            return false;
        }

        public bool InsertOrUpdate(int index, Func<T> itemFactory, Func<T, T> itemUpdateFactory, Predicate<T> check, out T stored, out bool isNew)
        {
            if (ReferenceEquals(itemFactory, null))
            {
                throw new ArgumentNullException("itemFactory");
            }
            if (ReferenceEquals(itemUpdateFactory, null))
            {
                throw new ArgumentNullException("itemUpdateFactory");
            }
            if (ReferenceEquals(check, null))
            {
                throw new ArgumentNullException("check");
            }
            return InternalInsertOrUpdate(index, () => itemFactory(), input => itemUpdateFactory((T)input), input => check((T)input), out stored, out isNew);
        }

        public bool InsertOrUpdate(int index, T item, Func<T, T> itemUpdateFactory, Predicate<T> check, out T stored, out bool isNew)
        {
            if (ReferenceEquals(itemUpdateFactory, null))
            {
                throw new ArgumentNullException("itemUpdateFactory");
            }
            if (ReferenceEquals(check, null))
            {
                throw new ArgumentNullException("check");
            }
            return InternalInsertOrUpdate(index, item, input => itemUpdateFactory((T)input), input => check((T)input), out stored, out isNew);
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        ///   <c>true</c> if the item was removed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool RemoveAt(int index)
        {
            object previousObject;
            if (_root.RemoveAt(unchecked((uint)index), out previousObject))
            {
                Interlocked.Decrement(ref _count);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="previous">The previous item in the specified index.</param>
        /// <returns>
        ///   <c>true</c> if the item was removed; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index;index must be greater or equal to 0 and less than capacity</exception>
        public bool RemoveAt(int index, out T previous)
        {
            object previousObject;
            if (_root.RemoveAt(unchecked((uint)index), out previousObject))
            {
                Interlocked.Decrement(ref _count);
                previous = (T)previousObject;
                return true;
            }
            previous = default(T);
            return false;
        }

        public void Set(int index, T value, out bool isNew)
        {
            _root.Set(unchecked((uint)index), value, out isNew);
            if (isNew)
            {
                Interlocked.Increment(ref _count);
            }
        }

        public bool TryGet(int index, out T value)
        {
            value = default(T);
            object valueObject;
            if (_root.TryGet(unchecked((uint)index), out valueObject))
            {
                value = (T)valueObject;
                return true; // true means value was found
            }
            return false; // false means value was not found
        }

        public bool TryGetOrInsert(int index, T item, out T stored)
        {
            // This method should be encapsulated in a public one.
            object storedObject;
            var result = _root.TryGetOrInsert(unchecked((uint)index), item, out storedObject);
            if (result)
            {
                Interlocked.Increment(ref _count);
            }
            stored = (T)storedObject;
            return result; // true means value was set
        }

        public bool TryGetOrInsert(int index, Func<T> itemFactory, out T stored)
        {
            if (itemFactory == null)
            {
                throw new ArgumentNullException("itemFactory");
            }
            return InternalTryGetOrInsert(index, itemFactory, out stored);
        }

        public bool TryUpdate(int index, T item, Predicate<T> check)
        {
            // This should never add an item
            return _root.TryUpdate(unchecked((uint)index), item, input => check((T)input)); // true means value was set
        }

        public bool TryUpdate(int index, T item, T comparisonItem)
        {
            // This should never add an item
            return _root.TryUpdate(unchecked((uint)index), item, comparisonItem); // true means value was set
        }

        /// <summary>
        /// Returns the values where the predicate is satisfied.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>
        /// An <see cref="IEnumerable{T}" /> that allows to iterate over the removed values.
        /// </returns>
        /// <remarks>
        /// It is not guaranteed that all the values that satisfies the predicate will be removed.
        /// </remarks>
        public IEnumerable<T> Where(Predicate<T> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            return InternalWhere(predicate);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal bool InternalInsertOrUpdate(int index, Func<object> itemFactory, Func<object, object> itemUpdateFactory, Predicate<object> check, out T stored, out bool isNew)
        {
            // NOTICE this method has no null check
            object storedObject;
            var result = _root.InsertOrUpdate
                         (
                             unchecked((uint)index),
                             itemFactory,
                             itemUpdateFactory,
                             check,
                             out storedObject,
                             out isNew
                         );
            if (isNew)
            {
                Interlocked.Increment(ref _count);
            }
            stored = (T)storedObject;
            return result;
        }

        internal bool InternalInsertOrUpdate(int index, T item, Func<object, object> itemUpdateFactory, Predicate<object> check, out T stored, out bool isNew)
        {
            // NOTICE this method has no null check
            object storedObject;
            var result = _root.InsertOrUpdate
                         (
                             unchecked((uint)index),
                             item,
                             itemUpdateFactory,
                             check,
                             out storedObject,
                             out isNew
                         );
            if (isNew)
            {
                Interlocked.Increment(ref _count);
            }
            stored = (T)storedObject;
            return result;
        }

        internal bool InternalTryGetOrInsert(int index, Func<T> itemFactory, out T stored)
        {
            // This method should be encapsulated in a public one.
            object storedObject;
            var result = _root.TryGetOrInsert(unchecked((uint)index), () => itemFactory(), out storedObject);
            if (result)
            {
                Interlocked.Increment(ref _count);
            }
            stored = (T)storedObject;
            return result; // true means value was set
        }

        internal IEnumerable<T> InternalWhere(Predicate<T> predicate)
        {
            // This method should be encapsulated in a public one.
            return _root.Where(value => predicate((T)value)).Select(value => (T)value);
        }

        internal bool TryGetCheckRemoveAt(int index, Predicate<object> check, out T previous)
        {
            // NOTICE this method has no null check
            // Keep this method internal
            object storedObject;
            if (_root.TryGetCheckRemoveAt(unchecked((uint)index), check, out storedObject))
            {
                previous = (T)storedObject;
                Interlocked.Decrement(ref _count);
                return true; // true means value was found and removed
            }
            previous = default(T);
            return false; // false means value was either not found or not removed
        }

        internal bool TryGetCheckSet(int index, T item, Predicate<object> check, out bool isNew)
        {
            // NOTICE this method has no null check
            // Keep this method internal
            var result = _root.TryGetCheckSet(unchecked((uint)index), item, check, out isNew);
            if (isNew)
            {
                Interlocked.Increment(ref _count);
            }
            return result; // true means value was set
        }

        internal bool TryGetCheckSet(int index, Func<object> itemFactory, Func<object, object> itemUpdateFactory, out bool isNew)
        {
            // NOTICE this method has no null check
            // Keep this method internal
            var result = _root.TryGetCheckSet(unchecked((uint)index), itemFactory, itemUpdateFactory, out isNew);
            if (isNew)
            {
                Interlocked.Increment(ref _count);
            }
            return result; // true means value was set
        }
    }
}