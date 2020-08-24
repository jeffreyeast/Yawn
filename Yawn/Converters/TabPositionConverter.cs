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
    public class TabPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.GetType() == typeof (System.Windows.Controls.Dock) && targetType == typeof(TabPositions))
            {
                switch ((System.Windows.Controls.Dock)value)
                {
                    case System.Windows.Controls.Dock.Bottom:
                        return TabPositions.Bottom;
                    case System.Windows.Controls.Dock.Top:
                        return TabPositions.Top;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (value.GetType() == typeof(TabPositions) && targetType == typeof(System.Windows.Controls.Dock))
            {
                switch ((TabPositions)value)
                {
                    case TabPositions.Bottom:
                        return System.Windows.Controls.Dock.Bottom;
                    case TabPositions.Top:
                        return System.Windows.Controls.Dock.Top;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}
