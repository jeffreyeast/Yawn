//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Yawn.FiltersAndViews
{
    public class FilteredCollectionView : ObservableCollection<DockableCollection>
    {
        ItemCollection SourceItemCollection;
        HashSet<DockableCollection> MonitoredDockableCollections;
        DockableCollectionFilter CollectionFilter;
        object CollectionFilterParameter;
        PredicateRescanRequired PredicateChecker;

        internal delegate bool DockableCollectionFilter(FilteredCollectionView view, DockableCollection dockableCollection, object parameter);
        internal delegate bool PredicateRescanRequired(string propertyName);



        internal FilteredCollectionView(ItemCollection sourceItemCollections, DockableCollectionFilter collectionFilter, object filterParameter, PredicateRescanRequired predicateChecker)
        {
            SourceItemCollection = sourceItemCollections;
            CollectionFilter = collectionFilter;
            CollectionFilterParameter = filterParameter;
            PredicateChecker = predicateChecker;
            ((INotifyCollectionChanged)sourceItemCollections).CollectionChanged += SourceItemCollection_ChangedHandler;
            Monitor();
            Load();
        }

        public new void Add(DockableCollection item)
        {
            throw new InvalidOperationException("FilteredCollectionView does not support explicit management of members");
        }

        private void DockableCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is DockableCollection dockableCollection)
            {
                if (PredicateChecker(e.PropertyName))
                {
                    if (CollectionFilter(this, dockableCollection, CollectionFilterParameter))
                    {
                        if (!Items.Contains(dockableCollection))
                        {
                            base.Add(dockableCollection);
                        }
                    }
                    else
                    {
                        if (Items.Contains(dockableCollection))
                        {
                            base.Remove(dockableCollection);
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private void Load()
        {
            HashSet<DockableCollection> qualifiedItems = new HashSet<DockableCollection>();
            HashSet<DockableCollection> existingItems = new HashSet<DockableCollection>(Items);

            foreach (DockableCollection dockableCollection in MonitoredDockableCollections)
            {
                if (CollectionFilter(this, dockableCollection, CollectionFilterParameter))
                {
                    qualifiedItems.Add(dockableCollection);
                }
            }

            foreach (DockableCollection dockableCollection in qualifiedItems)
            {
                if (!existingItems.Contains(dockableCollection))
                {
                    base.Add(dockableCollection);
                }
            }
            foreach (DockableCollection dockableCollection in existingItems)
            {
                if (!qualifiedItems.Contains(dockableCollection))
                {
                    base.Remove(dockableCollection);
                }
            }
        }

        private void Monitor()
        {
            MonitoredDockableCollections = new HashSet<DockableCollection>(SourceItemCollection.Cast<DockableCollection>());
            foreach (DockableCollection dockableCollection in MonitoredDockableCollections)
            {
                dockableCollection.PropertyChanged += DockableCollection_PropertyChanged;
            }
        }

        public new void Remove(DockableCollection item)
        {
            throw new InvalidOperationException("FilteredCollectionView does not support explicit management of members");
        }

        private void SourceItemCollection_ChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (DockableCollection dockableCollection in e.NewItems)
                    {
                        MonitoredDockableCollections.Add(dockableCollection);
                        dockableCollection.PropertyChanged += DockableCollection_PropertyChanged;
                        if (CollectionFilter(this, dockableCollection, CollectionFilterParameter))
                        {
                            base.Add(dockableCollection);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (DockableCollection dockableCollection in e.OldItems)
                    {
                        dockableCollection.PropertyChanged -= DockableCollection_PropertyChanged;
                        MonitoredDockableCollections.Remove(dockableCollection);
                        if (Contains(dockableCollection))
                        {
                            base.Remove(dockableCollection);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    UnMonitor();
                    Monitor();
                    Load();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void UnMonitor()
        {
            foreach (DockableCollection dockableCollection in MonitoredDockableCollections)
            {
                dockableCollection.PropertyChanged -= DockableCollection_PropertyChanged;
            }
        }
    }
}
