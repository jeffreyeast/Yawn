using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Yawn
{
    /// <summary>
    /// The Dimension class is used to represent the Width and Height dimensions of a DockableCollection in the Layout object
    /// </summary>
    public class Dimension
    {
        private interface IDimensionAccessor
        {
            double Value { get; set; }
        }

        private class HeightAccessor : IDimensionAccessor
        {
            public double Value
            {
                get => DockableCollection.Height;
                set
                {
                    if (DockableCollection.Height != value && !(double.IsNaN(value) && double.IsNaN(DockableCollection.Height)))
                    {
                        DockableCollection.Height = value;
                    }
                }
            }

            DockableCollection DockableCollection;


            public HeightAccessor(DockableCollection dockableCollection)
            {
                DockableCollection = dockableCollection;
            }
        }

        private class WidthAccessor : IDimensionAccessor
        {
            public double Value
            {
                get => DockableCollection.Width;
                set
                {
                    if (DockableCollection.Width != value && !(double.IsNaN(value) && double.IsNaN(DockableCollection.Width)))
                    {
                        DockableCollection.Width = value;
                    }
                }
            }

            DockableCollection DockableCollection;


            public WidthAccessor(DockableCollection dockableCollection)
            {
                DockableCollection = dockableCollection;
            }
        }

        public enum DimensionTypes
        {
            Height,
            Width,
        }

        public enum States
        {
            UserValueIsSet = 1,
            InternalValueIsSet = 2,

            SplitterActive = 4,
        }

        public States State { get; private set; }

        public bool HasInternalValue => (State & States.InternalValueIsSet) != 0;
        public bool HasUserValue => (State & States.UserValueIsSet) != 0 && !double.IsNaN(UserValue);
        public bool IsSplitterActive => (State & States.SplitterActive) != 0;
        public double InternalValue { get; private set; }
        private double SavedInternalValue;
        public double UserValue { get; private set; }
        private IDimensionAccessor WpfAccessor;



        internal Dimension(DockableCollection dockableCollection, DimensionTypes dimensionType)
        {
            State = 0;
            switch (dimensionType)
            {
                case DimensionTypes.Height:
                    WpfAccessor = new HeightAccessor(dockableCollection);
                    break;
                case DimensionTypes.Width:
                    WpfAccessor = new WidthAccessor(dockableCollection);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void ClearSplitter()
        {
            State &= ~States.SplitterActive;
            ClearWpfValue();
        }

        public void ClearWpfValue()
        {
            WpfAccessor.Value = double.NaN;
        }

        public void ClearInternalValue()
        {
            if (!IsSplitterActive)
            {
                State &= ~States.InternalValueIsSet;
            }
        }

        public void PostArrange()
        {
            SetWpfValue();
        }

        public void PostMeasure()
        {
        }

        public void PreArrange()
        {
            SavedInternalValue = InternalValue;
            ClearInternalValue();
            ClearWpfValue();
        }

        public void PreMeasure()
        {
            if (IsSplitterActive)
            {
                SetWpfValue(InternalValue);
            }
            else if (HasUserValue)
            {
                SetWpfValue(UserValue);
            }
            else
            {
                ClearWpfValue();
            }
        }

        public void Reset()
        {
            ClearInternalValue();
            ClearSplitter();
        }

        public void Restore()
        {
            InternalValue = SavedInternalValue;
        }

        public void SetInternalValue(double? value)
        {
            if (value.HasValue)
            {
                InternalValue = value.Value;
                State |= States.InternalValueIsSet;
            }
            else
            {
                State &= ~States.InternalValueIsSet;
            }
        }

        public void SetSplitter(double value)
        {
            SetInternalValue(value);
            SetWpfValue();
            State |= States.SplitterActive | States.InternalValueIsSet;
        }

        public void SetUserValue(double value)
        {
            UserValue = value;
            if (double.IsNaN(value))
            {
                State &= ~States.UserValueIsSet;
            }
            else
            {
                State |= States.UserValueIsSet;
            }
        }

        public void SetWpfValue()
        {
            SetWpfValue(InternalValue);
        }

        private void SetWpfValue(double value)
        {
            WpfAccessor.Value = value;
        }

        public override string ToString()
        {
            return ((State & States.InternalValueIsSet) == 0) ? "<>" : InternalValue.ToString("F0");
        }
    }



    /// <summary>
    /// The Dimensions class represents a width/height pair
    /// </summary>
    public class Dimensions
    {
        public Dimension Height { get; private set; }
        public Dimension Width { get; private set; }


        internal Dimensions(DockableCollection dockableCollection)
        {
            Height = new Dimension(dockableCollection, Dimension.DimensionTypes.Height);
            Width = new Dimension(dockableCollection, Dimension.DimensionTypes.Width);
            Height.SetUserValue(dockableCollection.Height);
            Width.SetUserValue(dockableCollection.Width);
        }

        public void PostArrange()
        {
            Height.PostArrange();
            Width.PostArrange();
        }

        public void PostMeasure()
        {
            Height.PostMeasure();
            Width.PostMeasure();
        }

        public void PreArrange()
        {
            Height.PreArrange();
            Width.PreArrange();
        }

        public void PreMeasure()
        {
            Height.PreMeasure();
            Width.PreMeasure();
        }

        public void Restore()
        {
            Height.Restore();
            Width.Restore();
        }

        public static implicit operator Size(Dimensions d) => new Size(d.Width.InternalValue, d.Height.InternalValue);

        public override string ToString()
        {
            return "(" + Width.ToString() + "," + Height.ToString() + ")";
        }
    }
}
