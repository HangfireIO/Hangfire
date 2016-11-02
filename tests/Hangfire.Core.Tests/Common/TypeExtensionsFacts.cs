using System;
using System.Collections.Generic;
using Hangfire.Common;
using Xunit;

// ReSharper disable InvokeAsExtensionMethod
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable UnusedTypeParameter

public class ClassWithoutNamespace
{
}

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

	    [Fact]
	    public void ToGenericTypeString_CorrectlyHandlesTypesWithoutNamespace()
	    {
	        Assert.Equal("ClassWithoutNamespace", typeof(ClassWithoutNamespace).ToGenericTypeString());
	    }

        [Fact]
        public void GetNonOpenMatchingMethod_ThrowsAnException_WhenTypeIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => TypeExtensions.GetNonOpenMatchingMethod(null, "Method", new Type[0]));

            Assert.Equal("type", exception.ParamName);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ThrowsAnException_WhenNameIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), null, new Type[0]));

            Assert.Equal("name", exception.ParamName);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethod()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method", new Type[0]);

            Assert.Equal("Method", method.Name);
            Assert.Equal(typeof(NonGenericClass), method.DeclaringType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethodWithNoParameter_WhenParameterTypesIsNull()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method", null);

            Assert.Equal("Method", method.Name);
            Assert.Equal(typeof(NonGenericClass), method.DeclaringType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethodWithOneParameter()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method",
                new Type[] { typeof(int) });

            Assert.Equal("Method", method.Name);
            Assert.Equal(typeof(NonGenericClass), method.DeclaringType);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethodWithManyParameters()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method",
                new Type[] { typeof(int), typeof(int) });

            Assert.Equal("Method", method.Name);
            Assert.Equal(typeof(NonGenericClass), method.DeclaringType);
            Assert.Equal(2, method.GetParameters().Length);
            Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
            Assert.Equal(typeof(int), method.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethod_WhenTypeIsInterface()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(IParent), "Method", new Type[0]);
            Assert.Equal("Method", method.Name);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodDefinedInBaseInterface()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(IChild), "Method", new Type[0]);
            Assert.Equal("Method", method.Name);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectGenericMethod()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new Type[] { typeof(int), typeof(string), typeof(object) });

            Assert.Equal("GenericMethod", method.Name);
            Assert.Equal(typeof(NonGenericClass), method.DeclaringType);
            Assert.Equal(true, method.IsGenericMethod);
            Assert.Equal(false, method.ContainsGenericParameters);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsNull_WhenMethodCouldNotBeFound()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "NonExistingMethod", new Type[0]);

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsNull_WhenOveroladedMethodCouldNotBeFound()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method",
                new Type[] { typeof(object), typeof(int) });

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodNameIsCaseSensitive()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "method", new Type[0]);

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodParameterTypeIsAssignableFromPassedType()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method",
                new Type[] { typeof(IChild) });

            Assert.Equal("Method", method.Name);
            Assert.Equal(typeof(NonGenericClass), method.DeclaringType);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(IParent), method.GetParameters()[0].ParameterType);
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

    public interface IParent
    {
        void Method();
    }

    public interface IChild : IParent { }

    public class NonGenericClass
    {
        public void Method() { }

        public void Method(int arg) { }

        public void Method(int arg0, int arg1) { }

        public void Method(NonGenericClass arg) { }

        public void Method(IParent arg) { }

        public void GenericMethod<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2) { }

        public void GenericMethod<T0>() { }

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
