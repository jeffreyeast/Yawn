//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yawn
{
    /// <summary>
    /// Every DockableCollection has 4 edges (Left, Top, Right, Bottom). This class represents the state,
    /// both physical and logical, associated with that edge.
    /// For non-collapsed collections, physical and virtual are identical. But for collapsed collections, they
    /// differ.
    /// </summary>
    public class DockableCollectionEdge
    {
        LayoutContext LayoutContext { get; set; }
        DockableCollection DockableCollection => LayoutContext.DockableCollection;
        internal System.Windows.Controls.Dock EdgesDockPosition { get; private set; }
        internal LinkedList<LayoutContext> PhysicalNeighbors { get; private set; }
        internal LinkedList<LayoutContext> LogicalNeighbors
        {
            get
            {
                ComputeLogicalRelationships(RelationshipStatus.NeighborsKnown);
                foreach (LayoutContext layoutContext in _logicalNeighbors)
                {
                    if (layoutContext.DockableCollection.IsCollapsed)
                    {
                        throw new InvalidProgramException();
                    }
                }

                return _logicalNeighbors;
            }
        }
        LinkedList<LayoutContext> _logicalNeighbors;
        internal LinkedList<LayoutContext> LogicalReplacements
        {
            get
            {
                ComputeLogicalRelationships(RelationshipStatus.ReplacementsKnown);
                if (_logicalReplacements != null)
                {
                    foreach (LayoutContext layoutContext in _logicalReplacements)
                    {
                        if (layoutContext.DockableCollection.IsCollapsed)
                        {
                            throw new InvalidProgramException();
                        }
                    }
                }

                return _logicalReplacements;
            }
        }
        LinkedList<LayoutContext> _logicalReplacements;
        internal LinkedList<LayoutContext> InteriorLogicalEdge
        {
            get
            {
                ComputeLogicalRelationships(RelationshipStatus.EdgesKnown);
                return _interiorLogicalEdge;
            }
        }
        LinkedList<LayoutContext> _interiorLogicalEdge;
        internal LinkedList<LayoutContext> ExteriorLogicalEdge
        {
            get
            {
                ComputeLogicalRelationships(RelationshipStatus.EdgesKnown);
                return _exteriorLogicalEdge;
            }
        }
        LinkedList<LayoutContext> _exteriorLogicalEdge;
        internal LinkedList<LayoutContext> InteriorPhysicalEdge
        {
            get
            {
                if (_interiorPhysicalEdge == null)
                {
                    ComputePhysicalEdges();
                }
                return _interiorPhysicalEdge;
            }
        }
        LinkedList<LayoutContext> _interiorPhysicalEdge;
        internal LinkedList<LayoutContext> ExteriorPhysicalEdge
        {
            get
            {
                if (_exteriorPhysicalEdge == null)
                {
                    ComputePhysicalEdges();
                }
                return _exteriorPhysicalEdge;
            }
        }
        LinkedList<LayoutContext> _exteriorPhysicalEdge;

        internal event EventHandler EdgeRecomputed;



        internal DockableCollectionEdge(LayoutContext layoutContext, System.Windows.Controls.Dock edgePosition)
        {
            LayoutContext = layoutContext;
            EdgesDockPosition = edgePosition;
            PhysicalNeighbors = new LinkedList<LayoutContext>();
        }

        private LayoutContext ComputeEdgesFirstLogicalMember(System.Windows.Controls.Dock edgesDockPosition)
        {
            if (DockableCollection.IsCollapsed)
            {
                throw new InvalidProgramException();
            }

            foreach (LayoutContext physicalCandidate in InteriorPhysicalEdge)
            {
                if (physicalCandidate.DockableCollection.IsCollapsed)
                {
                    physicalCandidate.ComputeLogicalReplacements(out LinkedList<LayoutContext> contactingEdgeCandidates, out LinkedList<LayoutContext> opposingEdgeCandidates, edgesDockPosition);
                    if (opposingEdgeCandidates != null)
                    {
                        foreach (LayoutContext noncollapsedCandidate in opposingEdgeCandidates)
                        {
                            if (!noncollapsedCandidate.DockableCollection.IsCollapsed)
                            {
                                return noncollapsedCandidate;
                            }
                        }
                    }
                }
                else
                {
                    return physicalCandidate;
                }
            }

            throw new InvalidProgramException();
        }

        private LayoutContext ComputeEdgesFirstPhysicalMember(System.Windows.Controls.Dock edgesDockPosition)
        {
            LayoutContext currentCollection = LayoutContext;

            while (true)
            {
                LayoutContext opposingCollection = currentCollection.Edges[edgesDockPosition].PhysicalNeighbors.FirstOrDefault();
                if (opposingCollection == null)
                {
                    break;
                }
                LayoutContext nextLayoutContext = opposingCollection.Edges[LayoutContext.OpposingNeighbors[edgesDockPosition]].PhysicalNeighbors.FirstOrDefault();
                if (nextLayoutContext == currentCollection)
                {
                    break;
                }
                currentCollection = nextLayoutContext;
            }

            return currentCollection;
        }

        private void ComputeLogicalEdges()
        {
            _exteriorLogicalEdge = new LinkedList<LayoutContext>();
            _interiorLogicalEdge = new LinkedList<LayoutContext>();

            if (!DockableCollection.IsCollapsed)
            {
                if (LogicalNeighbors.Count == 0)
                {
                    ComputeLogicalOutwardEdges();
                }
                else
                {
                    ComputeLogicalInwardEdges();
                }
            }
        }

        private void ComputeLogicalInwardEdges()
        {
            HashSet<LayoutContext> processedCollections = new HashSet<LayoutContext>();
            LayoutContext firstInteriorCollection = ComputeEdgesFirstLogicalMember(EdgesDockPosition);

            _interiorLogicalEdge.AddFirst(firstInteriorCollection);
            processedCollections.Add(firstInteriorCollection);

            LinkedListNode<LayoutContext> interiorNode = _interiorLogicalEdge.First;

            do
            {
                LinkedListNode<LayoutContext> exteriorNode = interiorNode.Value.Edges[EdgesDockPosition].LogicalNeighbors.First;

                while (exteriorNode != null)
                {
                    if (!processedCollections.Contains(exteriorNode.Value))
                    {
                        _exteriorLogicalEdge.AddLast(exteriorNode.Value);
                        processedCollections.Add(exteriorNode.Value);

                        foreach (LayoutContext interiorCollection in exteriorNode.Value.Edges[LayoutContext.OpposingNeighbors[EdgesDockPosition]].LogicalNeighbors)
                        {
                            if (!processedCollections.Contains(interiorCollection))
                            {
                                _interiorLogicalEdge.AddLast(interiorCollection);
                                processedCollections.Add(interiorCollection);
                            }
                        }
                    }

                    exteriorNode = exteriorNode.Next;
                }

                interiorNode = _interiorLogicalEdge.Last;
            } while (interiorNode.Value.Edges[EdgesDockPosition].LogicalNeighbors.Last != null &&
                        !processedCollections.Contains(interiorNode.Value.Edges[EdgesDockPosition].LogicalNeighbors.Last.Value));
        }

        private void ComputeLogicalNeighbors()
        {
            //  Determine if any collection in our physical neighbor list is collapsed

            bool isAnyNeighborCollapsed = false;

            foreach (LayoutContext neighbor in PhysicalNeighbors)
            {
                if (neighbor.DockableCollection.IsCollapsed)
                {
                    isAnyNeighborCollapsed = true;
                    break;
                }
            }

            if (isAnyNeighborCollapsed)
            {
                //  At least one neighbor is collapsed. There are two cases
                //  1.  A orthogonal peer of the neighbor will expand to cover the neighbor, in which case we can ignore the neighbor
                //  2.  One or more anticedents will expand, in which case they become logical neighbors.

                _logicalNeighbors = new LinkedList<LayoutContext>();
                foreach (LayoutContext neighbor in PhysicalNeighbors)
                {
                    if (neighbor.DockableCollection.IsCollapsed)
                    {
                        if (neighbor.Edges[EdgesDockPosition].LogicalReplacements != null)
                        {
#if DEBUG
                            //  The adjacent and opposing replacement sets should be identical

                            Debug.Assert(neighbor.Edges[LayoutContext.OpposingNeighbors[EdgesDockPosition]].LogicalReplacements != null);
                            LinkedListNode<LayoutContext> adjacentLink = neighbor.Edges[EdgesDockPosition].LogicalReplacements.First;
                            LinkedListNode<LayoutContext> opposingLink = neighbor.Edges[LayoutContext.OpposingNeighbors[EdgesDockPosition]].LogicalReplacements.First;
                            while (adjacentLink != null || opposingLink != null)
                            {
                                Debug.Assert(adjacentLink != null && opposingLink != null);
                                Debug.Assert(adjacentLink.Value == opposingLink.Value);
                                adjacentLink = adjacentLink.Next;
                                opposingLink = opposingLink.Next;
                            }
#endif
                            //  If we have a replacement that is reachable, that replacement is the neighbor

                            bool foundLogicalNeighbors = false;
                            foreach (LayoutContext replacement in neighbor.Edges[EdgesDockPosition].LogicalReplacements)
                            {
                                if (LayoutContext.IsPhysicallyReachable(replacement, EdgesDockPosition))
                                {
                                    foundLogicalNeighbors = true;
                                    if (!_logicalNeighbors.Contains(replacement))
                                    {
                                        if (replacement.DockableCollection.IsCollapsed)
                                        {
                                            throw new InvalidProgramException();
                                        }
                                        _logicalNeighbors.AddLast(replacement);
                                    }
                                }
                            }

                            //  If we found no replacement, then the neighbors are the logical neighbors of the collapsed member

                            if (!foundLogicalNeighbors)
                            {
                                foreach (var logicalNeighbor in neighbor.Edges[EdgesDockPosition].LogicalNeighbors)
                                {
                                    if (!_logicalNeighbors.Contains(logicalNeighbor))
                                    {
                                        if (logicalNeighbor.DockableCollection.IsCollapsed)
                                        {
                                            throw new InvalidProgramException();
                                        }
                                        _logicalNeighbors.AddLast(logicalNeighbor);
                                    }
                                }
                            }
                        }
                    }
                    else if (!_logicalNeighbors.Contains(neighbor))
                    {
                        _logicalNeighbors.AddLast(neighbor);
                    }
                }
            }
            else
            {
                _logicalNeighbors = PhysicalNeighbors;
            }
        }

        private void ComputeLogicalOutwardEdges()
        {
            //  This turns out to be surprisingly difficult. 
            //  Gather the set of edge members and insert them into the list top-to-bottom, left-to-right

            foreach (LayoutContext layoutContext in LayoutContext.DockingPanel.LayoutContexts)
            {
                //  Check if the member is non-collapsed and on an edge

                if (!layoutContext.DockableCollection.IsCollapsed &&
                    layoutContext.Edges[EdgesDockPosition].LogicalNeighbors.Count == 0)
                {
                    // It qualified, find it's insertion position (left to right, top to bottom)

                    for (LinkedListNode<LayoutContext> node = _interiorLogicalEdge.First; node != null; node = node.Next)
                    {
                        if (layoutContext.IsLogicalPreceeding(node.Value, EdgesDockPosition))
                        {
                            _interiorLogicalEdge.AddBefore(node, layoutContext);
                            goto nextNode;
                        }
                    }

                    _interiorLogicalEdge.AddLast(layoutContext);

                nextNode:;
                }
            }
        }

        enum RelationshipStatus
        {
            NotBuiltYet,
            ReplacementsKnown,
            NeighborsKnown,
            EdgesKnown,
        }
        enum RelationshipProgress
        {
            Nothing,
            LogicalReplacements,
            LogicalNeighbors,
            LogicalEdges,
        }
        RelationshipStatus CurrentRelationshipLevel = RelationshipStatus.NotBuiltYet;
        RelationshipProgress CurrentRelationshipProgress = RelationshipProgress.Nothing;

        private void ComputeLogicalRelationships(RelationshipStatus requiredLevel)
        {
            while (CurrentRelationshipLevel < requiredLevel)
            {
                switch (CurrentRelationshipLevel)
                {
                    case RelationshipStatus.NotBuiltYet:
                        if (CurrentRelationshipProgress == RelationshipProgress.LogicalReplacements)
                        {
                            throw new InvalidOperationException("Recursion not supported");
                        }
                        CurrentRelationshipProgress = RelationshipProgress.LogicalReplacements;
                        ComputeLogicalReplacements();
                        CurrentRelationshipLevel = RelationshipStatus.ReplacementsKnown;
                        CurrentRelationshipProgress = RelationshipProgress.Nothing;
                        break;

                    case RelationshipStatus.ReplacementsKnown:
                        if (CurrentRelationshipProgress == RelationshipProgress.LogicalNeighbors)
                        {
                            throw new InvalidOperationException("Recursion not supported");
                        }
                        CurrentRelationshipProgress = RelationshipProgress.LogicalNeighbors;
                        ComputeLogicalNeighbors();
                        CurrentRelationshipLevel = RelationshipStatus.NeighborsKnown;
                        CurrentRelationshipProgress = RelationshipProgress.Nothing;
                        break;

                    case RelationshipStatus.NeighborsKnown:
                        if (CurrentRelationshipProgress == RelationshipProgress.LogicalEdges)
                        {
                            throw new InvalidOperationException("Recursion not supported");
                        }
                        CurrentRelationshipProgress = RelationshipProgress.LogicalEdges;
                        ComputeLogicalEdges();
                        CurrentRelationshipLevel = RelationshipStatus.EdgesKnown;
                        CurrentRelationshipProgress = RelationshipProgress.Nothing;
                        EdgeRecomputed?.Invoke(this, null);
                        break;

                    case RelationshipStatus.EdgesKnown:
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void ComputeLogicalReplacements()
        {
            if (DockableCollection.IsCollapsed)
            {
                //  The space previously occupied by the compressed collection will fill filled by other collection(s). This
                //  will occur on either the vertical or horizontal axes. If this edge is on the edge of expansion, then the
                //  visible edge will be the expanding collections. Otherwise, it will be empty.
                //
                //  The edge(s) through which expansion is likely to occur owns the replacement. Other edges will have a null replacement.

                LayoutContext.ComputeLogicalReplacements(out LinkedList<LayoutContext> contactingEdgeReplacements, out LinkedList<LayoutContext> opposingEdgeReplacements, LayoutContext.OpposingNeighbors[EdgesDockPosition]);
                if (contactingEdgeReplacements == null)
                {
                    _logicalReplacements = opposingEdgeReplacements;
                }
                else if (opposingEdgeReplacements == null)
                {
                    _logicalReplacements = contactingEdgeReplacements;
                }
                else
                {
                    _logicalReplacements = new LinkedList<LayoutContext>();
                    switch (EdgesDockPosition)
                    {
                        case System.Windows.Controls.Dock.Left:
                        case System.Windows.Controls.Dock.Top:
                            foreach (var entry in contactingEdgeReplacements)
                            {
                                _logicalReplacements.AddLast(entry);
                            }
                            foreach (var entry in opposingEdgeReplacements)
                            {
                                _logicalReplacements.AddLast(entry);
                            }
                            break;

                        default:
                            foreach (var entry in opposingEdgeReplacements)
                            {
                                _logicalReplacements.AddLast(entry);
                            }
                            foreach (var entry in contactingEdgeReplacements)
                            {
                                _logicalReplacements.AddLast(entry);
                            }
                            break;
                    }
                }
            }
            else
            {
                _logicalReplacements = null;
            }
        }

        /// <summary>
        /// Computes physical edge members for any side of a collection
        /// </summary>
        private void ComputePhysicalEdges()
        {
            if (PhysicalNeighbors.First == null)
            {
                ComputePhysicalOutwardEdges();
            }
            else
            {
                ComputePhysicalInwardEdges();
            }
        }

        /// <summary>
        /// Computes physical edge members for a side that is not on the edge of the dock.
        /// </summary>
        private void ComputePhysicalInwardEdges()
        {
            _exteriorPhysicalEdge = new LinkedList<LayoutContext>();
            _interiorPhysicalEdge = new LinkedList<LayoutContext>();

            HashSet<LayoutContext> processedCollections = new HashSet<LayoutContext>();
            LayoutContext firstInteriorCollection = ComputeEdgesFirstPhysicalMember(EdgesDockPosition);

            _interiorPhysicalEdge.AddFirst(firstInteriorCollection);
            processedCollections.Add(firstInteriorCollection);

            LinkedListNode<LayoutContext> interiorNode = _interiorPhysicalEdge.First;

            do
            {
                LinkedListNode<LayoutContext> exteriorNode = interiorNode.Value.Edges[EdgesDockPosition].PhysicalNeighbors.First;

                while (exteriorNode != null)
                {
                    if (!processedCollections.Contains(exteriorNode.Value))
                    {
                        _exteriorPhysicalEdge.AddLast(exteriorNode.Value);
                        processedCollections.Add(exteriorNode.Value);

                        foreach (LayoutContext interiorCollection in exteriorNode.Value.Edges[LayoutContext.OpposingNeighbors[EdgesDockPosition]].PhysicalNeighbors)
                        {
                            if (!processedCollections.Contains(interiorCollection))
                            {
                                _interiorPhysicalEdge.AddLast(interiorCollection);
                                processedCollections.Add(interiorCollection);
                            }
                        }
                    }

                    exteriorNode = exteriorNode.Next;
                }

                interiorNode = _interiorPhysicalEdge.Last;
            } while (interiorNode.Value.Edges[EdgesDockPosition].PhysicalNeighbors.Last != null &&
                     !processedCollections.Contains(interiorNode.Value.Edges[EdgesDockPosition].PhysicalNeighbors.Last.Value));
        }

        /// <summary>
        /// Computes physical edge members for a side that is on the edge of the dock.
        /// </summary>
        private void ComputePhysicalOutwardEdges()
        {
            LayoutContext peer;

            _exteriorPhysicalEdge = new LinkedList<LayoutContext>();
            _interiorPhysicalEdge = new LinkedList<LayoutContext>();

            switch (EdgesDockPosition)
            {
                case System.Windows.Controls.Dock.Bottom:
                    peer = LayoutContext.BottomLeftMostChild(LayoutContext);
                    break;
                case System.Windows.Controls.Dock.Right:
                    peer = LayoutContext.TopRightMostChild(LayoutContext);
                    break;
                case System.Windows.Controls.Dock.Left:
                case System.Windows.Controls.Dock.Top:
                    peer = LayoutContext.TopLeftMostChild(LayoutContext);
                    break;
                default:
                    throw new NotImplementedException();
            }

            do
            {
                _interiorPhysicalEdge.AddLast(peer);
                switch (EdgesDockPosition)
                {
                    case System.Windows.Controls.Dock.Bottom:
                    case System.Windows.Controls.Dock.Right:
                        peer = peer.Edges[LayoutContext.MaximumOrthogonalEdge[EdgesDockPosition]].PhysicalNeighbors.LastOrDefault();
                        break;
                    case System.Windows.Controls.Dock.Left:
                    case System.Windows.Controls.Dock.Top:
                        peer = peer.Edges[LayoutContext.MaximumOrthogonalEdge[EdgesDockPosition]].PhysicalNeighbors.FirstOrDefault();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            } while (peer != null);
        }

        internal void DumpEdges(bool force = false, string header = null)
        {
            if (force)
            {
                ComputePhysicalEdges();
                ComputeLogicalRelationships(RelationshipStatus.EdgesKnown);
            }

            if (CurrentRelationshipLevel == RelationshipStatus.NotBuiltYet)
            {
                Debug.Print(header + EdgesDockPosition.ToString() + ": State: " + CurrentRelationshipLevel.ToString());
            }
            else
            {
                DumpEdgesInternal(header);
            }
        }

        private void DumpEdgesInternal(string header)
        {
            if (header == null)
            {
                header = LayoutContext.ToString() + " ";
            }

            string buffer = header + EdgesDockPosition.ToString() + ": State: " + CurrentRelationshipLevel.ToString() + "; Physical Neighbors: ";
            string separator = "";
            if (PhysicalNeighbors == null)
            {
                buffer += "<null>";
            }
            else
            {
                foreach (LayoutContext layoutContext in PhysicalNeighbors)
                {
                    buffer += separator + "[" + layoutContext.ToString() + "]";
                    separator = ",";
                }
            }

            buffer += "; Int Phys Edge: ";
            separator = "";
            if (_interiorPhysicalEdge == null)
            {
                buffer += "<null>";
            }
            else
            {
                foreach (LayoutContext layoutContext in _interiorPhysicalEdge)
                {
                    buffer += separator + "[" + layoutContext.ToString() + "]";
                    separator = ",";
                }
            }

            buffer += "; Ext Phys Edge: ";
            separator = "";
            if (_exteriorPhysicalEdge == null)
            {
                buffer += "<null>";
            }
            else
            {
                foreach (LayoutContext layoutContext in _exteriorPhysicalEdge)
                {
                    buffer += separator + "[" + layoutContext.ToString() + "]";
                    separator = ",";
                }
            }
            Debug.Print(buffer);

            buffer = "        Logical Neighbors: ";
            separator = "";
            if (_logicalNeighbors == null)
            {
                buffer += "<null>";
            }   
            else
            {
                foreach (LayoutContext layoutContext in _logicalNeighbors)
                {
                    buffer += separator + "[" + layoutContext.ToString() + "]";
                    separator = ",";
                }
            }

            buffer += "; Log Replacements: ";
            separator = "";
            if (_logicalReplacements == null)
            {
                buffer += "<null>";
            }
            else
            {
                foreach (LayoutContext layoutContext in _logicalReplacements)
                {
                    buffer += separator + "[" + layoutContext.ToString() + "]";
                    separator = ",";
                }
            }

            buffer += "; Int Log Edge: ";
            separator = "";
            if (_interiorLogicalEdge == null)
            {
                buffer += "<null>";
            }
            else
            {
                foreach (LayoutContext layoutContext in _interiorLogicalEdge)
                {
                    buffer += separator + "[" + layoutContext.ToString() + "]";
                    separator = ",";
                }
            }

            buffer += "; Ext Log Edge: ";
            separator = "";
            if (_exteriorLogicalEdge == null)
            {
                buffer += "<null>";
            }
            else
            {
                foreach (LayoutContext layoutContext in _exteriorLogicalEdge)
                {
                    buffer += separator + "[" + layoutContext.ToString() + "]";
                    separator = ",";
                }
            }
            Debug.Print(buffer);
        }

        internal void InvalidateLogical()
        {
            CurrentRelationshipLevel = RelationshipStatus.NotBuiltYet;
            _logicalReplacements = null;
            _logicalNeighbors = null;
            _interiorLogicalEdge = null;
            _exteriorLogicalEdge = null;
            LayoutContext.DockingPanel.InvalidateSilos();
        }

        internal void InvalidatePhysical()
        {
            _exteriorPhysicalEdge = null;
            _interiorPhysicalEdge = null;
            InvalidateLogical();
        }

        public override string ToString()
        {
            return LayoutContext.ToString() + "/" + EdgesDockPosition.ToString();
        }
    }
}
