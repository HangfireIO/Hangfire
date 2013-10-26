using System;
using System.ComponentModel;
using System.Globalization;

namespace HangFire.Tests
{
    public class CustomTypeConverter : TypeConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotSupportedException();
        }
    }
}