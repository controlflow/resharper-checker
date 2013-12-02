using System;
using System.IO;
using System.Reflection;
using JetBrains.ReSharper.Checker.TestData;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using NUnit.Framework;
using ReSharper.Weaver.Tests;

namespace JetBrains.ReSharper.Checker.Tests {
  [TestFixture]
  public sealed class IntegrationTests {
    private readonly Assembly myResultingAssembly;

    public IntegrationTests() {
      // move to assembly fixture

      var assembly = typeof(SimpleClass).Assembly;
      var testDataAssemblyPath = MockAssemblyResolver.FindPath(assembly);

      var outputDirectory = Path.GetDirectoryName(testDataAssemblyPath);
      var assemblyResolver = new MockAssemblyResolver(outputDirectory);

      var targetAssemblyPath = Path.ChangeExtension(testDataAssemblyPath, "2.dll");
      File.Copy(testDataAssemblyPath, targetAssemblyPath, overwrite: true);

      var readerParameters = new ReaderParameters {AssemblyResolver = assemblyResolver};
      var writerParameters = new WriterParameters();

      var testDataSymbolsPath = Path.ChangeExtension(testDataAssemblyPath, "pdb");
      if (File.Exists(testDataSymbolsPath)) {
        readerParameters.SymbolReaderProvider = new PdbReaderProvider();
        readerParameters.ReadSymbols = true;
        writerParameters.SymbolWriterProvider = new PdbWriterProvider();
        writerParameters.WriteSymbols = true;

        File.Copy(testDataSymbolsPath, Path.ChangeExtension(testDataSymbolsPath, "2.pdb"), overwrite: true);
      }

      var moduleDefinition = ModuleDefinition.ReadModule(targetAssemblyPath, readerParameters);
      var weavingTask = new ModuleWeaver {
        ModuleDefinition = moduleDefinition,
        AssemblyResolver = assemblyResolver
      };

      weavingTask.Execute();
      moduleDefinition.Write(targetAssemblyPath, writerParameters);

      myResultingAssembly = Assembly.LoadFile(targetAssemblyPath);
    }

    private dynamic Instance {
      get {
        var type = myResultingAssembly.GetType(typeof(SimpleClass).FullName);
        Assert.That(type, Is.Not.Null);

        return Activator.CreateInstance(type);
      }
    }

    [Test] public void SingleArgument() {
      Assert.DoesNotThrow(() => Instance.SingleArgument("abc"));
      Assert.Throws<ArgumentNullException>(() => Instance.SingleArgument(null));
    }

    [Test] public void MultipleArguments() {
      Assert.DoesNotThrow(() => Instance.MultipleArguments("abc", "def", "ghi"));
      Assert.DoesNotThrow(() => Instance.MultipleArguments("abc", "def", null));
      Assert.Throws<ArgumentNullException>(() => Instance.MultipleArguments(null, "def", null));
      Assert.Throws<ArgumentNullException>(() => Instance.MultipleArguments("abc", null, null));
    }

    [Test] public void ByRefParameter() {
      string str = "abc", nullStr = null;
      Assert.DoesNotThrow(() => Instance.ByRefParameter(ref str, "boo"));
      Assert.Throws<ArgumentNullException>(() => Instance.ByRefParameter(ref nullStr, "boo"));
      Assert.Throws<ArgumentNullException>(() => Instance.ByRefParameter(ref str, null));
    }

    [Test] public void ByRefOutParameter() {
      string str;
      Assert.DoesNotThrow(() => Instance.ByRefOutParameter(out str, "abc"));
      Assert.Throws<ArgumentNullException>(() => Instance.ByRefOutParameter(out str, null));
    }

    [Test] public void ParamsArgument() {
      Assert.DoesNotThrow(() => Instance.ParamsArgument("abc", "def"));
      Assert.Throws<NullReferenceException>(() => Instance.ParamsArgument("abc", null));
      Assert.Throws<ArgumentNullException>(() => Instance.ParamsArgument(null));
    }

    [Test] public void ReturnValue() {
      Assert.DoesNotThrow(() => Instance.ReturnValue("abc"));
      Assert.Throws<ArgumentNullException>(() => Instance.ReturnValue(null));
    }

    [Test] public void PointersTest() {
      Assert.DoesNotThrow(() => Instance.PointersTest(true));
      Assert.Throws<ArgumentNullException>(() => Instance.PointersTest(false));
    }

    [Test] public void IncorrectAttributeUsages() {
      Assert.DoesNotThrow(() => {
        var arg = 42;
        Instance.IncorrectAttributeUsage(arg, ref arg, out arg);
        Instance.IncorrectAttributeUsage2();
      });
    }

    [Test] public void BuggyMethod() {
      Assert.Throws<ArgumentNullException>(() => {
        string arg; Instance.BuggyMethod(out arg);
      });
    }
  }
}