using System;
using JetBrains.Annotations;

namespace ReSharper.Weaver.TestData
{
  public class SimpleClass {
    public void Foo([NotNull] string arg) {
      Console.WriteLine("arg: {0}", arg);
    }
  }
}
