//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Yawn
{
    public class CollectionStatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (targetType == typeof(DockableCollection.States))
                {
                    if (value is bool booleanValue)
                    {
                        return booleanValue ? DockableCollection.States.Pinned : DockableCollection.States.UnPinnedAndVisible;
                    }
                }
                else if (targetType == typeof(bool) || targetType == typeof(bool?))
                {
                    if (value is DockableCollection.States state)
                    {
                        switch (state)
                        {
                            case DockableCollection.States.Constructed:
                            case DockableCollection.States.Loaded:
                            case DockableCollection.States.Pinned:
                            case DockableCollection.States.PinnedAndEmpty:
                                return true;
                            case DockableCollection.States.UnPinnedAndCollapsed:
                            case DockableCollection.States.UnPinnedAndEmpty:
                            case DockableCollection.States.UnPinnedAndVisible:
                                return false;
                            default:
                                break;
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
