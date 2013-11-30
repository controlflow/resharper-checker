using System;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ReSharper.Weaver.Fody {
  public class ArgumentNullExceptionUtil {
    [NotNull] private static readonly string StringTypeFqn = typeof(String).FullName;

    [CanBeNull] public static MethodReference FindConstructor([NotNull] ModuleDefinition module) {
      // todo: review how this thing works
      var typeReference = module.Import(typeof(ArgumentNullException));
      if (typeReference == null) return null;

      var typeDefinition = typeReference.Resolve();
      if (typeDefinition == null) return null;

      // look for constructor with two arguments
      foreach (var methodDefinition in typeDefinition.Methods) {
        if (methodDefinition.IsConstructor && methodDefinition.HasParameters) {
          var parameters = methodDefinition.Parameters;
          if (parameters.Count == 2
            && parameters[0].ParameterType.FullName == StringTypeFqn
            && parameters[1].ParameterType.FullName == StringTypeFqn) {
            return methodDefinition;
          }
        }
      }

      return null;
    }

    [NotNull] public static Instruction[] EmitNullCheckInstruction(
      int reference, [NotNull] MethodReference constructorReference,
      [NotNull] Instruction target, [NotNull] string paramName, [NotNull] string message) {

      var loadArgumentInstruction = Instruction.Create(OpCodes.Ldarg, reference);
      var nullCheckInstruction = Instruction.Create(OpCodes.Brtrue, target); // check what is emitting
      var loadParamNameInstruction = Instruction.Create(OpCodes.Ldstr, paramName);
      var loadMessageInstruction = Instruction.Create(OpCodes.Ldstr, message);
      var createExceptionInstruction = Instruction.Create(OpCodes.Newobj, constructorReference);
      var throwInstruction = Instruction.Create(OpCodes.Throw);

      return new[] {
        loadArgumentInstruction, nullCheckInstruction,
        loadParamNameInstruction, loadMessageInstruction,
        createExceptionInstruction, throwInstruction
      };
    }
  }
}