//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Yawn.FiltersAndViews
{
    public class CollapsedCollectionContentView : FilteredCollectionContentView
    {
        System.Windows.Controls.Dock CollapsedTabPosition;

        internal CollapsedCollectionContentView(ItemCollection collections, System.Windows.Controls.Dock collapsedTabPosition) : base(collections, Satisfies, HasPredicateChanged)
        {
            CollapsedTabPosition = collapsedTabPosition;
        }

        protected static bool HasPredicateChanged(string propertyName)
        {
            return propertyName == "State";
        }

        private static bool Satisfies(FilteredCollectionView view, DockableCollection dockableCollection, object parameter)
        {
            return dockableCollection.IsCollapsed && dockableCollection.CollapsedTabPosition == (parameter as CollapsedCollectionContentView).CollapsedTabPosition;
        }
    }
}
