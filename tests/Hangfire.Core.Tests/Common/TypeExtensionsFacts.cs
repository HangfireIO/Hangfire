using System.Collections.Generic;
using Hangfire.Common;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
	public class TypeExtensionsFacts
	{
        [Fact]
        public void ToGenericTypeString_PrintsNonGenericNestedClassName_WithDot()
        {
            Assert.Equal(typeof(NonGenericClass).ToGenericTypeString(), "NonGenericClass");
            Assert.Equal(typeof(NonGenericClass.NestedNonGenericClass).ToGenericTypeString(), "NonGenericClass.NestedNonGenericClass");
            Assert.Equal(typeof(NonGenericClass.NestedNonGenericClass.DoubleNestedNonGenericClass).ToGenericTypeString(), "NonGenericClass.NestedNonGenericClass.DoubleNestedNonGenericClass");
        }	    

        [Fact]
        public void ToGenericTypeString_PrintsOpenGenericNestedClassName_WithGenericParameters()
	    {
            Assert.Equal(typeof(NonGenericClass.NestedGenericClass<,>).ToGenericTypeString(), "NonGenericClass.NestedGenericClass<T1,T2>");
	        Assert.Equal(typeof(GenericClass<>).ToGenericTypeString(), "GenericClass<T0>");
            Assert.Equal(typeof(GenericClass<>.NestedNonGenericClass).ToGenericTypeString(), "GenericClass<T0>.NestedNonGenericClass");
            Assert.Equal(typeof(GenericClass<>.NestedNonGenericClass.DoubleNestedGenericClass<,,>).ToGenericTypeString(), "GenericClass<T0>.NestedNonGenericClass.DoubleNestedGenericClass<T1,T2,T3>");
        }	    
        
        [Fact]
        public void ToGenericTypeString_PrintsClosedGenericNestedClassName_WithGivenTypes()
	    {
            Assert.Equal(typeof(NonGenericClass.NestedGenericClass<Assert, List<Assert>>).ToGenericTypeString(), "NonGenericClass.NestedGenericClass<Assert,List<Assert>>");
            Assert.Equal(typeof(GenericClass<Assert>).ToGenericTypeString(), "GenericClass<Assert>");
            Assert.Equal(typeof(GenericClass<List<Assert>>.NestedNonGenericClass).ToGenericTypeString(), "GenericClass<List<Assert>>.NestedNonGenericClass");
            Assert.Equal(typeof(GenericClass<List<GenericClass<List<Assert>>.NestedNonGenericClass.DoubleNestedGenericClass<Assert, List<Assert>, Stack<Assert>>>>.NestedNonGenericClass.DoubleNestedGenericClass<Assert, List<Assert>, Stack<Assert>>).ToGenericTypeString(), "GenericClass<List<GenericClass<List<Assert>>.NestedNonGenericClass.DoubleNestedGenericClass<Assert,List<Assert>,Stack<Assert>>>>.NestedNonGenericClass.DoubleNestedGenericClass<Assert,List<Assert>,Stack<Assert>>");        
	    }
	}

    public class GenericClass<T0>
    {
        public class NestedNonGenericClass
        {
            public class DoubleNestedGenericClass<T1, T2, T3>
            {

            }
        }
    }

    public class NonGenericClass
    {
        public class NestedNonGenericClass
        {
            public class DoubleNestedNonGenericClass
            {

            }
        }

        public class NestedGenericClass<T1, T2>
        {
        }
    }
}
