using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.ReSharper.Checker;
using Mono.Cecil;
using Mono.Cecil.Pdb;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

[SetUpFixture]
public sealed class CheckerTestAssembly {
  private static readonly List<Assembly> WeavedAssemblies = new List<Assembly>();
  private static readonly Dictionary<Type, object> Instances = new Dictionary<Type, object>();

  [SetUp] public static void SetUp() {
    var assembly = typeof(CheckerTestAssembly).Assembly;
    var currentAssemblyPath = JetBrains.ReSharper.Checker.Tests.MockAssemblyResolver.FindPath(assembly);
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
      assemblyResolver = new MockAssemblyResolver();
    } else {
      assemblyResolver = new JetBrains.ReSharper.Checker.Tests.MockAssemblyResolver(outputDirectory);
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

  public static object GetWeavedTypeFor<T>() {
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

  private sealed class MockAssemblyResolver : DefaultAssemblyResolver {
    private bool IsTestData([NotNull] string name) {
      return name.IndexOf("TestData", StringComparison.OrdinalIgnoreCase) > -1;
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name) {
      if (IsTestData(name.FullName)) return null;
      return base.Resolve(name);
    }

    public override AssemblyDefinition Resolve(string fullName) {
      if (IsTestData(fullName)) return null;
      return base.Resolve(fullName);
    }

    public override AssemblyDefinition Resolve(string fullName, ReaderParameters parameters) {
      if (IsTestData(fullName)) return null;
      return base.Resolve(fullName, parameters);
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) {
      if (IsTestData(name.FullName)) return null;
      return base.Resolve(name, parameters);
    }
  }
}