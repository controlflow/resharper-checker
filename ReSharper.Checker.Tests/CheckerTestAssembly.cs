using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.ReSharper.Checker;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using NUnit.Framework;
using ReSharper.Weaver.Tests;

// ReSharper disable once CheckNamespace

[SetUpFixture]
public sealed class CheckerTestAssembly {
  private static readonly List<Assembly> WeavedAssemblies = new List<Assembly>();
  private static readonly Dictionary<Type, object> Instances = new Dictionary<Type, object>();

  [SetUp] public static void SetUp() {
    var assembly = typeof(CheckerTestAssembly).Assembly;
    var currentAssemblyPath = MockAssemblyResolver.FindPath(assembly);
    var testDirectory = Path.GetDirectoryName(currentAssemblyPath);
    if (testDirectory == null) return;

    foreach (var testDataFile in Directory.EnumerateFiles(testDirectory, "*TestData*.dll")) {
      if (testDataFile.IndexOf("weaved.dll", StringComparison.OrdinalIgnoreCase) > -1) continue;

      var weavedAssembly = WeaveAssembly(testDataFile);
      WeavedAssemblies.Add(weavedAssembly);
    }
  }

  private static Assembly WeaveAssembly(string testDataAssemblyPath) {
    var outputDirectory = Path.GetDirectoryName(testDataAssemblyPath);

    IAssemblyResolver assemblyResolver;
    if (testDataAssemblyPath.IndexOf("NoRef", StringComparison.OrdinalIgnoreCase) > -1) {
      assemblyResolver = new DefaultAssemblyResolver();
    } else {
      assemblyResolver = new MockAssemblyResolver(outputDirectory);
    }

    var targetAssemblyPath = Path.ChangeExtension(testDataAssemblyPath, "weaved.dll");
    File.Copy(testDataAssemblyPath, targetAssemblyPath, overwrite: true);

    var readerParameters = new ReaderParameters {AssemblyResolver = assemblyResolver};
    var writerParameters = new WriterParameters();

    var testDataSymbolsPath = Path.ChangeExtension(testDataAssemblyPath, "pdb");
    if (File.Exists(testDataSymbolsPath)) {
      readerParameters.SymbolReaderProvider = new PdbReaderProvider();
      readerParameters.ReadSymbols = true;
      writerParameters.SymbolWriterProvider = new PdbWriterProvider();
      writerParameters.WriteSymbols = true;

      File.Copy(testDataSymbolsPath,
        Path.ChangeExtension(testDataSymbolsPath, "weaved.pdb"), overwrite: true);
    }

    var moduleDefinition = ModuleDefinition.ReadModule(targetAssemblyPath, readerParameters);
    var weavingTask = new ModuleWeaver {
      ModuleDefinition = moduleDefinition,
      AssemblyResolver = assemblyResolver
    };

    weavingTask.Execute();
    moduleDefinition.Write(targetAssemblyPath, writerParameters);

    return Assembly.LoadFile(targetAssemblyPath);
  }

  public static object GetWeavedType<T>() {
    lock (Instances) {
      var originalType = typeof(T);

      object value;
      if (Instances.TryGetValue(originalType, out value))
        return value;

      foreach (var weavedAssembly in WeavedAssemblies) {
        var weavedType = weavedAssembly.GetType(originalType.FullName, false);
        if (weavedType != null) {
          var instance = Activator.CreateInstance(weavedType);
          Instances[originalType] = instance;
          return instance;
        }
      }

      throw new ArgumentException(string.Format(
        "Type {0} is not found", originalType.FullName));
    }
  }
}