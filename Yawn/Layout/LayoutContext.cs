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
using System.Windows.Controls;
using System.Windows.Media;

namespace Yawn
{
    /// <summary>
    /// The LayoutContext is used during the DockingPanel's Arrange layout phase to hold positioning information.
    /// </summary>
    public class LayoutContext : CollapseInvalidator.ICollapseInvalidatorClient
    {
        internal enum PositionClasses
        {
            Internal = 1,
            Horizontal = 2 | Internal,
            Vertical = 4 | Internal,
            Collapse = 8 | Internal,
            Resize = 16 | Internal,
            EveryCollection = 32,

            All = Horizontal | Vertical,
        }


        public DockableCollection DockableCollection;

        public double? Left
        {
            get => _left;
            set
            {
                if (_left != value)
                {
                    _left = value;
                }
            }
        }
        double? _left;
        double? _leftSave;

        public double? Top
        {
            get => _top;
            set
            {
                if (_top != value)
                {
                    _top = value;
                }
            }
        }
        double? _top;
        double? _topSave;

        public Dimensions Size { get; private set; }

        public double? Bottom => Top.HasValue && Size.Height.HasInternalValue ? Top.Value + Size.Height.InternalValue : (double?)null;

        public double? Right => Left.HasValue && Size.Width.HasInternalValue ? Left.Value + Size.Width.InternalValue : (double?)null;

        public bool IsFullyPositioned => (Left.HasValue && Top.HasValue && Size.Height.HasInternalValue && Size.Height.InternalValue >= DockingPanel.MinimumChildSize.Height && Size.Width.HasInternalValue && Size.Width.InternalValue >= DockingPanel.MinimumChildSize.Width);

        public Rect Bounds => new Rect(Left.Value, Top.Value, Size.Width.InternalValue, Size.Height.InternalValue);

        internal DockingPanel DockingPanel { get; private set; }
        public Dictionary<System.Windows.Controls.Dock, DockableCollectionEdge> Edges { get; private set; }
        static readonly System.Windows.Controls.Dock[] CardinalEdges = Enum.GetValues(typeof(System.Windows.Controls.Dock)) as System.Windows.Controls.Dock[];
        internal int CycleNumber;

        internal static readonly Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock> ClockwisePeers = new Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock>()
        {
            {System.Windows.Controls.Dock.Bottom, System.Windows.Controls.Dock.Left },
            {System.Windows.Controls.Dock.Left, System.Windows.Controls.Dock.Top },
            {System.Windows.Controls.Dock.Right, System.Windows.Controls.Dock.Bottom },
            {System.Windows.Controls.Dock.Top, System.Windows.Controls.Dock.Right },
        };
        internal static readonly Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock> CounterClockwisePeers = new Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock>()
        {
            {System.Windows.Controls.Dock.Bottom, System.Windows.Controls.Dock.Right },
            {System.Windows.Controls.Dock.Left, System.Windows.Controls.Dock.Bottom },
            {System.Windows.Controls.Dock.Right, System.Windows.Controls.Dock.Top },
            {System.Windows.Controls.Dock.Top, System.Windows.Controls.Dock.Left },
        };
        internal static readonly Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock> OpposingNeighbors = new Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock>()
        {
            {System.Windows.Controls.Dock.Bottom, System.Windows.Controls.Dock.Top },
            {System.Windows.Controls.Dock.Left, System.Windows.Controls.Dock.Right },
            {System.Windows.Controls.Dock.Right, System.Windows.Controls.Dock.Left },
            {System.Windows.Controls.Dock.Top, System.Windows.Controls.Dock.Bottom },
        };
        internal static readonly Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock> MaximumOrthogonalEdge = new Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock>()
        {
            {System.Windows.Controls.Dock.Bottom, System.Windows.Controls.Dock.Right },
            {System.Windows.Controls.Dock.Left, System.Windows.Controls.Dock.Bottom },
            {System.Windows.Controls.Dock.Right, System.Windows.Controls.Dock.Bottom },
            {System.Windows.Controls.Dock.Top, System.Windows.Controls.Dock.Right },
        };
        internal static readonly Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock> MinimumOrthogonalEdge = new Dictionary<System.Windows.Controls.Dock, System.Windows.Controls.Dock>()
        {
            {System.Windows.Controls.Dock.Bottom, System.Windows.Controls.Dock.Left },
            {System.Windows.Controls.Dock.Left, System.Windows.Controls.Dock.Top },
            {System.Windows.Controls.Dock.Right, System.Windows.Controls.Dock.Top },
            {System.Windows.Controls.Dock.Top, System.Windows.Controls.Dock.Left },
        };
        internal static readonly Dictionary<System.Windows.Controls.Dock, PositionClasses> DockPositionToPositionClass = new Dictionary<System.Windows.Controls.Dock, PositionClasses>()
        {
            {System.Windows.Controls.Dock.Bottom, PositionClasses.Vertical | PositionClasses.Resize },
            {System.Windows.Controls.Dock.Left, PositionClasses.Horizontal | PositionClasses.Resize },
            {System.Windows.Controls.Dock.Right, PositionClasses.Horizontal | PositionClasses.Resize },
            {System.Windows.Controls.Dock.Top, PositionClasses.Vertical | PositionClasses.Resize },
        };

        internal LayoutContext(DockingPanel dockingPanel, DockableCollection dockableCollection)
        {
            DockingPanel = dockingPanel;
            DockableCollection = dockableCollection;
            Size = new Dimensions(DockableCollection);
            Edges = new Dictionary<System.Windows.Controls.Dock, DockableCollectionEdge>(4);
            foreach (System.Windows.Controls.Dock edge in Enum.GetValues(typeof(System.Windows.Controls.Dock)))
            {
                Edges.Add(edge, new DockableCollectionEdge(this, edge));
            }
        }

        internal static LayoutContext BottomLeftMostChild(LayoutContext child)
        {
            LayoutContext nextChild;
            while ((nextChild = child.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.FirstOrDefault() ?? child.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.FirstOrDefault()) != null)
            {
                child = nextChild;
            }
            return child;
        }

        /// <summary>
        /// Builds the silo in the indicated direction as a breadth-first list.
        /// </summary>
        /// <param name="seekDirection">Provides the direction of propagation (either Right or Bottom)</param>
        /// <returns></returns>
        private List<Silo> BuildSilos(System.Windows.Controls.Dock seekDirection)
        {
            //  There are two phases to building a silo. The first is determining the root(s) on the panel edge, the second
            //  is gathering the members in roughly breadth-first order (it's breadth-first for any rooted subtree, but
            //  until you reach a common descendant, the order of disjoint tree members is undefined).

            List<Silo> silos = new List<Silo>();
            List<LayoutContext> siloRoots = new List<LayoutContext>();

            foreach (LayoutContext root in Edges[OpposingNeighbors[seekDirection]].InteriorLogicalEdge)
            {
                Silo silo = new Silo();
                BuildSiloInternal(silo, new LayoutContext[] { root }, seekDirection);

                Silo lastSilo = silos.LastOrDefault();
                if (lastSilo == null || lastSilo.IsDisjoint(silo))
                {
                    silos.Add(silo);
                    siloRoots.Clear();
                    siloRoots.Add(root);
                }
                else
                {
                    siloRoots.Add(root);
                    lastSilo.Clear();
                    BuildSiloInternal(lastSilo, siloRoots, seekDirection);
                }
            }

            return silos;
        }

        private static void BuildSiloInternal(Silo silo, IEnumerable<LayoutContext> roots, System.Windows.Controls.Dock seekDirection)
        {
            //  Add the current edge to the silo

            foreach (LayoutContext peer in roots)
            {
                if (!silo.Contains(peer))
                {
                    silo.Add(peer);
                }
            }

            //  Now add the remaining edge levels

            foreach (LayoutContext peer in roots)
            {
                foreach (LayoutContext descendant in peer.Edges[seekDirection].ExteriorLogicalEdge)
                {
                    BuildSiloInternal(silo, descendant.Edges[OpposingNeighbors[seekDirection]].InteriorLogicalEdge,  seekDirection);
                }
            }
        }

        internal List<Silo> BuildHorizontalSilos()
        {
            return BuildSilos(System.Windows.Controls.Dock.Right);
        }

        internal List<Silo> BuildVerticalSilos()
        {
            return BuildSilos(System.Windows.Controls.Dock.Bottom);
        }

        internal void ClearSplitterContext(System.Windows.Controls.Dock dockPosition)
        {
            foreach (LayoutContext peer in GetInteriorEdge(dockPosition))
            {
                switch (dockPosition)
                {
                    case System.Windows.Controls.Dock.Bottom:
                        peer.Size.Height.ClearSplitter();
                        break;
                    case System.Windows.Controls.Dock.Right:
                        peer.Size.Width.ClearSplitter();
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            foreach (LayoutContext peer in GetExteriorEdge(dockPosition))
            {
                switch (dockPosition)
                {
                    case System.Windows.Controls.Dock.Bottom:
                        peer.Size.Height.ClearSplitter();
                        break;
                    case System.Windows.Controls.Dock.Right:
                        peer.Size.Width.ClearSplitter();
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private string DockableCollectionId => DockableCollection.Id.ToString();

        internal void Dump()
        {
            Debug.Print("    " + DockableCollectionId + " " + ToString() +
                ": DCS=(" + DockableCollection.Width.ToString("F0") + "," + DockableCollection.Height.ToString("F0") + ")" +
                ", DS=(" + DockableCollection.DesiredSize.Width.ToString("F0") + "," + DockableCollection.DesiredSize.Height.ToString("F0") + ")" +
                ", L=" + (Left.HasValue ? Left.Value.ToString("F0") : "<>") +
                ", T=" + (Top.HasValue ? Top.Value.ToString("F0") : "<>") +
                ", R=" + (Right.HasValue ? Right.Value.ToString("F0") : "<>") +
                ", B=" + (Bottom.HasValue ? Bottom.Value.ToString("F0") : "<>") +
                ", W=" + Size.Width.ToString() +
                ", H=" + Size.Height.ToString());
        }

        internal void DumpAll()
        {
            Dump();
            string links = "        Links:";
            links += FormatList(Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors, " Left");
            links += FormatList(Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors, ", Top");
            links += FormatList(Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors, ", Right");
            links += FormatList(Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors, ", Bottom");
            Debug.Print(links);
        }

        internal void DumpEdges(bool force = false)
        {
            string header = ToString() + ": State = " + DockableCollection.State.ToString() + Environment.NewLine + "    ";
            foreach (System.Windows.Controls.Dock edgesDockPosition in ClockwisePeers.Keys)
            {
                Edges[edgesDockPosition].DumpEdges(force, header);
                header = "    ";
            }
        }

        private string FormatList(IEnumerable<LayoutContext> list, string name)
        {
            string result = name + "{";
            string separator = "";

            foreach (LayoutContext layoutContext in list)
            {
                result += separator + layoutContext.DockableCollectionId;
                separator = ",";
            }

            return result + "}";
        }

        public double GetCoordinate(System.Windows.Controls.Dock edge)
        {
            switch (edge)
            {
                case System.Windows.Controls.Dock.Bottom:
                    return Bottom.Value;
                case System.Windows.Controls.Dock.Left:
                    return Left.Value;
                case System.Windows.Controls.Dock.Right:
                    return Right.Value;
                case System.Windows.Controls.Dock.Top:
                    return Top.Value;
                default:
                    throw new NotImplementedException();
            }
        }

        internal LinkedList<LayoutContext> GetExteriorEdge(System.Windows.Controls.Dock edgePosition)
        {
            return Edges[edgePosition].ExteriorLogicalEdge;
        }

        internal LinkedList<LayoutContext> GetInteriorEdge(System.Windows.Controls.Dock edgePosition)
        {
            return Edges[edgePosition].InteriorLogicalEdge;
        }

        public bool HasCoordinate(System.Windows.Controls.Dock edge)
        {
            switch (edge)
            {
                case System.Windows.Controls.Dock.Bottom:
                    return Bottom.HasValue;
                case System.Windows.Controls.Dock.Left:
                    return Left.HasValue;
                case System.Windows.Controls.Dock.Right:
                    return Right.HasValue;
                case System.Windows.Controls.Dock.Top:
                    return Top.HasValue;
                default:
                    throw new NotImplementedException();
            }
        }

        private void HorizontalSplitterMoved(IEnumerable<LayoutContext> topside, IEnumerable<LayoutContext> bottomside, double delta)
        {
            if (delta != 0)
            {
                foreach (LayoutContext layoutContext in topside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed &&
                        layoutContext.DockableCollection.ActualHeight + delta < DockingPanel.MinimumChildSize.Height && delta < 0)
                    {
                        return;
                    }
                }
                foreach (LayoutContext layoutContext in bottomside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed &&
                        layoutContext.DockableCollection.ActualHeight - delta < DockingPanel.MinimumChildSize.Height && delta > 0)
                    {
                        return;
                    }
                }

                foreach (LayoutContext layoutContext in topside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed)
                    {
                        layoutContext.Size.Height.SetSplitter(layoutContext.DockableCollection.ActualHeight + delta);
                    }
                }
                foreach (LayoutContext layoutContext in bottomside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed)
                    {
                        layoutContext.Size.Height.SetSplitter(layoutContext.DockableCollection.ActualHeight - delta);
                    }
                }
            }
        }

        internal void InsertAbove(LayoutContext newMemberContext)
        {
            InsertIntoPeerList(this, newMemberContext, System.Windows.Controls.Dock.Bottom);
            newMemberContext.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(this);
            Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(newMemberContext);
        }

        internal void InsertBelow(LayoutContext newMemberContext)
        {
            InsertIntoPeerList(this, newMemberContext, System.Windows.Controls.Dock.Top);
            newMemberContext.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(this);
            Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(newMemberContext);
        }

        internal void InsertBottom(LayoutContext anyExistingChild)
        {
            LayoutContext peer = BottomLeftMostChild(anyExistingChild);
            while (peer != null)
            {
                Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(peer);
                peer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(this);
                peer = peer.NextBottomMostRightPeer;
            }
            DockingPanel.InvalidatePhysical();
        }

        internal void InsertIntoPeerList(LayoutContext neighbor, LayoutContext newMember, System.Windows.Controls.Dock relationshipOldVisAVieNew)
        {
            LayoutContext peer;

            switch (relationshipOldVisAVieNew)
            {
                case System.Windows.Controls.Dock.Bottom:       // Old is below New
                    while ((peer = neighbor.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.FirstOrDefault()) != null)
                    {
                        var neighborLLN = peer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Find(neighbor);
                        peer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        peer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Remove(neighborLLN);
                        neighbor.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Remove(peer);
                        newMember.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(peer);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(context);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(context);
                    }
                    break;
                case System.Windows.Controls.Dock.Left:      // Old is to the left of New
                    while ((peer = neighbor.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.FirstOrDefault()) != null)
                    {
                        var neighborLLN = peer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Find(neighbor);
                        peer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        peer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Remove(neighborLLN);
                        neighbor.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Remove(peer);
                        newMember.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(peer);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddAfter(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(context);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddAfter(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(context);
                    }
                    break;
                case System.Windows.Controls.Dock.Right:    //  Old is to the right of new
                    while ((peer = neighbor.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.FirstOrDefault()) != null)
                    {
                        var neighborLLN = peer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Find(neighbor);
                        peer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        peer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Remove(neighborLLN);
                        neighbor.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Remove(peer);
                        newMember.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(peer);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(context);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(context);
                    }
                    break;
                case System.Windows.Controls.Dock.Top:      // Old is above New
                    while ((peer = neighbor.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.FirstOrDefault()) != null)
                    {
                        var neighborLLN = peer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Find(neighbor);
                        peer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddBefore(neighborLLN, newMember);
                        peer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Remove(neighborLLN);
                        neighbor.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Remove(peer);
                        newMember.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(peer);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddAfter(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(context);
                    }
                    foreach (LayoutContext context in neighbor.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors)
                    {
                        var neighborLLN = context.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Find(neighbor);
                        context.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddAfter(neighborLLN, newMember);
                        newMember.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(context);
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            DockingPanel.InvalidatePhysical();
        }

        internal void InsertLeft(LayoutContext anyExistingChild)
        {
            LayoutContext peer = TopLeftMostChild(anyExistingChild);
            while (peer != null)
            {
                Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(peer);
                peer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(this);
                peer = peer.NextLeftMostBottomPeer;
            }
            DockingPanel.InvalidatePhysical();
        }

        internal void InsertRight(LayoutContext anyExistingChild)
        {
            LayoutContext peer = TopRightMostChild(anyExistingChild);
            while (peer != null)
            {
                Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(peer);
                peer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(this);
                peer = peer.NextRightMostBottomPeer;
            }
            DockingPanel.InvalidatePhysical();
        }

        internal void InsertTop(LayoutContext anyExistingChild)
        {
            LayoutContext peer = TopLeftMostChild(anyExistingChild);
            while (peer != null)
            {
                Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddLast(peer);
                peer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddLast(this);
                peer = peer.NextTopMostRightPeer;
            }
            DockingPanel.InvalidatePhysical();
        }

        internal void InsertToLeftOf(LayoutContext newMemberContext)
        {
            InsertIntoPeerList(this, newMemberContext, System.Windows.Controls.Dock.Right);
            newMemberContext.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(this);
            Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(newMemberContext);
        }

        internal void InsertToRightOf(LayoutContext newMemberContext)
        {
            InsertIntoPeerList(this, newMemberContext, System.Windows.Controls.Dock.Left);
            newMemberContext.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddLast(this);
            Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddLast(newMemberContext);
        }

        internal void InvalidateLogical()
        {
            foreach (System.Windows.Controls.Dock dockPosition in Enum.GetValues(typeof(System.Windows.Controls.Dock)))
            {
                Edges[dockPosition].InvalidateLogical();
            }
        }

        internal void InvalidatePhysical()
        {
            foreach (System.Windows.Controls.Dock dockPosition in Enum.GetValues(typeof(System.Windows.Controls.Dock)))
            {
                Edges[dockPosition].InvalidatePhysical();
            }
        }

        internal void InvalidatePositioning(PositionClasses positionClass)
        {
#if POSITIONDUMP
            string result = ToString() + " InvalidatePositioning: ";
            result += ((positionClass & PositionClasses.Horizontal) == PositionClasses.Horizontal) ? "Horizontal " : "";
            result += ((positionClass & PositionClasses.Vertical) == PositionClasses.Vertical) ? "Vertical " : "";
            result += ((positionClass & PositionClasses.Internal) == PositionClasses.Internal) ? "Internal " : "";
            result += ((positionClass & PositionClasses.Collapse) == PositionClasses.Collapse) ? "Collapse " : "";
            result += ((positionClass & PositionClasses.Resize) == PositionClasses.Resize) ? "Resize " : "";
            Debug.Print(result);
#endif

            if ((positionClass & PositionClasses.Horizontal) == PositionClasses.Horizontal &&
                 (positionClass & PositionClasses.Resize) == PositionClasses.Resize && DockableCollection.HorizontalContentAlignment == HorizontalAlignment.Stretch)
            {
                Size.Width.Reset();
            }
            if ((positionClass & PositionClasses.Vertical) == PositionClasses.Vertical &&
                 (positionClass & PositionClasses.Resize) == PositionClasses.Resize && DockableCollection.VerticalContentAlignment == VerticalAlignment.Stretch)
            {
                Size.Height.Reset();
            }
            if ((positionClass & PositionClasses.Internal) == PositionClasses.Internal)
            {
                DockableCollection.InvalidateMeasure();
            }

            if ((positionClass & PositionClasses.Collapse) == PositionClasses.Collapse)
            {
                DockingPanel.InvalidateLogical();
                CollapseInvalidator.InvalidateNeighbors(DockingPanel, this, System.Windows.Controls.Dock.Right);
                CollapseInvalidator.InvalidateNeighbors(DockingPanel, this, System.Windows.Controls.Dock.Left);
                CollapseInvalidator.InvalidateNeighbors(DockingPanel, this, System.Windows.Controls.Dock.Top);
                CollapseInvalidator.InvalidateNeighbors(DockingPanel, this, System.Windows.Controls.Dock.Bottom);
            }
        }

        /// <summary>
        /// Tests if the layout context (this) logically preceeds (is either above or to the left of) the referenceContext
        /// </summary>
        /// <param name="referenceContext">identifies the root of the subtree to be searched</param>
        /// <param name="dockEdgesPosition">identifies the edge being tested</param>
        /// <returns></returns>
        internal bool IsLogicalPreceeding(LayoutContext referenceContext, System.Windows.Controls.Dock dockEdgesPosition)
        {
            if (referenceContext.Edges[MinimumOrthogonalEdge[dockEdgesPosition]].LogicalNeighbors.Contains(this))
            {
                return true;
            }

            foreach (LayoutContext neighbor in referenceContext.Edges[MinimumOrthogonalEdge[dockEdgesPosition]].LogicalNeighbors)
            {
                if (IsLogicalPreceeding(neighbor, dockEdgesPosition))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsOnDockEdge(System.Windows.Controls.Dock dockPosition)
        {
            return GetExteriorEdge(dockPosition).FirstOrDefault() == null;
        }

        internal bool IsPhysicallyReachable(LayoutContext targetLayoutContext, System.Windows.Controls.Dock seekDirection)
        {
            foreach (var neighbor in Edges[seekDirection].ExteriorPhysicalEdge)
            {
                if (neighbor == targetLayoutContext)
                {
                    return true;
                }
                if (neighbor.IsPhysicallyReachable(targetLayoutContext, seekDirection))
                {
                    return true;
                }
            }
            return false;
        }

        private LayoutContext NextBottomMostRightPeer
        {
            get
            {
                foreach (LayoutContext candidatePeer in Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors)
                {
                    if (candidatePeer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.FirstOrDefault() == null)
                    {
                        return candidatePeer;
                    }
                }
                return null;
            }
        }

        internal LayoutContext NextLeftMostBottomPeer
        {
            get
            {
                foreach (LayoutContext candidatePeer in Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors)
                {
                    if (candidatePeer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.FirstOrDefault() == null)
                    {
                        return candidatePeer;
                    }
                }
                return null;
            }
        }

        private LayoutContext NextRightMostBottomPeer
        {
            get
            {
                foreach (LayoutContext candidatePeer in Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors)
                {
                    if (candidatePeer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.FirstOrDefault() == null)
                    {
                        return candidatePeer;
                    }
                }
                return null;
            }
        }

        internal LayoutContext NextTopMostRightPeer
        {
            get
            {
                foreach (LayoutContext candidatePeer in Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors)
                {
                    if (candidatePeer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.FirstOrDefault() == null)
                    {
                        return candidatePeer;
                    }
                }
                return null;
            }
        }

        enum PeerCopyChoices
        {
            None,
            CopyHorizontal,
            CopyVertical,
        }

        private bool IsHorizontalIsomorphWithLeftPeer =>
            Edges[System.Windows.Controls.Dock.Left].ExteriorPhysicalEdge.FirstOrDefault()?.Edges[System.Windows.Controls.Dock.Right].ExteriorPhysicalEdge.FirstOrDefault() == this &&
            Edges[System.Windows.Controls.Dock.Left].ExteriorPhysicalEdge.LastOrDefault()?.Edges[System.Windows.Controls.Dock.Right].ExteriorPhysicalEdge.LastOrDefault() == this;

        private bool IsHorizontalIsomorphWithRightPeer =>
            Edges[System.Windows.Controls.Dock.Right].ExteriorPhysicalEdge.FirstOrDefault()?.Edges[System.Windows.Controls.Dock.Left].ExteriorPhysicalEdge.FirstOrDefault() == this &&
            Edges[System.Windows.Controls.Dock.Right].ExteriorPhysicalEdge.LastOrDefault()?.Edges[System.Windows.Controls.Dock.Left].ExteriorPhysicalEdge.LastOrDefault() == this;

        private bool IsVerticalIsomorphWithBottomPeer =>
            Edges[System.Windows.Controls.Dock.Bottom].ExteriorPhysicalEdge.FirstOrDefault()?.Edges[System.Windows.Controls.Dock.Top].ExteriorPhysicalEdge.FirstOrDefault() == this &&
            Edges[System.Windows.Controls.Dock.Bottom].ExteriorPhysicalEdge.LastOrDefault()?.Edges[System.Windows.Controls.Dock.Top].ExteriorPhysicalEdge.LastOrDefault() == this;

        private bool IsVerticalIsomorphWithTopPeer =>
            Edges[System.Windows.Controls.Dock.Top].ExteriorPhysicalEdge.FirstOrDefault()?.Edges[System.Windows.Controls.Dock.Bottom].ExteriorPhysicalEdge.FirstOrDefault() == this &&
            Edges[System.Windows.Controls.Dock.Top].ExteriorPhysicalEdge.LastOrDefault()?.Edges[System.Windows.Controls.Dock.Bottom].ExteriorPhysicalEdge.LastOrDefault() == this;

        internal void Remove()
        {
            // Somebody has to expand into the space freed up by this collection

            InvalidatePositioning(PositionClasses.Collapse);

            // Copy our peer groups to our neighbors, where needed

            PeerCopyChoices copyChoice = (IsHorizontalIsomorphWithLeftPeer || IsHorizontalIsomorphWithRightPeer) ? PeerCopyChoices.CopyHorizontal :
                                         (IsVerticalIsomorphWithBottomPeer || IsVerticalIsomorphWithTopPeer) ? PeerCopyChoices.CopyVertical : PeerCopyChoices.None;

            switch (copyChoice)
            {
                case PeerCopyChoices.CopyHorizontal:
                    foreach (LayoutContext rightPeer in Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors)
                    {
                        var leftLLN = rightPeer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Find(this);
                        foreach (LayoutContext leftPeer in Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors)
                        {
                            var rightLLN = leftPeer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Find(this);
                            rightPeer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.AddBefore(leftLLN, leftPeer);
                            leftPeer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.AddBefore(rightLLN, rightPeer);
                        }
                    }
                    break;
                case PeerCopyChoices.CopyVertical:
                    foreach (LayoutContext bottomPeer in Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors)
                    {
                        var topLLN = bottomPeer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Find(this);
                        foreach (LayoutContext topPeer in Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors)
                        {
                            var bottomLLN = topPeer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Find(this);
                            bottomPeer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.AddBefore(topLLN, topPeer);
                            topPeer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.AddBefore(bottomLLN, bottomPeer);
                        }
                    }
                    break;
                case PeerCopyChoices.None:
                    break;
                default:
                    throw new NotImplementedException();
            }


            // Now, remove ourself from the peer graph

            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors)
            {
                peer.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Remove(this);
            }
            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors)
            {
                peer.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Remove(this);
            }
            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors)
            {
                peer.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Remove(this);
            }
            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors)
            {
                peer.Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Remove(this);
            }

            DockingPanel.InvalidatePhysical();


            //  And tidy up our peer groups

            Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Clear();
            Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Clear();
            Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Clear();
            Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Clear();
        }

        internal void PreArrange()
        {
            _leftSave = _left;
            _left = null;
            _topSave = _top;
            _top = null;
            Size.PreArrange();
        }

        internal void Restore()
        {
            _left = _leftSave;
            _top = _topSave;
            Size.Restore();
        }

        internal void SplitterMoved(System.Windows.Controls.Dock dockPosition, double delta)
        {
            switch (dockPosition)
            {
                case System.Windows.Controls.Dock.Bottom:
                    HorizontalSplitterMoved(GetInteriorEdge(dockPosition), GetExteriorEdge(dockPosition), delta);
                    break;
                case System.Windows.Controls.Dock.Left:
                    VerticalSplitterMoved(GetExteriorEdge(dockPosition), GetInteriorEdge(dockPosition), delta);
                    break;
                case System.Windows.Controls.Dock.Right:
                    VerticalSplitterMoved(GetInteriorEdge(dockPosition), GetExteriorEdge(dockPosition), delta);
                    break;
                case System.Windows.Controls.Dock.Top:
                    HorizontalSplitterMoved(GetExteriorEdge(dockPosition), GetInteriorEdge(dockPosition), delta);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return DockableCollection.ToString();
        }

        internal static LayoutContext TopLeftMostChild(LayoutContext child)
        {
            LayoutContext nextChild;
            while ((nextChild = child.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.FirstOrDefault() ?? child.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.FirstOrDefault()) != null)
            {
                child = nextChild;
            }
            return child;
        }

        internal static LayoutContext TopLeftMostVisibleChild(LayoutContext child)
        {
            LayoutContext nextChild;
            while ((nextChild = child.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.FirstOrDefault() ?? child.Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.FirstOrDefault()) != null)
            {
                child = nextChild;
            }
            return child.DockableCollection.IsCollapsed ? child.Edges[System.Windows.Controls.Dock.Right].LogicalReplacements?.FirstOrDefault() ?? child.Edges[System.Windows.Controls.Dock.Bottom].LogicalReplacements.FirstOrDefault() : child;
        }

        internal static LayoutContext TopRightMostChild(LayoutContext child)
        {
            LayoutContext nextChild;
            while ((nextChild = child.Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.FirstOrDefault() ?? child.Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.FirstOrDefault()) != null)
            {
                child = nextChild;
            }
            return child;
        }

        internal void ComputeLogicalReplacements(out LinkedList<LayoutContext> contactingEdgePromotionCandidates, out LinkedList<LayoutContext> opposingEdgePromotionCandidates, System.Windows.Controls.Dock contactingEdgesDockPosition)
        {
            //  Compute who could expand into the vacated space

            ComputePromotionCandidates(out contactingEdgePromotionCandidates, Edges[contactingEdgesDockPosition].ExteriorPhysicalEdge, LayoutContext.OpposingNeighbors[contactingEdgesDockPosition]);
            ComputePromotionCandidates(out opposingEdgePromotionCandidates, Edges[LayoutContext.OpposingNeighbors[contactingEdgesDockPosition]].ExteriorPhysicalEdge, contactingEdgesDockPosition);
        }

        private void ComputePromotionCandidates(out LinkedList<LayoutContext> promotionCandidates, LinkedList<LayoutContext> limitingEdge, System.Windows.Controls.Dock seekDirection)
        {
            LayoutContext visibleDescendant = GetVisibleAncestor(seekDirection); 
            if (visibleDescendant != null)
            {
                if (!AreAnyDescendantsVisible(visibleDescendant.Edges[OpposingNeighbors[seekDirection]].ExteriorPhysicalEdge, limitingEdge, OpposingNeighbors[seekDirection]))
                {
                    promotionCandidates = new LinkedList<LayoutContext>();
                    foreach (LayoutContext candidate in visibleDescendant.Edges[OpposingNeighbors[seekDirection]].InteriorPhysicalEdge)
                    {
                        if (!candidate.DockableCollection.IsCollapsed)
                        {
                            promotionCandidates.AddLast(candidate);
                        }
                    }
                    return;
                }
            }

            promotionCandidates = null;
            return;
        }

        private bool AreAnyDescendantsVisible(LinkedList<LayoutContext> edge, LinkedList<LayoutContext> limitingEdge, System.Windows.Controls.Dock edgesDockPosition)
        {
            bool hitLimitingEdge = false;

            foreach (LayoutContext collection in edge)
            {
                if (limitingEdge.Contains(collection))
                {
                    hitLimitingEdge = true;
                }
                else if (!collection.DockableCollection.IsCollapsed)
                {
                    return true;
                }
            }

            if (!hitLimitingEdge)
            {
                foreach (LayoutContext collection in edge)
                {
                    bool foundVisibleDescendant = AreAnyDescendantsVisible(collection.Edges[edgesDockPosition].PhysicalNeighbors, limitingEdge, edgesDockPosition);
                    if (foundVisibleDescendant)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal LayoutContext GetVisibleAncestor(System.Windows.Controls.Dock seekDirection)
        {
            foreach (LayoutContext candidate in Edges[seekDirection].PhysicalNeighbors)
            {
                if (!candidate.DockableCollection.IsCollapsed)
                {
                    return candidate;
                }
            }

            foreach (LayoutContext candidate in Edges[seekDirection].PhysicalNeighbors)
            {
                LayoutContext visibleAncestor = candidate.GetVisibleAncestor(seekDirection);
                if (visibleAncestor != null)
                {
                    return visibleAncestor;
                }
            }

            return null;
        }

        internal void Validate(Size layoutSize)
        {
            if (!(_left.HasValue && _top.HasValue && Size.Width.HasInternalValue && Size.Height.HasInternalValue)) throw new InvalidOperationException(ToString() + " not fully configured");
            if (!(_left.Value == 0 || Edges[System.Windows.Controls.Dock.Left].PhysicalNeighbors.Count > 0)) throw new InvalidOperationException(ToString() + " gap to left");
            if (!(_top.Value == 0 || Edges[System.Windows.Controls.Dock.Top].PhysicalNeighbors.Count > 0)) throw new InvalidOperationException(ToString() + " gap above");
            if (!(Math.Abs(Right.Value - layoutSize.Width) < 0.1 || Edges[System.Windows.Controls.Dock.Right].PhysicalNeighbors.Count > 0)) throw new InvalidOperationException(ToString() + " gap to right");
            if (!(Math.Abs(Bottom.Value - layoutSize.Height) < 0.1 || Edges[System.Windows.Controls.Dock.Bottom].PhysicalNeighbors.Count > 0)) throw new InvalidOperationException(ToString() + " gap below");
            
            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Bottom].LogicalNeighbors)
            {
                if (!(Bottom.Value == peer.Top.Value)) throw new InvalidOperationException(ToString() + " gap below");
            }
            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Left].LogicalNeighbors)
            {
                if (!(Left.Value == peer.Right.Value)) throw new InvalidOperationException(ToString() + " gap to left");
            }
            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Right].LogicalNeighbors)
            {
                if (!(Right.Value == peer.Left.Value)) throw new InvalidOperationException(ToString() + " gap to right");
            }
            foreach (LayoutContext peer in Edges[System.Windows.Controls.Dock.Top].LogicalNeighbors)
            {
                if (!(Top.Value == peer.Bottom.Value)) throw new InvalidOperationException(ToString() + " gap above");
            }
        }

        private void VerticalSplitterMoved(IEnumerable<LayoutContext> leftside, IEnumerable<LayoutContext> rightside, double delta)
        {
            if (delta != 0)
            {
                foreach (LayoutContext layoutContext in leftside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed &&
                        layoutContext.DockableCollection.ActualWidth + delta < DockingPanel.MinimumChildSize.Width && delta < 0)
                    {
                        return;
                    }
                }
                foreach (LayoutContext layoutContext in rightside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed &&
                        layoutContext.DockableCollection.ActualWidth - delta < DockingPanel.MinimumChildSize.Width && delta > 0)
                    {
                        return;
                    }
                }

                foreach (LayoutContext layoutContext in leftside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed)
                    {
                        layoutContext.Size.Width.SetSplitter(layoutContext.DockableCollection.ActualWidth + delta);
                    }
                }
                foreach (LayoutContext layoutContext in rightside)
                {
                    if (!layoutContext.DockableCollection.IsCollapsed)
                    {
                        layoutContext.Size.Width.SetSplitter(layoutContext.DockableCollection.ActualWidth - delta);
                    }
                }
            }
        }
    }
}
