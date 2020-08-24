//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Yawn
{
    /// <summary>
    /// The Layout object is used to save and restore the layout of DockableCollections and their DockableContexts.
    /// </summary>
    public static class Layout
    {
        /// <summary>
        /// The DockableContextContentCreator delegate describes the callback routine invoked by
        /// the Layout object when restoring a DockableContext.  It creates the DockableContext object.
        /// </summary>
        /// <param name="contentType">  Provides the type string of the content</param>
        /// <param name="description">  Provides a context/description string (originally provided
        ///                             to the Layout object by the content's ISaveableContent when saving the configuration). If
        ///                             the content doesn't implement ISaveableContent, the DockableContent's TabText is used.</param>
        /// <returns>DockableContent   Non-null if the content is successfully created, else null. If False, the DockableContent object
        ///                             is discarded (not merged into the visual tree).</returns>
        /// 
        public delegate FrameworkElement DockableContextContentCreator(string contentType, string description);



        /// <summary>
        /// The Restore method is invoked to populate a Dock with a previously saved configuration.
        /// </summary>
        /// <param name="layout">               Provides the object previously created by Save</param>
        /// <param name="dock">                 Provides the Dock to receive the new DockableCollection and DockableContent objects. It must have
        ///                                     no items when Restore is invoked.</param>
        /// <param name="contentCreator">       Provides an optional callback, invoked once for each freshly restored DockableContent object
        ///                                     in the saved configuration.</param>
        public static void Restore(object layout, Dock dock, DockableContextContentCreator contentCreator)
        {
            if (layout is DockDescriptor dockDescriptor)
            {
                if (dock.Items.Count > 0)
                {
                    throw new InvalidOperationException("Yawn.Layout.Restore requires the input Dock to be empty.");
                }

                dockDescriptor.RestoreLayout(dock, contentCreator);
            }
            else
            {
                throw new ArgumentException("Yawn.Layout.Restore expected a " + typeof(DockDescriptor).ToString() + ", but was passed a " + layout.GetType().ToString(), "layout");
            }
        }

        /// <summary>
        /// Save is invoked to preserve the configuration of a Dock.
        /// </summary>
        /// <param name="dock">                 Provides the Dock object whose configuration is to be saved.</param>
        /// <returns>object                     An opaque object which may be serialized to persist the configuration</returns>
        public static object Save(Dock dock)
        {
            return new DockDescriptor(dock);
        }

        [Serializable]
        public class DockDescriptor
        {
            public List<DockableCollectionDescriptor> DockedCollections { get; private set; }
            public List<DockDescriptor> FloatingDocks { get; private set; }
            public Size DockSize { get; set; }
            public Point Position { get; set; }
            [XmlAttribute]
            public int MajorVersion { get; set; }
            [XmlAttribute]
            public int MinorVersion { get; set; }

            readonly static int MetadataMajorVersion = 1;           //  Bump for incompatible changes
            readonly static int MetadataMinorVersion = 1;           //  Bump for upwards compatible changes



            public DockDescriptor()
            {
                DockedCollections = new List<DockableCollectionDescriptor>();
                FloatingDocks = new List<DockDescriptor>();
            }

            public DockDescriptor(Dock dock)
            {
                DockedCollections = new List<DockableCollectionDescriptor>();
                FloatingDocks = new List<DockDescriptor>();
                DockSize = new Size(dock.ActualWidth, dock.ActualHeight);
                Position = dock.PointToScreen(new Point(0, 0));
                MajorVersion = MetadataMajorVersion;
                MinorVersion = MetadataMinorVersion;

                int internalId = 1;
                foreach (DockableCollection dockableCollection in dock.Items)
                {
                    new DockableCollectionDescriptor(dockableCollection, this, internalId++);
                }
                SavePeerLinkages();

                foreach (Dock floatingDock in dock.FloatingDocks)
                {
                    FloatingDocks.Add(new DockDescriptor(floatingDock));
                }
            }

            internal void RestoreLayout(Dock dock, DockableContextContentCreator contentCreator)
            {
                Dictionary<int, DockableCollection> dockableCollections = new Dictionary<int, DockableCollection>();

                if (MajorVersion != MetadataMajorVersion || MinorVersion > MetadataMinorVersion)
                {
                    throw new InvalidDataException("Saved layout version (" + MajorVersion.ToString() + "." + MinorVersion.ToString() +
                        " is not compatible with the expected version " + MetadataMajorVersion.ToString() + "." + MetadataMinorVersion.ToString());
                }

                //  First, create the collections and associate them with the dock

                foreach (DockableCollectionDescriptor desc in DockedCollections)
                {
                    DockableCollection newCollection = desc.Create(dock, contentCreator);
                    dockableCollections.Add(desc.InternalId, newCollection);

                }


                //  Next, resolve their peer associations

                foreach (DockableCollectionDescriptor desc in DockedCollections)
                {
                    DockableCollection dockableCollection = dockableCollections[desc.InternalId];
                    desc.ResolvePeerRelationships(dockableCollections, dockableCollection);
                }

                dock.InvalidatePhysical();

                if (dock.ActualWidth != DockSize.Width || dock.ActualHeight != DockSize.Height)
                {
                    dock.InvalidatePositioning(LayoutContext.PositionClasses.EveryCollection | LayoutContext.PositionClasses.All | LayoutContext.PositionClasses.Resize);
                }

                foreach (DockDescriptor desc in FloatingDocks)
                {
                    dock.CreateFloatingDock(desc.Position, desc.DockSize, (DockWindow dockWindow) =>
                   {
                       desc.RestoreLayout(dockWindow.Dock, contentCreator);
                   });
                }
            }

            private void SavePeerLinkages()
            {
                //  Load the DockableCollections into a dictionary for fast access

                Dictionary<DockableCollection, int> dockableCollections = new Dictionary<DockableCollection, int>();
                foreach (DockableCollectionDescriptor desc in DockedCollections)
                {
                    dockableCollections.Add(desc.DockableCollection, desc.InternalId);
                }

                //  Now save the peer linkages for each DockableCollection

                foreach (DockableCollectionDescriptor desc in DockedCollections)
                {
                    foreach (LayoutContext layoutContext in DockingPanel.GetLayoutContext(desc.DockableCollection).Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors)
                    {
                        desc.BottomPeers.Add(dockableCollections[layoutContext.DockableCollection as DockableCollection]);
                    }
                    foreach (LayoutContext layoutContext in DockingPanel.GetLayoutContext(desc.DockableCollection).Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors)
                    {
                        desc.LeftPeers.Add(dockableCollections[layoutContext.DockableCollection as DockableCollection]);
                    }
                    foreach (LayoutContext layoutContext in DockingPanel.GetLayoutContext(desc.DockableCollection).Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors)
                    {
                        desc.RightPeers.Add(dockableCollections[layoutContext.DockableCollection as DockableCollection]);
                    }
                    foreach (LayoutContext layoutContext in DockingPanel.GetLayoutContext(desc.DockableCollection).Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors)
                    {
                        desc.TopPeers.Add(dockableCollections[layoutContext.DockableCollection as DockableCollection]);
                    }
                }
            }
        }

        [Serializable]
        public class DockableCollectionDescriptor
        {
            [XmlAttribute]
            public string Description { get; set; }
            public System.Windows.Controls.Dock? DockPosition { get; set; }
            [XmlAttribute]
            public DockableCollection.States State { get; set; }
            [XmlAttribute]
            public TabPositions TabPosition { get; set; }
            public double? Left;
            public double? Top;
            public double? LayoutHeight;
            public double? LayoutWidth;
            public double? Height;
            public double? Width;
            [XmlAttribute]
            public HorizontalAlignment HorizontalContentAlignment;
            [XmlAttribute]
            public VerticalAlignment VerticalContentAlignment;
            public List<int> BottomPeers { get; private set; }
            public List<int> LeftPeers { get; private set; }
            public List<int> RightPeers { get; private set; }
            public List<int> TopPeers { get; private set; }
            public List<DockableContentDescriptor> Content { get; private set; }
            [XmlAttribute]
            public int InternalId { get; set; }
            [XmlAttribute]
            public int CurrentTab { get; set; }
            [XmlAttribute]
            public System.Windows.Controls.Dock CollapsedTabPosition;
            [NonSerialized]
            [XmlIgnore]
#if JSON
            [JsonIgnore]
#endif
            public DockableCollection DockableCollection;

            public DockableCollectionDescriptor()
            {
                BottomPeers = new List<int>();
                LeftPeers = new List<int>();
                RightPeers = new List<int>();
                TopPeers = new List<int>();
                Content = new List<DockableContentDescriptor>();
            }

            public DockableCollectionDescriptor(DockableCollection dockableCollection, DockDescriptor dock, int internalId)
            {
                DockableCollection = dockableCollection;
                Description = dockableCollection.Description;
                DockPosition = DockingPanel.GetDockPosition(dockableCollection);
                State = dockableCollection.State;
                TabPosition = dockableCollection.TabPosition;
                HorizontalContentAlignment = dockableCollection.HorizontalContentAlignment;
                VerticalContentAlignment = dockableCollection.VerticalContentAlignment;

                if (dockableCollection.IsCollapsed)
                {
                    CollapsedTabPosition = dockableCollection.CollapsedTabPosition;
                }
                else
                {
                    LayoutContext layoutContext = DockingPanel.GetLayoutContext(dockableCollection);
                    Left = layoutContext.Left.Value;
                    Top = layoutContext.Top.Value;
                    LayoutHeight = layoutContext.Height.Value;
                    LayoutWidth = layoutContext.Width.Value;
                    Height = layoutContext.DockableCollection.Height;
                    Width = layoutContext.DockableCollection.Width;
                }

                BottomPeers = new List<int>();
                LeftPeers = new List<int>();
                RightPeers = new List<int>();
                TopPeers = new List<int>();
                Content = new List<DockableContentDescriptor>();

                InternalId = internalId;

                dock.DockedCollections.Add(this);

                int contentId = 1;
                foreach (FrameworkElement dockableContent in dockableCollection.Items)
                {
                    if (dockableCollection.VisibleContent == dockableContent)
                    {
                        CurrentTab = contentId;
                    }
                    new DockableContentDescriptor(dockableContent, this, contentId++);
                }
            }

            internal DockableCollection Create(Dock dock, DockableContextContentCreator contentCreator)
            {
                DockableCollection newCollection = new DockableCollection()
                {
                    Description = this.Description,
                    HorizontalContentAlignment = this.HorizontalContentAlignment,
                    State = this.State,
                    TabPosition = this.TabPosition,
                    VerticalContentAlignment = this.VerticalContentAlignment,
                };
                DockingPanel.SetDockPosition(newCollection, this.DockPosition);

                if (newCollection.IsCollapsed)
                {
                    newCollection.CollapsedTabPosition = this.CollapsedTabPosition;
                }

                foreach (DockableContentDescriptor desc in Content)
                {
                    FrameworkElement content = desc.RestoreDockedLayout(newCollection, contentCreator);
                    if (content != null && desc.InternalId == CurrentTab)
                    {
                        newCollection.SetVisibleContent(content);
                    }
                }
                dock.Items.Add(newCollection);

                if (!newCollection.IsCollapsed)
                {
                    LayoutContext layoutContext = DockingPanel.GetLayoutContext(newCollection);
                    layoutContext.Left = Left;
                    layoutContext.Top = Top;
                    layoutContext.Height = LayoutHeight;
                    layoutContext.Width = LayoutWidth;
                    layoutContext.DockableCollection.Height = Height.Value;
                    layoutContext.DockableCollection.Width = Width.Value;
                }

                return newCollection;
            }

            internal void ResolvePeerRelationships(Dictionary<int, DockableCollection> dockableCollections, DockableCollection dockableCollection)
            {
                LayoutContext dockableCollectionLayoutContext = DockingPanel.GetLayoutContext(dockableCollection);

                dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Clear();
                foreach (int internalId in BottomPeers)
                {
                    DockableCollection peer = dockableCollections[internalId];
                    LayoutContext peerLayoutContext = DockingPanel.GetLayoutContext(peer);
                    dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(peerLayoutContext);
                }
                dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Clear();
                foreach (int internalId in LeftPeers)
                {
                    DockableCollection peer = dockableCollections[internalId];
                    LayoutContext peerLayoutContext = DockingPanel.GetLayoutContext(peer);
                    dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(peerLayoutContext);
                }
                dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Clear();
                foreach (int internalId in RightPeers)
                {
                    DockableCollection peer = dockableCollections[internalId];
                    LayoutContext peerLayoutContext = DockingPanel.GetLayoutContext(peer);
                    dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(peerLayoutContext);
                }
                dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Clear();
                foreach (int internalId in TopPeers)
                {
                    DockableCollection peer = dockableCollections[internalId];
                    LayoutContext peerLayoutContext = DockingPanel.GetLayoutContext(peer);
                    dockableCollectionLayoutContext.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(peerLayoutContext);
                }
            }
        }

        [Serializable]
        public class DockableContentDescriptor
        {
            [XmlAttribute]
            public string TabText { get; set; }
            public SerializableColor Background { get; set; }
            public SerializableColor Foreground { get; set; }
            [XmlAttribute]
            public string TypeName { get; set; }
            [XmlAttribute]
            public string Description { get; set; }
            [XmlAttribute]
            public int InternalId { get; set; }

            public DockableContentDescriptor()
            {

            }

            public DockableContentDescriptor(FrameworkElement content, DockableCollectionDescriptor dockableCollection, int internalId)
            {
                TabText = DockingPanel.GetTabText(content);
                InternalId = internalId;

                SerializableColor? backgroundColor = GetColorFromContent(content, "Background");
                SerializableColor? foregroundColor = GetColorFromContent(content, "Foreground");

                if (backgroundColor.HasValue)
                {
                    Background = backgroundColor.Value;
                }
                if (foregroundColor.HasValue)
                {
                    Foreground = foregroundColor.Value;
                }

                TypeName = content.GetType().ToString();
                Description = DockingPanel.GetDescriptiveText(content);

                dockableCollection.Content.Add(this);
            }

            private SerializableColor? GetColorFromContent(FrameworkElement content, string property)
            {
                Type contentType = content.GetType();
                PropertyInfo propertyInfo = contentType.GetProperty(property, typeof(Brush));
                if (propertyInfo != null)
                {
                    Brush brush = propertyInfo.GetValue(content) as Brush;
                    if (brush != null)
                    {
                        if (brush is SolidColorBrush solidColorBrush)
                        {
                            return new SerializableColor(solidColorBrush.Color);
                        }
                        else
                        {
                            throw new NotImplementedException("Yawn.Layout.Save supports solid color brushes as values of the " + property + "property for " + content.GetType().ToString() + " objects");
                        }
                    }
                }

                return null;
            }

            internal FrameworkElement RestoreDockedLayout(DockableCollection newCollection, DockableContextContentCreator contentCreator)
            {
                FrameworkElement content = null;

                if (contentCreator != null)
                {
                    content = contentCreator.Invoke(TypeName, Description);
                    if (content == null)
                    {
                        return null;
                    }
                }

                SetContentColors(content, this.Background, this.Foreground);

                DockingPanel.SetTabText(content, TabText);
                DockingPanel.SetDescriptiveText(content, Description);

                newCollection.Items.Add(content);

                return content;
            }

            private void SetContentColors(FrameworkElement content, SerializableColor background, SerializableColor foreground)
            {
                Type contentType = content.GetType();
                PropertyInfo backgroundInfo = null;
                PropertyInfo foregroundInfo = null;

                if (!background.IsNull)
                {
                    backgroundInfo = contentType.GetProperty("Background", typeof(Brush));
                }
                if (!foreground.IsNull)
                {
                    foregroundInfo = contentType.GetProperty("Foreground", typeof(Brush));
                }

                if (!background.IsNull && backgroundInfo != null)
                {
                    backgroundInfo.SetValue(content, new SolidColorBrush(background));
                }

                if (!foreground.IsNull && foregroundInfo != null)
                {
                    foregroundInfo.SetValue(content, new SolidColorBrush(foreground));
                }
            }
        }
    }
}
