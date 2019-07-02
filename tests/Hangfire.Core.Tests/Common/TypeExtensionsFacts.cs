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
	        Assert.Equal(typeof(GenericClass<>).ToGenericTypeString(), "GenericClass<T1>");
            Assert.Equal(typeof(GenericClass<>.NestedNonGenericClass).ToGenericTypeString(), "GenericClass<T1>.NestedNonGenericClass");
            Assert.Equal(typeof(GenericClass<>.NestedNonGenericClass.DoubleNestedGenericClass<,,>).ToGenericTypeString(), "GenericClass<T1>.NestedNonGenericClass.DoubleNestedGenericClass<T2,T3,T4>");
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
                new[] { typeof(int) });

            Assert.Equal("Method", method.Name);
            Assert.Equal(typeof(NonGenericClass), method.DeclaringType);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethodWithManyParameters()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method",
                new[] { typeof(int), typeof(int) });

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
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "TrivialGenericMethod",
                new[] { typeof(int), typeof(string), typeof(object) });

            Assert.Equal("TrivialGenericMethod", method.Name);
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
                new[] { typeof(object), typeof(int) });

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodNameIsCaseSensitive()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "method", new Type[0]);

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsNull_WhenMethodParameterTypeIsAssignableFromPassedType()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "Method",
                new[] { typeof(NonGenericClass) });

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasParameterWhoseTypeContainsGenericParameter()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "OtherGenericMethod",
                new[] { typeof(IEnumerable<int>) });

            Assert.Equal("OtherGenericMethod", method.Name);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(IEnumerable<int>), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasParameterWhoseTypeContainsGenericParameterAndIsComplicated()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "OtherGenericMethod",
                new[] { typeof(List<IEnumerable<int>>) });

            Assert.Equal("OtherGenericMethod", method.Name);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(List<IEnumerable<int>>), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasParameterWhoseTypeIsGenericAndContainsTwoGenericParameters()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "OtherGenericMethod",
                new[] { typeof(Tuple<int, double>) });

            Assert.Equal("OtherGenericMethod", method.Name);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(Tuple<int, double>), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesNonTrivialOrderOfUsingMethodGenericParametersInMethodParameterTypes()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "OneMoreGenericMethod",
                new[] { typeof(Tuple<int, double, float>) });

            Assert.Equal("OneMoreGenericMethod", method.Name);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(Tuple<int, double, float>), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasSomeParametersOfTheSameTypeWhichIsMethodGenericParameter()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new[] { typeof(int), typeof(int) });

            Assert.Equal("GenericMethod", method.Name);
            Assert.Equal(2, method.GetParameters().Length);
            Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
            Assert.Equal(typeof(int), method.GetParameters()[1].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasGenericAndNonGenericParameters()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new[] { typeof(int), typeof(NonGenericClass), typeof(double) });

            Assert.Equal("GenericMethod", method.Name);
            Assert.Equal(3, method.GetParameters().Length);
            Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
            Assert.Equal(typeof(NonGenericClass), method.GetParameters()[1].ParameterType);
            Assert.Equal(typeof(double), method.GetParameters()[2].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasParameterOfGenericTypeWhichContainsMe()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new[] { typeof(Tuple<double, List<int>>)  });

            Assert.Equal("GenericMethod", method.Name);
            Assert.Equal(1, method.GetParameters().Length);
            Assert.Equal(typeof(Tuple<double,List<int>>), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasSomeParametersWhoseTypesContainsTheSameGenericParameter()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new[] { typeof(int), typeof(double) });

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsNull_WhenParameterTypeIsMatchedByGenericTypeAndNotMatchedByGenericArguments()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "OtherGenericMethod",
                new[] { typeof(List<int>)});

            Assert.Equal(null, method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethod_WhenParameterTypeIsGenericArray()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new[] { typeof(string[]) });

            Assert.Equal("GenericMethod", method.Name);
            Assert.Equal(typeof(string[]), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsCorrectMethod_WhenParameterTypeIsComplicatedGenericArray()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new[] { typeof(List<int>[]) });

            Assert.Equal("GenericMethod", method.Name);
            Assert.Equal(typeof(List<int>[]), method.GetParameters()[0].ParameterType);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_ReturnsNull_WhenMatchingGenricMethodNotBeFound()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new[] { typeof(Tuple<List<int>, string>) });

            Assert.Null(method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasNoParametersOrTypes()
        {
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new Type[0]);

            Assert.Null(method);
        }

        [Fact]
        public void GetNonOpenMatchingMethod_HandlesMethodHasNoParametersOrTypes2()
        {
            // public void GenericMethod<T0, T1>(T0 arg) { }
            var method = TypeExtensions.GetNonOpenMatchingMethod(typeof(NonGenericClass), "GenericMethod",
                new [] { typeof(string) });

            Assert.Null(method);
        }
    }

    public class GenericClass<T1>
    {
        public class NestedNonGenericClass
        {
            public class DoubleNestedGenericClass<T2, T3, T4>
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

        public void Method(IParent arg) { }

        public void Method(object arg) { }

        public void TrivialGenericMethod<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2) { }

        public void OtherGenericMethod<T>(IEnumerable<T> arg0) { }

        public void OtherGenericMethod<T>(List<IEnumerable<T>> arg0) { }

        public void OtherGenericMethod<T0, T1>(Tuple<T0, T1> arg0) { }

        public void OneMoreGenericMethod<T0, T1, T2>(Tuple<T2, T0, T1> arg0) { }

        public void GenericMethod<T0>() { }

        public void GenericMethod<T0, T1>(T0 arg) { }

        public void GenericMethod<T>(T arg0, T arg1) { }

        public void GenericMethod<T>(int arg0, T arg1, double arg2) { }

        public void GenericMethod<T>(Tuple<T, List<int>> arg) { }

        public void GenericMethod<T>(T[] arg) { }

        public void GenericMethod<T>(List<T>[] arg) { }

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
