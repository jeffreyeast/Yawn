﻿//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Yawn
{
    internal abstract class AutoPositionerClient : AutoPositioner.IAutoPositionerClient
    {
        public double GetAvailableSpace(Silo silo, LayoutContext element, double baseCoordinate, double totalAvailableSpace, List<LayoutContext> elementsToBePositioned)
        {
            return MinBoundary(element, totalAvailableSpace) - baseCoordinate;
        }

        public abstract double GetDesiredSpace(LayoutContext element);

        public abstract LayoutContext GetNextDescendant(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned);

        public abstract double GetPeersMaxDesiredSpace(LayoutContext element, List<LayoutContext> elementsToBePositioned);

        public abstract double GetPreceedingCoordinate(LayoutContext element);

        public double GetVaryingSpace(Silo silo, double availableSpace, double minSpacePerElement, List<LayoutContext> varyingElements, List<LayoutContext> elementsToBePositioned)
        {
            //  Try to figure the amount of space available to all the remaining varying elements *at our level*

            double fixedSpace = 0;
            int varyingGroupCount = varyingElements.Count;

            GetFixedSpaceInternal(varyingElements.Last(), false, ref fixedSpace, ref varyingGroupCount, elementsToBePositioned);

            double varyingSpace = (availableSpace - fixedSpace) * ((double)varyingElements.Count) / (double)varyingGroupCount;
            return Math.Max(varyingSpace, varyingGroupCount * minSpacePerElement);
        }

        protected abstract void GetFixedSpaceInternal(LayoutContext root, bool inVaryingGroup, ref double fixedSpace, ref int varyingGroupCount, List<LayoutContext> elementsToBePositioned);

        public abstract int GetRemainingDepth(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned);

        public abstract bool IsStretchable(LayoutContext element);

        protected abstract double MinBoundary(LayoutContext element, double edgeBoundary);

        public abstract void SetPosition(Silo silo, LayoutContext element, double coordinate, double size, List<LayoutContext> elementsToBePositioned);
    }




    internal class HorizontalClient : AutoPositionerClient
    {
        public override double GetDesiredSpace(LayoutContext element)
        {
            return element.Width.HasValue ? element.Width.Value : element.DockableCollection.DesiredSize.Width;
        }

        public override LayoutContext GetNextDescendant(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned)
        {
            foreach (var peer in element.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                if (elementsToBePositioned.Contains(peer))
                {
                    return peer;
                }
            }
            return null;
        }

        protected override void GetFixedSpaceInternal(LayoutContext root, bool inVaryingGroup, ref double fixedSpace, ref int varyingGroupCount, List<LayoutContext> elementsToBePositioned)
        {
            double subtreesFixedSpace = 0;
            int subtreesVaryingGroupCount = 0;

            foreach (LayoutContext peer in root.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                if (elementsToBePositioned.Contains(peer))
                {
                    double thisPeersSubtreesFixedSpace = 0;
                    int thisPeersSubtreesVaryingGroupCount = 0;
                    bool thisPeersSubtreeInVaryingGroup = inVaryingGroup;

                    if (IsStretchable(peer))
                    {
                        varyingGroupCount += inVaryingGroup ? 1 : 0;
                        thisPeersSubtreeInVaryingGroup = true;
                    }
                    else
                    {
                        thisPeersSubtreeInVaryingGroup = false;
                        thisPeersSubtreesFixedSpace += GetDesiredSpace(peer);
                    }

                    GetFixedSpaceInternal(peer, thisPeersSubtreeInVaryingGroup, ref thisPeersSubtreesFixedSpace, ref thisPeersSubtreesVaryingGroupCount, elementsToBePositioned);

                    subtreesFixedSpace = Math.Max(subtreesFixedSpace, thisPeersSubtreesFixedSpace);
                    subtreesVaryingGroupCount = Math.Max(subtreesVaryingGroupCount, thisPeersSubtreesVaryingGroupCount);
                }
            }

            fixedSpace += subtreesFixedSpace;
            varyingGroupCount += subtreesVaryingGroupCount;
        }

        public override double GetPeersMaxDesiredSpace(LayoutContext element, List<LayoutContext> elementsToBePositioned)
        {
            if (element.Width.HasValue)
            {
                return element.Width.Value;
            }
            else
            {
                double maxDesiredSpace = 0;

                foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Left].InteriorLogicalEdge)
                {
                    if (elementsToBePositioned.Contains(peer))
                    {
                        maxDesiredSpace = Math.Max(maxDesiredSpace, GetDesiredSpace(peer));
                    }
                }

                return maxDesiredSpace;
            }
        }

        public override double GetPreceedingCoordinate(LayoutContext element)
        {
            LayoutContext preceedingPeer = element.Edges[System.Windows.Controls.Dock.Left].LogicalNeighbors.FirstOrDefault();
            if (preceedingPeer == null)
            {
                return 0;
            }
            else
            {
                if (preceedingPeer.Right.HasValue)
                {
                    return preceedingPeer.Right.Value;
                }
                else
                {
                    throw new InvalidOperationException("Unpositioned preceeding element");
                }
            }
        }

        public override int GetRemainingDepth(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned)
        {
            int maxPeerCount = 0;

            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                if (peer.Left.HasValue)
                {
                    return maxPeerCount;
                }
            }

            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                if (elementsToBePositioned.Contains(peer))
                {
                    int peerCount = GetRemainingDepth(silo, peer, elementsToBePositioned) + 1;
                    maxPeerCount = Math.Max(maxPeerCount, peerCount);
                }
            }

            return maxPeerCount;
        }

        public override bool IsStretchable(LayoutContext element)
        {
            return double.IsNaN(element.DockableCollection.Width) &&
                element.DockableCollection is DockableCollection dockableCollection ? dockableCollection.HorizontalContentAlignment == System.Windows.HorizontalAlignment.Stretch : false;
        }

        protected override double MinBoundary(LayoutContext element, double edgeBoundary)
        {
            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                if (peer.Left.HasValue)
                {
                    return peer.Left.Value;
                }
            }

            double minBoundary = edgeBoundary;
            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                double boundary = MinBoundary(peer, edgeBoundary);
                minBoundary = Math.Min(minBoundary, boundary);
            }

            return minBoundary;
        }

        public override void SetPosition(Silo silo, LayoutContext element, double coordinate, double size, List<LayoutContext> elementsToBePositioned)
        {
            element.Left = coordinate;
            element.Width = size;
        }
    }






    internal class VerticalClient : AutoPositionerClient
    {
        public override double GetDesiredSpace(LayoutContext element)
        {
            return element.Height.HasValue ? element.Height.Value : element.DockableCollection.DesiredSize.Height;
        }

        protected override void GetFixedSpaceInternal(LayoutContext root, bool inVaryingGroup, ref double fixedSpace, ref int varyingGroupCount, List<LayoutContext> elementsToBePositioned)
        {
            double subtreesFixedSpace = 0;
            int subtreesVaryingGroupCount = 0;

            foreach (LayoutContext peer in root.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                if (elementsToBePositioned.Contains(peer))
                {
                    double thisPeersSubtreesFixedSpace = 0;
                    int thisPeersSubtreesVaryingGroupCount = 0;
                    bool thisPeersSubtreeInVaryingGroup = inVaryingGroup;

                    if (IsStretchable(peer))
                    {
                        varyingGroupCount += inVaryingGroup ? 1 : 0;
                        thisPeersSubtreeInVaryingGroup = true;
                    }
                    else
                    {
                        thisPeersSubtreeInVaryingGroup = false;
                        thisPeersSubtreesFixedSpace += GetDesiredSpace(peer);
                    }

                    GetFixedSpaceInternal(peer, thisPeersSubtreeInVaryingGroup, ref thisPeersSubtreesFixedSpace, ref thisPeersSubtreesVaryingGroupCount, elementsToBePositioned);

                    subtreesFixedSpace = Math.Max(subtreesFixedSpace, thisPeersSubtreesFixedSpace);
                    subtreesVaryingGroupCount = Math.Max(subtreesVaryingGroupCount, thisPeersSubtreesVaryingGroupCount);
                }
            }

            fixedSpace += subtreesFixedSpace;
            varyingGroupCount += subtreesVaryingGroupCount;
        }

        public override LayoutContext GetNextDescendant(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned)
        {
            foreach (var peer in element.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                if (elementsToBePositioned.Contains(peer))
                {
                    return peer;
                }
            }
            return null;
        }

        public override double GetPeersMaxDesiredSpace(LayoutContext element, List<LayoutContext> elementsToBePositioned)
        {
            if (element.Height.HasValue)
            {
                return element.Height.Value;
            }
            else
            {
                double maxDesiredSpace = 0;

                foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Top].InteriorLogicalEdge)
                {
                    if (elementsToBePositioned.Contains(peer))
                    {
                        maxDesiredSpace = Math.Max(maxDesiredSpace, GetDesiredSpace(peer));
                    }
                }

                return maxDesiredSpace;
            }
        }

        public override double GetPreceedingCoordinate(LayoutContext element)
        {
            LayoutContext preceedingPeer = element.Edges[System.Windows.Controls.Dock.Top].LogicalNeighbors.FirstOrDefault();
            if (preceedingPeer == null)
            {
                return 0;
            }
            else
            {
                if (preceedingPeer.Bottom.HasValue)
                {
                    return preceedingPeer.Bottom.Value;
                }
                else
                {
                    throw new InvalidOperationException("Unpositioned preceeding element");
                }
            }
        }

        public override int GetRemainingDepth(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned)
        {
            int maxPeerCount = 0;

            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                if (peer.Top.HasValue)
                {
                    return maxPeerCount;
                }
            }

            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                if (elementsToBePositioned.Contains(peer))
                {
                    int peerCount = GetRemainingDepth(silo, peer, elementsToBePositioned) + 1;
                    maxPeerCount = Math.Max(maxPeerCount, peerCount);
                }
            }

            return maxPeerCount;
        }

        public override bool IsStretchable(LayoutContext element)
        {
            return double.IsNaN(element.DockableCollection.Height) &&
                element.DockableCollection is DockableCollection dockableCollection ? dockableCollection.VerticalContentAlignment == System.Windows.VerticalAlignment.Stretch : false;
        }

        protected override double MinBoundary(LayoutContext element, double edgeBoundary)
        {
            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                if (peer.Top.HasValue)
                {
                    return peer.Top.Value;
                }
            }

            double minBoundary = edgeBoundary;
            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                double boundary = MinBoundary(peer, edgeBoundary);
                minBoundary = Math.Min(minBoundary, boundary);
            }

            return minBoundary;
        }

        public override void SetPosition(Silo silo, LayoutContext element, double coordinate, double size, List<LayoutContext> elementsToBePositioned)
        {
            element.Top = coordinate;
            element.Height = size;
        }
    }
}