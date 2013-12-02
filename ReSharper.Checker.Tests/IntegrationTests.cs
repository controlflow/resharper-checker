using System;
using JetBrains.ReSharper.Checker.TestData;
using NUnit.Framework;

namespace JetBrains.ReSharper.Checker.Tests {
  [TestFixture]
  public sealed class IntegrationTests {
    private dynamic SimpleClass {
      get { return CheckerTestAssembly.GetWeavedType<SimpleClass>(); }
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
  }
}