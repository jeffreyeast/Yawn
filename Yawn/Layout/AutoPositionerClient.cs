//  Copyright (c) 2020 Jeff East
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

        public abstract double GetDesiredSpace(LayoutContext element, double minimumSize);

        public abstract LayoutContext GetNextDescendant(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned);

        public abstract double GetPeersMaxDesiredSpace(LayoutContext element, double minimumSize, IEnumerable<LayoutContext> elementsToBePositioned);

        public abstract double GetPreceedingCoordinate(LayoutContext element);

        public double GetVaryingSpace(Silo silo, double availableSpace, double minSpacePerElement, List<LayoutContext> varyingElements, List<LayoutContext> elementsToBePositioned)
        {
            //  Try to figure the amount of space available to all the remaining varying elements *at our level*

            double fixedSpace = 0;
            int varyingGroupCount = 1;

            GetFixedSpaceInternal(varyingElements.Last(), false, ref fixedSpace, ref varyingGroupCount, minSpacePerElement, elementsToBePositioned);

            double varyingSpace = (availableSpace - fixedSpace) * ((double)varyingElements.Count) / (double)(varyingElements.Count + varyingGroupCount - 1);
            return Math.Max(varyingSpace, varyingGroupCount * minSpacePerElement);
        }

        protected abstract void GetFixedSpaceInternal(LayoutContext root, bool inVaryingGroup, ref double fixedSpace, ref int varyingGroupCount, double minimumSize, List<LayoutContext> elementsToBePositioned);

        public abstract int GetRemainingDepth(Silo silo, LayoutContext element, List<LayoutContext> elementsToBePositioned);

        public abstract bool IsStretchable(LayoutContext element);

        protected abstract double MinBoundary(LayoutContext element, double edgeBoundary);

        public abstract void SetPosition(Silo silo, LayoutContext element, double coordinate, double size, List<LayoutContext> elementsToBePositioned);
    }




    internal class HorizontalClient : AutoPositionerClient
    {
        public override double GetDesiredSpace(LayoutContext element, double minimumSize)
        {
            double desiredSpace = Math.Max(element.Size.Width.HasInternalValue ? element.Size.Width.InternalValue : element.DockableCollection.DesiredSize.Width, minimumSize);
            return desiredSpace;
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

        protected override void GetFixedSpaceInternal(LayoutContext root, bool inVaryingGroup, ref double fixedSpace, ref int varyingGroupCount, double minimumSize, List<LayoutContext> elementsToBePositioned)
        {
            foreach (LayoutContext peer in root.Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                if (peer.Left.HasValue)
                {
                    return;
                }
            }

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
                        thisPeersSubtreesFixedSpace += GetDesiredSpace(peer, minimumSize);
                    }

                    GetFixedSpaceInternal(peer, thisPeersSubtreeInVaryingGroup, ref thisPeersSubtreesFixedSpace, ref thisPeersSubtreesVaryingGroupCount, minimumSize, elementsToBePositioned);

                    subtreesFixedSpace = Math.Max(subtreesFixedSpace, thisPeersSubtreesFixedSpace);
                    subtreesVaryingGroupCount = Math.Max(subtreesVaryingGroupCount, thisPeersSubtreesVaryingGroupCount);
                }
            }

            fixedSpace += subtreesFixedSpace;
            varyingGroupCount += subtreesVaryingGroupCount;
        }

        public override double GetPeersMaxDesiredSpace(LayoutContext element, double minimumSize, IEnumerable<LayoutContext> elementsToBePositioned)
        {
            double maxDesiredSpace = 0;

            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Left].InteriorLogicalEdge)
            {
                if (elementsToBePositioned.Contains(peer) && element.Edges[System.Windows.Controls.Dock.Right].InteriorLogicalEdge.Contains(peer))
                {
                    maxDesiredSpace = Math.Max(maxDesiredSpace, GetDesiredSpace(peer, minimumSize));
                }
            }

            return maxDesiredSpace;
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
            return !element.Size.Width.HasUserValue && !element.Size.Width.IsSplitterActive &&
                element.DockableCollection.HorizontalContentAlignment == System.Windows.HorizontalAlignment.Stretch;
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
            element.Size.Width.SetInternalValue(size);
        }
    }






    internal class VerticalClient : AutoPositionerClient
    {
        public override double GetDesiredSpace(LayoutContext element, double minimumSize)
        {
            double desiredSize = Math.Max(element.Size.Height.HasInternalValue ? element.Size.Height.InternalValue : element.DockableCollection.DesiredSize.Height, minimumSize);
            return desiredSize;
        }

        protected override void GetFixedSpaceInternal(LayoutContext root, bool inVaryingGroup, ref double fixedSpace, ref int varyingGroupCount, double minimumSize, List<LayoutContext> elementsToBePositioned)
        {
            foreach (LayoutContext peer in root.Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                if (peer.Top.HasValue)
                {
                    return;
                }
            }

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
                        thisPeersSubtreesFixedSpace += GetDesiredSpace(peer, minimumSize);
                    }

                    GetFixedSpaceInternal(peer, thisPeersSubtreeInVaryingGroup, ref thisPeersSubtreesFixedSpace, ref thisPeersSubtreesVaryingGroupCount, minimumSize, elementsToBePositioned);

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

        public override double GetPeersMaxDesiredSpace(LayoutContext element, double minimumSize, IEnumerable<LayoutContext> elementsToBePositioned)
        {
            double maxDesiredSpace = 0;

            foreach (LayoutContext peer in element.Edges[System.Windows.Controls.Dock.Top].InteriorLogicalEdge)
            {
                if (elementsToBePositioned.Contains(peer) && element.Edges[System.Windows.Controls.Dock.Bottom].InteriorLogicalEdge.Contains(peer))
                {
                    maxDesiredSpace = Math.Max(maxDesiredSpace, GetDesiredSpace(peer, minimumSize));
                }
            }

            return maxDesiredSpace;
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
            return !element.Size.Height.HasUserValue && !element.Size.Height.IsSplitterActive &&
                element.DockableCollection.VerticalContentAlignment == System.Windows.VerticalAlignment.Stretch;
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
            element.Size.Height.SetInternalValue(size);
        }
    }
}
