using System;
using JetBrains.Annotations;
using Mono.Cecil;

namespace ReSharper.Weaver.Fody {
  public sealed class ModuleWeaver {
    [NotNull] private readonly ModuleDefinition myModuleDefinition;
    [NotNull] private readonly IAssemblyResolver myAssemblyResolver;

    public ModuleWeaver([NotNull] ModuleDefinition moduleDefinition,
                        [NotNull] IAssemblyResolver assemblyResolver) {
      myModuleDefinition = moduleDefinition;
      myAssemblyResolver = assemblyResolver;
    }

    public void Execute() {
      var notNullAttributes = NotNullAttributeUtil.FindAttributes(myModuleDefinition);
      if (notNullAttributes.Count == 0) return;

      var argumentNullCtor = ArgumentNullExceptionUtil.FindConstructor(myModuleDefinition);
      if (argumentNullCtor == null) return;

      foreach (var typeDefinition in myModuleDefinition.GetTypes()) {
        foreach (var methodDefinition in typeDefinition.Methods) {
          if (methodDefinition.HasBody) {
            
            if (methodDefinition.HasParameters) {
              foreach (var parameterDefinition in methodDefinition.Parameters) {
                
                if (parameterDefinition.HasCustomAttributes) {
                  foreach (var customAttribute in parameterDefinition.CustomAttributes) {
                    var metadataToken = customAttribute.AttributeType.MetadataToken;
                    if (notNullAttributes.Contains(metadataToken)) {
                      var collection = methodDefinition.Body.Instructions;
                      var instructions = ArgumentNullExceptionUtil.EmitNullCheckInstruction(
                        parameterDefinition.Index,
                        argumentNullCtor,
                        collection[0], parameterDefinition.Name,
                        "Violation of parameter [NotNull] contract");

                      for (var index = 0; index < instructions.Length; index++) {
                        
                      }


                      GC.KeepAlive(this);
                    }
                  }
                }
              }
            }

          }
        }
      }




    }
  }
}