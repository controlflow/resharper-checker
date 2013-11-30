using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ReSharper.Weaver.Fody {
  public sealed class ModuleWeaver {
    [NotNull] private readonly ModuleDefinition myModuleDefinition;
    [NotNull] private readonly IAssemblyResolver myAssemblyResolver;

    [NotNull] private static readonly string NotNullName = typeof(NotNullAttribute).Name;
    [NotNull] private static readonly string SysAttributeFqn = typeof(Attribute).FullName;

    public ModuleWeaver([NotNull] ModuleDefinition moduleDefinition,
                        [NotNull] IAssemblyResolver assemblyResolver) {
      myModuleDefinition = moduleDefinition;
      myAssemblyResolver = assemblyResolver;
    }

    private static bool IsNotNullAttribute([NotNull] TypeReference type) {
      if (type.Name != NotNullName) return false;

      if (type.IsValueType)       return false;
      if (type.IsArray)           return false;
      if (type.IsGenericInstance) return false;
      if (type.IsPointer)         return false;
      if (type.IsNested)          return  false;
      if (type.IsByReference)     return false;

      var typeDefinition = type.Resolve();
      if (typeDefinition != null) {
        if (typeDefinition.HasGenericParameters) return false;
        if (typeDefinition.IsAbstract)           return false;
        if (!typeDefinition.IsClass)             return false;

        var baseTypeReference = typeDefinition.BaseType;
        if (baseTypeReference == null)                       return false;
        if (baseTypeReference.FullName != SysAttributeFqn) return false;
      }

      return true;
    }

    [NotNull] private HashSet<MetadataToken> FindNotNullAttributes() {
      var notNullTokens = new HashSet<MetadataToken>();

      foreach (var typeReference in myModuleDefinition.GetTypeReferences()) {
        if (IsNotNullAttribute(typeReference))
          notNullTokens.Add(typeReference.MetadataToken);
      }

      foreach (var typeReference in myModuleDefinition.GetTypes()) {
        if (IsNotNullAttribute(typeReference))
          notNullTokens.Add(typeReference.MetadataToken);
      }

      return notNullTokens;
    }

    public void Execute() {
      var notNullAttributes = FindNotNullAttributes();
      if (notNullAttributes.Count == 0) return;





      // check type refs and type defs

      var consoleDef = myModuleDefinition.Import(typeof (Console)).Resolve();
      var writeLine = myModuleDefinition.Import(consoleDef.Methods.First(m =>
        !m.IsConstructor && m.Name == "WriteLine" && m.Parameters.Count == 1 &&
        m.Parameters[0].ParameterType.FullName == typeof (String).FullName));

      foreach (var typeDefinition in myModuleDefinition.GetTypes()) {
        foreach (var methodDefinition in typeDefinition.Methods) {
          if (methodDefinition.HasBody && !methodDefinition.IsConstructor) {
            var methodBody = methodDefinition.Body;

            var str = Instruction.Create(OpCodes.Ldstr, "REWRITE LOL");
            var wl = Instruction.Create(OpCodes.Call, writeLine);

            methodBody.Instructions.Insert(0, str);
            methodBody.Instructions.Insert(1, wl);

            //Instruction.Create()
          }
        }
      }
    }
  }
}