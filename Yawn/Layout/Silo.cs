//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yawn
{
    /// <summary>
    /// The Silo class is used to represent a set of elements who share a logical column or row in a Dock
    /// </summary>
    internal class Silo : IEnumerable<LayoutContext>
    {
        //  We maintain two lists of members: one hashed and one linear. The hashed set provides fast membership and set
        //  operations, while the ordered set is used for enumeration. The enumeration order is FIFO.

        HashSet<LayoutContext> HashedMembers { get; set; }
        List<LayoutContext> OrderedMembers { get; set; }
        internal static int LastCyleNumber = 1;



        internal Silo()
        {
            HashedMembers = new HashSet<LayoutContext>();
            OrderedMembers = new List<LayoutContext>();
        }

        internal void Add(LayoutContext layoutContext)
        {
            HashedMembers.Add(layoutContext);
            OrderedMembers.Add(layoutContext);
        }

        internal void Clear()
        {
            HashedMembers.Clear();
            OrderedMembers.Clear();
        }

        internal bool Contains(LayoutContext layoutContext)
        {
            return HashedMembers.Contains(layoutContext);
        }

        internal int Count => OrderedMembers.Count;

        internal void Dump()
        {
            string result = "    ";
            string separator = "";
            foreach(LayoutContext member in this)
            {
                result += separator + member.ToString();
                separator = ",";
            }
            Debug.Print(result);
        }

        internal bool IsDisjoint(Silo peerSilo)
        {
            return IsDisjoint(++LastCyleNumber, peerSilo);
        }

        private bool IsDisjoint(int cycleNumber, Silo peerSilo)
        {
            //  The idea here is to mark each member of the silo, and then check if the other
            //  silo shares a member (because it's marked).

            foreach (LayoutContext layoutContext in OrderedMembers)
            {
                layoutContext.CycleNumber = cycleNumber;
            }

            foreach (LayoutContext layoutContext in peerSilo.OrderedMembers)
            {
                if (layoutContext.CycleNumber == cycleNumber)
                {
                    //  The silos share at least one member

                    return false;
                }
            }

            return true;
        }


#if true
        // Merge is currently invalid, as the implementation doesn't maintain the order of the elements within the
        // silo, which layout depends on.

        internal void MergeWith(Silo peerSilo)
        {
            throw new NotImplementedException();
        }
#else
        internal void MergeWith(Silo peerSilo)
        {
            Merge(++LastCyleNumber, peerSilo);
        }

        private void Merge(int cycleNumber, Silo peerSilo)
        {
            foreach (LayoutContext layoutContext in OrderedMembers)
            {
                layoutContext.CycleNumber = cycleNumber;
            }

            foreach (LayoutContext layoutContext in peerSilo.OrderedMembers)
            {
                if (layoutContext.CycleNumber != cycleNumber)
                {
                    Add(layoutContext);
                }
            }
        }
#endif

        internal void Remove(LayoutContext layoutContext)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<LayoutContext> GetEnumerator()
        {
            return OrderedMembers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return OrderedMembers.GetEnumerator();
        }
    }
}
