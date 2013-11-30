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

    private static bool IsNotNullAttribute([NotNull] TypeReference reference) {
      if (reference.Name != NotNullName) return false;

      if (reference.IsValueType)       return false;
      if (reference.IsArray)           return false;
      if (reference.IsGenericInstance) return false;
      if (reference.IsPointer)         return false;
      if (reference.IsNested)          return false;
      if (reference.IsByReference)     return false;

      var definition = reference.Resolve();
      if (definition != null) {
        if (definition.HasGenericParameters) return false;
        if (definition.IsAbstract)           return false;
        if (!definition.IsClass)             return false;

        var baseReference = definition.BaseType;
        if (baseReference == null)                     return false;
        if (baseReference.FullName != SysAttributeFqn) return false;
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

    [CanBeNull] private MethodDefinition FindArgumentNullConstructor() {
      var typeReference = myModuleDefinition.Import(typeof(ArgumentNullException));
      if (typeReference == null) return null;

      var typeDefinition = typeReference.Resolve();
      if (typeDefinition == null) return null;

      foreach (var methodDefinition in typeDefinition.Methods) {
        if (methodDefinition.IsConstructor && methodDefinition.HasParameters) {
          var parameters = methodDefinition.Parameters;
          if (parameters.Count == 1 &&
              parameters[0].ParameterType.FullName == typeof(String).FullName) {
            return methodDefinition;
          }
        }
      }

      return null;
    }

    public void Execute() {
      var notNullAttributes = FindNotNullAttributes();
      if (notNullAttributes.Count == 0) return;

      var argumentNullConstructor = FindArgumentNullConstructor();
      if (argumentNullConstructor == null) return;

      var paramNameInstruction = Instruction.Create(OpCodes.Ldstr, "arg");
      var boooooo = Instruction.Create(OpCodes.Ldstr, "Violation of [NotNull] contract");

      

      //Instruction.Create()


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