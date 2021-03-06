﻿//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Yawn.FiltersAndViews
{
    public class CollapsedCollectionView : FilteredCollectionView
    {
        internal CollapsedCollectionView(ItemCollection collections) : base(collections, Satisfies, null, HasPredicateChanged)
        {
        }

        protected static bool HasPredicateChanged(string propertyName)
        {
            return propertyName == "State";
        }

        private static bool Satisfies(FilteredCollectionView view, DockableCollection dockableCollection, object parameter)
        {
            return dockableCollection.IsCollapsed;
        }
    }
}
