using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Checker.TestData;

namespace JetBrains.ReSharper.Checker.TestDataRef {
  public class DerivedClass : SimpleClass {
    public void UsesReferencedNotNull([NotNull] string arg) {
      Console.WriteLine("Called");
    }
  }
}