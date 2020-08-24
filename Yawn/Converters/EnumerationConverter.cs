//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Yawn
{
    public class EnumerationConverter : IValueConverter
    {
        private static ResourceManager ResourceManager = new ResourceManager("Yawn.Properties.Resources", typeof(Properties.Resources).Assembly);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (targetType == typeof(string))
            {
                string resourceKey = value.ToString() + "Text";
                string result = ResourceManager.GetString(resourceKey);
                if (result == null)
                {
                    return "Unable to find resource key: " + resourceKey;
                }
                else
                {
                    return result;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
