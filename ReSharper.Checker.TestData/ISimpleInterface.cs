using JetBrains.Annotations;

namespace JetBrains.ReSharper.Checker.TestData {
  public interface ISimpleInterface : IEvenMoreSimpleInterface {
    [NotNull] string InterfaceMethod(string arg);
    void InterfaceMethod2([NotNull] string arg);
  }

  public interface IEvenMoreSimpleInterface {
    [NotNull] string MultipleImplMethod(string arg1, string arg2);

    [NotNull] string PropertyInterface { get; }
  }

  public interface IOtherInterface {
    string MultipleImplMethod(string arg1, [NotNull] string arg2);
    string VirtualMethod2(string arg, [NotNull] string arg2);
  }
}