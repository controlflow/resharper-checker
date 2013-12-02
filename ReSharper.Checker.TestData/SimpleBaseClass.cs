using JetBrains.Annotations;

namespace JetBrains.ReSharper.Checker.TestData {
  public abstract class SimpleBaseClass {
    [NotNull] public abstract string AbstractMethod(string arg);

    [NotNull] public virtual string VirtualMethod(string arg, string arg2) {
      return arg;
    }

    [NotNull] public virtual string VirtualMethod2(string arg, string arg2) {
      return arg;
    }
  }
}