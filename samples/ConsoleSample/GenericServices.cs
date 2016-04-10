using System;

namespace ConsoleSample
{
    public class GenericServices<TType>
    {
        public void Method<TMethod>(TType arg1, TMethod arg2)
        {
            Console.WriteLine($"Arg1: {arg1}, Arg2: {arg2}");
        }
    }
}