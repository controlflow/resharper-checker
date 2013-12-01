using System;
using System.IO;
using System.Reflection;
using Mono.Cecil;

namespace ReSharper.Weaver.Tests {
  public sealed class MockAssemblyResolver : IAssemblyResolver {
    private readonly string myDirectory;

    public MockAssemblyResolver(string directory) {
      myDirectory = directory;
    }

    public AssemblyDefinition Resolve(AssemblyNameReference name) {
      var fileName = Path.ChangeExtension(Path.Combine(myDirectory, name.Name), "dll");

      if (File.Exists(fileName))
        return AssemblyDefinition.ReadAssembly(fileName);

      var findPath = FindPath(Assembly.Load(name.FullName));
      return AssemblyDefinition.ReadAssembly(findPath);
    }

    public AssemblyDefinition Resolve(string fullName) {
      var findPath = FindPath(Assembly.Load(fullName));
      return AssemblyDefinition.ReadAssembly(findPath);
    }

    public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) {
      throw new NotSupportedException();
    }

    public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters) {
      throw new NotSupportedException();
    }

    public static string FindPath(Assembly assembly) {
      const string fileSchema = "file:///";

      var codeBase = assembly.CodeBase;
      if (!codeBase.StartsWith(fileSchema, StringComparison.Ordinal)) return codeBase;

      return codeBase.Substring(fileSchema.Length);
    }
  }
}