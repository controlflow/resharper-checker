using System;
using JetBrains.Annotations;
using Mono.Cecil;

namespace JetBrains.ReSharper.Checker {
  public static class ArgumentNullExceptionUtil {
    [NotNull] private static readonly string StringTypeFqn = typeof(String).FullName;

    [CanBeNull] public static MethodReference FindConstructor([NotNull] ModuleDefinition module) {
      // todo: review how this thing works
      var typeReference = module.Import(typeof(ArgumentNullException));
      if (typeReference == null) return null;

      var typeDefinition = typeReference.Resolve();
      if (typeDefinition == null) return null;

      // look for constructor with two 'string' arguments
      foreach (var methodDefinition in typeDefinition.Methods) {
        if (methodDefinition.IsConstructor && methodDefinition.HasParameters) {
          var parameters = methodDefinition.Parameters;
          if (parameters.Count == 2
            && parameters[0].ParameterType.FullName == StringTypeFqn
            && parameters[1].ParameterType.FullName == StringTypeFqn) {
            return module.Import(methodDefinition);
          }
        }
      }

      return null;
    }
  }
}