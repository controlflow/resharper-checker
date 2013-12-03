using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Checker.TestData
{
  public class SimpleClass : SimpleBaseClass, ISimpleInterface, IOtherInterface {
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
        // ReSharper disable once RedundantAssignment
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

    public override string AbstractMethod(string arg) {
      return arg;
    }

    public override string VirtualMethod(string arg, [NotNull] string arg2) {
      return arg;
    }

    public string InterfaceMethod(string arg) {
      return arg;
    }

    public void InterfaceMethod2(string arg) {
      ((ISimpleInterface) this).InterfaceMethod2(arg);
    }

    void ISimpleInterface.InterfaceMethod2(string arg) {
      Console.WriteLine(arg);
    }

    public string MultipleImplMethod(string arg1, string arg2) {
      return arg1;
    }

    public override string VirtualMethod2(string arg, string arg2) {
      return arg;
    }

    public void ManyArgs(
      [NotNull] string arg0, [NotNull] string arg1,
      [NotNull] string arg2, [NotNull] string arg3,
      [NotNull] string arg4, [NotNull] string arg5,
      [NotNull] string arg6, [NotNull] string arg7,
      [NotNull] string arg8, [NotNull] string arg9,
      [NotNull] string arg10, [NotNull] string arg11,
      [NotNull] string arg12, [NotNull] string arg13,
      [NotNull] string arg14, [NotNull] string arg15,
      [NotNull] string arg16, [NotNull] string arg17,
      [NotNull] string arg18, [NotNull] string arg19) {
      
    }

    // todo: support annotations like this in R#
    public string Property1 { [NotNull] get; [param: NotNull] set; }

    [NotNull] public string Property2 { get; set; }

    public override string PropertyVirtual { get; set; }
    public string PropertyInterface { get; set; }
  }
}
