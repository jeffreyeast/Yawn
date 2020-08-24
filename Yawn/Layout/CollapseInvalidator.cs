//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Yawn
{
    internal class CollapseInvalidator
    {
        internal interface ICollapseInvalidatorClient
        {
            Dictionary<System.Windows.Controls.Dock, DockableCollectionEdge> Edges { get; }
        }


        internal static bool InvalidateNeighbors(DockingPanel dockingPanel, LayoutContext root, System.Windows.Controls.Dock edge)
        {
            HashSet<LayoutContext> neighbors = new HashSet<LayoutContext>(root.Edges[edge].PhysicalNeighbors);
            HashSet<LayoutContext> preceedingNeighbors = new HashSet<LayoutContext>();
            HashSet<LayoutContext> rootsClockwiseNeighbors = new HashSet<LayoutContext>(root.Edges[LayoutContext.ClockwisePeers[edge]].PhysicalNeighbors);
            HashSet<LayoutContext> rootsCounterClockwiseNeighbors = new HashSet<LayoutContext>(root.Edges[LayoutContext.CounterClockwisePeers[edge]].PhysicalNeighbors);

            preceedingNeighbors.Add(root);

            for (LayoutContext clockwiseNeighbor = root.Edges[LayoutContext.ClockwisePeers[edge]].PhysicalNeighbors.FirstOrDefault(); 
                clockwiseNeighbor != null && clockwiseNeighbor.DockableCollection.IsCollapsed; 
                clockwiseNeighbor = clockwiseNeighbor.Edges[LayoutContext.ClockwisePeers[edge]].PhysicalNeighbors.FirstOrDefault())
            {
                HashSet<LayoutContext> neighborsPeers = new HashSet<LayoutContext>(clockwiseNeighbor.Edges[edge].PhysicalNeighbors);
                if (neighbors.SetEquals(neighborsPeers))
                {
                    preceedingNeighbors.Add(clockwiseNeighbor);
                    rootsClockwiseNeighbors = new HashSet<LayoutContext>(clockwiseNeighbor.Edges[LayoutContext.ClockwisePeers[edge]].PhysicalNeighbors);
                }
                else
                {
                    break;
                }
            }

            for (LayoutContext counterClockwiseNeighbor = root.Edges[LayoutContext.CounterClockwisePeers[edge]].PhysicalNeighbors.FirstOrDefault();
                counterClockwiseNeighbor != null && counterClockwiseNeighbor.DockableCollection.IsCollapsed;
                counterClockwiseNeighbor = counterClockwiseNeighbor.Edges[LayoutContext.CounterClockwisePeers[edge]].PhysicalNeighbors.FirstOrDefault())
            {
                HashSet<LayoutContext> neighborsPeers = new HashSet<LayoutContext>(counterClockwiseNeighbor.Edges[edge].PhysicalNeighbors);
                if (neighbors.SetEquals(neighborsPeers))
                {
                    preceedingNeighbors.Add(counterClockwiseNeighbor);
                    rootsCounterClockwiseNeighbors = new HashSet<LayoutContext>(counterClockwiseNeighbor.Edges[LayoutContext.CounterClockwisePeers[edge]].PhysicalNeighbors);
                }
                else
                {
                    break;
                }
            }

            int neighborCount = root.Edges[edge].PhysicalNeighbors.Count();

            while (neighborCount > 0)
            {
                //  First, check if the neighbors are members of the silo

                int collapsedNeighbors = 0;
                int noncollapsedNeighbors = 0;


                foreach (LayoutContext neighbor in neighbors)
                {
                    HashSet<LayoutContext> opposingPeers = new HashSet<LayoutContext>(neighbor.Edges[LayoutContext.OpposingNeighbors[edge]].PhysicalNeighbors);
                    if (!opposingPeers.IsSubsetOf(preceedingNeighbors))
                    { 
                            return false;
                    }

                    HashSet<LayoutContext> clockwiseNeighbors = new HashSet<LayoutContext>(neighbor.Edges[LayoutContext.ClockwisePeers[edge]].PhysicalNeighbors);
                    if (!clockwiseNeighbors.IsSubsetOf(rootsClockwiseNeighbors))
                    {
                        return false;
                    }

                    HashSet<LayoutContext> counterClockwiseNeighbors = new HashSet<LayoutContext>(neighbor.Edges[LayoutContext.CounterClockwisePeers[edge]].PhysicalNeighbors);
                    if (!counterClockwiseNeighbors.IsSubsetOf(rootsCounterClockwiseNeighbors))
                    {
                        return false;
                    }

                    //  The neighbor is a member of the silo

                    if (neighbor.DockableCollection.IsCollapsed)
                    {
                        collapsedNeighbors++;
                    }
                    else
                    {
                        noncollapsedNeighbors++;
                    }
                }

                //  If all the neighbors are collapsed, continue the scan with their neighbors

                if (collapsedNeighbors == neighborCount)
                {
                    preceedingNeighbors = neighbors;
                    neighbors = new HashSet<LayoutContext>();
                    foreach (LayoutContext neighbor in preceedingNeighbors)
                    {
                        neighbors.UnionWith(neighbor.Edges[edge].PhysicalNeighbors);
                    }
                    neighborCount = neighbors.Count();
                }
                else if (noncollapsedNeighbors == neighborCount)
                {
                    // Non of the peers are collapsed, so all can be invalidated

                    foreach (LayoutContext neighbor in neighbors)
                    {
                        neighbor.InvalidatePositioning(LayoutContext.DockPositionToPositionClass[edge]);
                    }
                    return true;
                }
                else
                {
                    //  Some neighbors are collapsed, some are not

                    return false;
                }
            }

            return false;
        }
    }
}
