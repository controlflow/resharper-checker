using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JetBrains.ReSharper.Checker
{
  public static class ChecksEmitUtil {
    public static bool IsNullableType([NotNull] TypeReference reference) {
      if (reference.IsValueType) return false;
      if (reference.IsFunctionPointer) return false;
      if (reference.IsPointer) return true;
      if (reference.IsArray) return true;

      if (reference.IsByReference) {
        return IsNullableType(reference.GetElementType());
      }

      // todo: generic type

      if (reference.FullName == "System.Void") {
        return false;
      }

      return true;
      // todo: void
    }

    [NotNull] static Instruction LoadArgument([NotNull] ParameterDefinition parameter) {
      var parameterIndex = parameter.Index;
      var methodDefinition = (MethodDefinition) parameter.Method;

      if (parameterIndex == -1 && methodDefinition.Body.ThisParameter == parameter) {
        parameterIndex = 0;
      } else if (methodDefinition.HasThis) {
        ++parameterIndex;
      }

      switch (parameterIndex) {
        case 0: return Instruction.Create(OpCodes.Ldarg_0);
        case 1: return Instruction.Create(OpCodes.Ldarg_1);
        case 2: return Instruction.Create(OpCodes.Ldarg_2);
        case 3: return Instruction.Create(OpCodes.Ldarg_3);
        default:
          return (parameterIndex < 256)
            ? Instruction.Create(OpCodes.Ldarg_S, (byte) parameterIndex)
            : Instruction.Create(OpCodes.Ldarg, parameterIndex);
      }
    }

    [NotNull] public static Instruction[] EmitNullCheckInstructions(
      [CanBeNull] ParameterDefinition parameterToCheck, [NotNull] MethodReference constructorReference,
      [NotNull] Instruction target, [NotNull] string paramName, [NotNull] string message) {

      var instructions = new List<Instruction>();

      if (parameterToCheck != null) {
        // TODO: support for unbounded generics!

        instructions.Add(LoadArgument(parameterToCheck));

        if (parameterToCheck.ParameterType.IsByReference) {
          instructions.Add(Instruction.Create(OpCodes.Ldind_Ref));
        }
      }

      instructions.Add(Instruction.Create(OpCodes.Brtrue_S, target));
      instructions.Add(Instruction.Create(OpCodes.Ldstr, paramName));
      instructions.Add(Instruction.Create(OpCodes.Ldstr, message));
      instructions.Add(Instruction.Create(OpCodes.Newobj, constructorReference));
      instructions.Add(Instruction.Create(OpCodes.Throw));

      return instructions.ToArray();
    }
  }
}
