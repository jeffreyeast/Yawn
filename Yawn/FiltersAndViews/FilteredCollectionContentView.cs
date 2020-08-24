//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Yawn.FiltersAndViews
{
    public class FilteredCollectionContentView : ObservableCollection<DockableContentContext>
    {
        protected FilteredCollectionView FilteredCollections { get; private set; }


        internal FilteredCollectionContentView(ItemCollection sourceItemCollections, FilteredCollectionView.DockableCollectionFilter collectionFilter, FilteredCollectionView.PredicateRescanRequired predicateChecker)
        {
            FilteredCollections = new FilteredCollectionView(sourceItemCollections, collectionFilter, this, predicateChecker);
            FilteredCollections.CollectionChanged += FilteredCollections_CollectionChanged;
            InitialLoad();
        }

        public new void Add(DockableContentContext item)
        {
            throw new InvalidOperationException("FilteredCollectionContentView does not support explicit management of members");
        }

        private void DockableContentContexts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (DockableContentContext context in e.NewItems)
                    {
                        base.Add(context);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (DockableContentContext context in e.OldItems)
                    {
                        base.Remove(context);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void FilteredCollections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (DockableCollection dockableCollection in e.NewItems)
                    {
                        dockableCollection.DockableContentContexts.CollectionChanged += DockableContentContexts_CollectionChanged;
                        foreach (DockableContentContext context in dockableCollection.DockableContentContexts)
                        {
                            base.Add(context);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (DockableCollection dockableCollection in e.OldItems)
                    {
                        dockableCollection.DockableContentContexts.CollectionChanged -= DockableContentContexts_CollectionChanged;
                        foreach (DockableContentContext context in dockableCollection.DockableContentContexts)
                        {
                            base.Remove(context);
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void InitialLoad()
        {
            foreach (DockableCollection dockableCollection in FilteredCollections)
            {
                dockableCollection.DockableContentContexts.CollectionChanged += DockableContentContexts_CollectionChanged;
                foreach (DockableContentContext context in dockableCollection.DockableContentContexts)
                {
                    base.Add(context);
                }
            }
        }

        public new void Remove(DockableContentContext item)
        {
            throw new InvalidOperationException("FilteredCollectionContentView does not support explicit management of members");
        }
    }
}
