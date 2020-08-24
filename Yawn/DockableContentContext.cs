//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Yawn.Interfaces;

namespace Yawn
{
    /// <summary>
    /// Represents content in a DockableCollection. Assumes the collection of content is ordered by TabSequence.
    /// </summary>
    public class DockableContentContext : INotifyPropertyChanged, IComparable<DockableContentContext>
    {
        public DockableCollection DockableCollection 
        {
            get => _dockableCollection;
            internal set
            {
                if (_dockableCollection != value)
                {
                    if (_dockableCollection != null)
                    {
                        _dockableCollection.PropertyChanged -= DockableCollection_PropertyChanged;
                    }
                    _dockableCollection = value;
                    if (_dockableCollection == null)
                    {
                        IsContentVisible = false;
                    }
                    else
                    {
                        _dockableCollection.PropertyChanged += DockableCollection_PropertyChanged;
                        IsContentVisible = _dockableCollection.VisibleContent == FrameworkElement;
                    }
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DockableCollection"));
                }
            }
        }
        DockableCollection _dockableCollection;

        public FrameworkElement FrameworkElement { get; private set; }

        public bool IsContentVisible
        {
            get => _isContentVisible;
            set
            {
                if (_isContentVisible != value)
                {
                    _isContentVisible = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsContentVisible"));
                }
            }
        }
        bool _isContentVisible;

        public uint TabSequence 
        {
            get => _tabSequence;
            set
            {
                if (_tabSequence != value)
                {
                    _tabSequence = value;
                    BaseCollection.OnItemKeyChanged(this);
                }
            }
        }
        uint _tabSequence;

        OrderedObservableCollection<DockableContentContext> BaseCollection;

        public event PropertyChangedEventHandler PropertyChanged;


        public DockableContentContext(DockableCollection dockableCollection, OrderedObservableCollection<DockableContentContext> baseCollection, FrameworkElement frameworkElement)
        {
            DockableCollection = dockableCollection;
            BaseCollection = baseCollection;
            FrameworkElement = frameworkElement;
            IsContentVisible = false;
        }

        public int CompareTo(DockableContentContext other)
        {
            return TabSequence.CompareTo(other.TabSequence);
        }

        private void DockableCollection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "VisibleContent")
            {
                IsContentVisible = DockableCollection.VisibleContent == FrameworkElement;
            }
        }

        public override string ToString()
        {
            return DockingPanel.GetTabText(FrameworkElement);
        }
    }
}
