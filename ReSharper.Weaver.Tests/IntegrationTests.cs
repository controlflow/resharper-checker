using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using NUnit.Framework;
using ReSharper.Weaver.Fody;
using ReSharper.Weaver.TestData;

namespace ReSharper.Weaver.Tests
{
  [TestFixture]
  public sealed class IntegrationTests
  {
    private readonly Assembly myResultingAssembly;

    public IntegrationTests()
    {
      // move to assembly fixture

      var assembly = typeof(SimpleClass).Assembly;
      var testDataAssemblyPath = MockAssemblyResolver.FindPath(assembly);

      var outputDirectory = Path.GetDirectoryName(testDataAssemblyPath);
      var assemblyResolver = new MockAssemblyResolver(outputDirectory);

      var targetAssemblyPath = Path.ChangeExtension(testDataAssemblyPath, "2.dll");
      File.Copy(testDataAssemblyPath, targetAssemblyPath, overwrite: true);

      var readerParameters = new ReaderParameters { AssemblyResolver = assemblyResolver };
      var writerParameters = new WriterParameters();

      var testDataSymbolsPath = Path.ChangeExtension(testDataAssemblyPath, "pdb");
      if (File.Exists(testDataSymbolsPath))
      {
        readerParameters.SymbolReaderProvider = new PdbReaderProvider();
        readerParameters.ReadSymbols = true;
        writerParameters.SymbolWriterProvider = new PdbWriterProvider();
        writerParameters.WriteSymbols = true;

        File.Copy(testDataSymbolsPath, Path.ChangeExtension(testDataSymbolsPath, "2.pdb"), overwrite: true);
      }

      var moduleDefinition = ModuleDefinition.ReadModule(targetAssemblyPath, readerParameters);
      var weavingTask = new ModuleWeaver(moduleDefinition, assemblyResolver);

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

    [Test] public void SingleArgument()
    {
      Assert.DoesNotThrow(() => Instance.SingleArgument("abc"));
      Assert.Throws<ArgumentNullException>(() => Instance.SingleArgument(null));
    }

    [Test] public void MultipleArguments()
    {
      Assert.DoesNotThrow(() => Instance.MultipleArguments("abc", "def", "ghi"));
      Assert.DoesNotThrow(() => Instance.MultipleArguments("abc", "def", null));
      Assert.Throws<ArgumentNullException>(() => Instance.MultipleArguments(null, "def", null));
      Assert.Throws<ArgumentNullException>(() => Instance.MultipleArguments("abc", null, null));
    }

    [Test] public void ByRefParameter() {
      string str = "abc", nullStr = null;
      Assert.DoesNotThrow(() => Instance.ByRefParameter(ref str));
      Assert.Throws<ArgumentNullException>(() => Instance.ByRefParameter(ref nullStr));
    }

    [Test] public void ParamsArgument() {
      Assert.DoesNotThrow(() => Instance.ParamsArgument("abc", "def"));
      Assert.Throws<NullReferenceException>(() => Instance.ParamsArgument("abc", null));
      Assert.Throws<ArgumentNullException>(() => Instance.ParamsArgument(null));
    }
  }
}