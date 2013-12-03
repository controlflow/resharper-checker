using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Checker.TestData;
using JetBrains.ReSharper.Checker.TestDataNoRef;
using JetBrains.ReSharper.Checker.TestDataRef;
using NUnit.Framework;

namespace JetBrains.ReSharper.Checker.Tests {
  [TestFixture]
  public sealed class IntegrationTests {
    [NotNull] private dynamic SimpleClass {
      get { return CheckerTestAssembly.GetWeavedTypeFor<SimpleClass>(); }
    }

    [NotNull] private dynamic DerivedClass {
      get { return CheckerTestAssembly.GetWeavedTypeFor<DerivedClass>(); }
    }

    [NotNull] private dynamic DetachedDerivedClass {
      get { return CheckerTestAssembly.GetWeavedTypeFor<DetachedDerivedClass>(); }
    }

    [Test] public void SingleArgument() {
      Assert.DoesNotThrow(() => SimpleClass.SingleArgument("abc"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.SingleArgument(null));
    }

    [Test] public void MultipleArguments() {
      Assert.DoesNotThrow(() => SimpleClass.MultipleArguments("abc", "def", "ghi"));
      Assert.DoesNotThrow(() => SimpleClass.MultipleArguments("abc", "def", null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.MultipleArguments(null, "def", null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.MultipleArguments("abc", null, null));
    }

    [Test] public void ByRefParameter() {
      string str = "abc", nullStr = null;
      Assert.DoesNotThrow(() => SimpleClass.ByRefParameter(ref str, "boo"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.ByRefParameter(ref nullStr, "boo"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.ByRefParameter(ref str, null));
    }

    [Test] public void ByRefOutParameter() {
      string str;
      Assert.DoesNotThrow(() => SimpleClass.ByRefOutParameter(out str, "abc"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.ByRefOutParameter(out str, null));
    }

    [Test] public void ParamsArgument() {
      Assert.DoesNotThrow(() => SimpleClass.ParamsArgument("abc", "def"));
      Assert.Throws<NullReferenceException>(() => SimpleClass.ParamsArgument("abc", null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.ParamsArgument(null));
    }

    [Test] public void ReturnValue() {
      Assert.DoesNotThrow(() => SimpleClass.ReturnValue("abc"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.ReturnValue(null));
    }

    [Test] public void PointersTest() {
      Assert.DoesNotThrow(() => SimpleClass.PointersTest(true));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.PointersTest(false));
    }

    [Test] public void IncorrectAttributeUsages() {
      Assert.DoesNotThrow(() => {
        var arg = 42;
        SimpleClass.IncorrectAttributeUsage(arg, ref arg, out arg);
        SimpleClass.IncorrectAttributeUsage2();
      });
    }

    [Test] public void BuggyMethod() {
      Assert.Throws<ArgumentNullException>(() => {
        string arg; SimpleClass.BuggyMethod(out arg);
      });
    }

    [Test] public void GenericChecks() {
      Assert.DoesNotThrow(() => SimpleClass.GenericChecks("abc", 123, "def"));
      Assert.DoesNotThrow(() => SimpleClass.GenericChecks("abc", 0, "def"));
      const string nullStr = null;
      Assert.Throws<ArgumentNullException>(() => SimpleClass.GenericChecks(nullStr, 123, "def"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.GenericChecks("abc", 123, nullStr));
    }

    [Test] public void InheritedAnnotations() {
      Assert.DoesNotThrow(() => SimpleClass.AbstractMethod("abc"));
      Assert.DoesNotThrow(() => SimpleClass.VirtualMethod("abc", "abc"));
      Assert.DoesNotThrow(() => SimpleClass.InterfaceMethod("abc"));
      Assert.DoesNotThrow(() => SimpleClass.InterfaceMethod2("abc"));
      Assert.DoesNotThrow(() => SimpleClass.MultipleImplMethod("abc", "def"));
      Assert.DoesNotThrow(() => SimpleClass.VirtualMethod2("abc", "def"));

      Assert.Throws<ArgumentNullException>(() => SimpleClass.AbstractMethod(null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.VirtualMethod(null, "abc"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.VirtualMethod("abc", null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.InterfaceMethod(null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.InterfaceMethod2(null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.MultipleImplMethod("abc", null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.MultipleImplMethod(null, "def"));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.VirtualMethod2("abc", null));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.VirtualMethod2(null, "def"));
    }

    [Test] public void NotNullFromReferenced() {
      Assert.DoesNotThrow(() => DerivedClass.UsesReferencedNotNull("abc"));
      Assert.Throws<ArgumentNullException>(() => DerivedClass.UsesReferencedNotNull(null));
    }

    [Test] public void NotNullFromUnresolved() {
      Assert.DoesNotThrow(() => DetachedDerivedClass.UsesReferencedNotNull("abc"));
      Assert.Throws<ArgumentNullException>(() => DetachedDerivedClass.UsesReferencedNotNull(null));
    }

    [Test] public void PropertyAccessors() {
      Assert.Throws<ArgumentNullException>(() => GC.KeepAlive(SimpleClass.Property1));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.Property1 = null);
      Assert.DoesNotThrow(() => SimpleClass.Property1 = "abc");
      Assert.DoesNotThrow(() => GC.KeepAlive(SimpleClass.Property1));
    }

    [Test] public void PropertyAnnotation() {
      Assert.Throws<ArgumentNullException>(() => GC.KeepAlive(SimpleClass.Property2));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.Property2 = null);
      Assert.DoesNotThrow(() => SimpleClass.Property2 = "abc");
      Assert.DoesNotThrow(() => GC.KeepAlive(SimpleClass.Property2));
    }

    [Test] public void PropertyVirtual() {
      Assert.Throws<ArgumentNullException>(() => GC.KeepAlive(SimpleClass.PropertyVirtual));
      Assert.Throws<ArgumentNullException>(() => SimpleClass.PropertyVirtual = null);
      Assert.DoesNotThrow(() => SimpleClass.PropertyVirtual = "abc");
      Assert.DoesNotThrow(() => GC.KeepAlive(SimpleClass.PropertyVirtual));
    }

    [Test] public void PropertyInterface() {
      Assert.DoesNotThrow(() => SimpleClass.PropertyInterface = "abc");
      Assert.DoesNotThrow(() => GC.KeepAlive(SimpleClass.PropertyInterface));
      Assert.DoesNotThrow(() => SimpleClass.PropertyInterface = null);
      Assert.Throws<ArgumentNullException>(() => GC.KeepAlive(SimpleClass.PropertyInterface));
    }
  }
}