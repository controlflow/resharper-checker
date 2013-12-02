using System;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Checker.TestDataNoRef {
  public class DetachedDerivedClass {
    public void UsesReferencedNotNull([NotNull] string arg) {
      Console.WriteLine("Called");
    }
  }
}
