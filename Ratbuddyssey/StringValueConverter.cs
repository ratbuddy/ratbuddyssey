using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Ratbuddyssey.Converters
{
    public class StringValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty((string)value))
            {
                if((string)value=="U")
                {
                    return "Unlimited";
                }
                else
                {
                    return value;
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrEmpty((string)value))
            {
                if ((string)value == "Unlimited")
                {
                    return "U";
                }
                else
                {
                    return value;
                }
            }
            return "";
        }
    }
}
