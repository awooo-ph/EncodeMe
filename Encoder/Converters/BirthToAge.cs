using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace NORSU.EncodeMe.Converters
{
    class BirthToAge : ConverterBase
    {
        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (!(value is DateTime date)) return "";
            return (int) ((DateTime.Now - date).TotalDays / 365.25);
        }
    }
}
