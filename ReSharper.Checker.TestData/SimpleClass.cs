using System;
using System.Collections.Generic;
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

    // ReSharper disable once RedundantAssignment
    public void ByRefParameter([NotNull] ref string arg, string value) {
      arg = value;
    }

    public void ByRefOutParameter([NotNull] out string outArg, string value) {
      outArg = value;
    }

    public void ParamsArgument([NotNull] params string[] xs) {
      foreach (var s in xs) {
        GC.KeepAlive(s.Length);
      }
    }

    [NotNull] public string ReturnValue(string input) {
      return input;
    }

    public unsafe void PointersTest(bool @throw) {
      var value = 42;
      // ReSharper disable once AssignNullToNotNullAttribute
      var t = PointerPassThrough(@throw ? &value : null);
      GC.KeepAlive((IntPtr) t);
    }

    [NotNull]
    private static unsafe int* PointerPassThrough(int* ptr) {
      return ptr;
    }

    // ReSharper disable AnnotationRedundanceAtValueType
    [NotNull] public int IncorrectAttributeUsage(
      [NotNull] int arg, [NotNull] ref int arg2, [NotNull] out int arg3) {
      return arg3 = 0;
    }

    [NotNull] public void IncorrectAttributeUsage2() { }

    [NotNull] public string BuggyMethod([NotNull] out string value) {
      var cache = new Dictionary<int, string>();
      if (!cache.TryGetValue(42, out value)) {
        // ReSharper disable once AssignNullToNotNullAttribute
        value = null;
      }

      return "smth";
    }

    [NotNull] public T1 GenericChecks<T1, T2, T3>(
      [NotNull] T1 arg1, [NotNull] T2 arg2, [NotNull] T3 arg3)
      where T2 : struct
      where T3 : class {
      return arg1;
    }
  }
}
