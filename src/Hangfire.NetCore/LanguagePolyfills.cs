// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
#if !NETSTANDARD2_1_OR_GREATER
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
        public bool ReturnValue { get; }
    }
#endif
}