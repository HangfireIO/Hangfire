using System.ComponentModel;

namespace HangFire.Tests
{
    [TypeConverter(typeof(CustomTypeConverter))]
    public class CustomType {}
}