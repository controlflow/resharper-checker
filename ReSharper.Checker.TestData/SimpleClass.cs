using System;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Checker.TestData
{
  public class SimpleClass {
    public void SingleArgument([NotNull] string arg) {
      GC.KeepAlive(arg.Length);
    }

    public void MultipleArguments(
      [NotNull] object arg1, [NotNull] object arg2, object arg3) {

      try {
        GC.KeepAlive(arg1.GetHashCode());
        GC.KeepAlive(arg2.GetHashCode());
      } finally {
        GC.KeepAlive(this);
      }
    }

    public string ByRefParameter([NotNull] ref string arg) {
      return arg;
    }

    public void ParamsArgument([NotNull] params string[] xs) {
      foreach (var s in xs) {
        GC.KeepAlive(s.Length);
      }
    }
  }
}
