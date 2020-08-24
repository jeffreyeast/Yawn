//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yawn
{
    public class OrderedObservableCollection<T> : ObservableCollection<T> where T : IComparable<T>
    {
        public new void Add(T item)
        {
            for (int index = 0; index < Count; index++)
            {
                if (item.CompareTo(Items[index]) < 0)
                {
                    base.InsertItem(index, item);
                    return;
                }
            }
            base.InsertItem(Count, item);
        }

        public new void Insert(Int32 oldIndex, T item)
        {
            throw new InvalidOperationException("You cannot explicitly insert an item in an OrderedObservableCollection");
        }

        public new void Move(Int32 oldIndex, Int32 newIndex)
        {
            throw new InvalidOperationException("You cannot explicitly move an item in an OrderedObservableCollection");
        }

        public void OnItemKeyChanged(T item)
        {
            //  Locate the old and new locations for the item

            int oldIndex = base.IndexOf(item);
            for (int newIndex = 0; newIndex < Count; newIndex++)
            {
                if (item.CompareTo(Items[newIndex]) < 0)
                {
                    if (newIndex < oldIndex)
                    {
                        base.MoveItem(oldIndex, newIndex);
                    }
                    else if (newIndex > oldIndex)
                    {
                        base.MoveItem(oldIndex, newIndex - 1);
                    }
                    return;
                }
            }
            if (oldIndex != Count - 1)
            {
                base.MoveItem(oldIndex, Count - 1);
            }
        }
    }
}
