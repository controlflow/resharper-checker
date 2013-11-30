using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using ReSharper.Weaver.Fody;
using ReSharper.Weaver.TestData;

namespace ReSharper.Weaver.Tests
{
  [TestClass]
  public sealed class IntegrationTests
  {
    private readonly Assembly myResultingAssembly;

    public IntegrationTests()
    {
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

    [TestMethod]
    public void TestMethod1()
    {
      var type = myResultingAssembly.GetType(typeof(SimpleClass).FullName);
      dynamic instance = Activator.CreateInstance(type);
      instance.Foo();
    }
  }
}