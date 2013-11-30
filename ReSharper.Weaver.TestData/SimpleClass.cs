using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ReSharper.Weaver.TestData
{
  public class SimpleClass
  {
    //[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public void Foo()
    {
      Console.WriteLine("LOLO");
    }
  }
}
