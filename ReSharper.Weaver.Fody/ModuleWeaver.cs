using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ReSharper.Weaver.Fody {
  public sealed class ModuleWeaver {
    [NotNull] private readonly ModuleDefinition myModuleDefinition;
    [NotNull] private readonly IAssemblyResolver myAssemblyResolver;

    public ModuleWeaver([NotNull] ModuleDefinition moduleDefinition,
                        [NotNull] IAssemblyResolver assemblyResolver) {
      myModuleDefinition = moduleDefinition;
      myAssemblyResolver = assemblyResolver;
    }

    //public bool EmitDeclarationSideChecks { get; set; }
    //public bool EmitUsageSideChecks { get; set; }

    public void Execute() {
      var notNullAttributes = NotNullAttributeUtil.FindAttributes(myModuleDefinition);
      if (notNullAttributes.Count == 0) return;

      var argumentNullCtor = ArgumentNullExceptionUtil.FindConstructor(myModuleDefinition);
      if (argumentNullCtor == null) return;

      //Expression<Action<string, string>> expr = (a, b) => new ArgumentNullException(a, b);

      foreach (var typeDefinition in myModuleDefinition.GetTypes()) {
        foreach (var methodDefinition in typeDefinition.Methods) {
          if (methodDefinition.HasBody) {
            EmitParametersCheck(methodDefinition, notNullAttributes, argumentNullCtor);
          }
        }
      }

    }

    [NotNull] public static Instruction[] EmitNullCheckInstruction(
      ParameterDefinition parameterToCheck, [NotNull] MethodReference constructorReference,
      [NotNull] Instruction target, [NotNull] string paramName, [NotNull] string message) {

      var instructions = new List<Instruction>();
      instructions.Add(Instruction.Create(OpCodes.Ldarg, parameterToCheck));

      if (parameterToCheck.ParameterType.IsByReference) {
        instructions.Add(Instruction.Create(OpCodes.Ldind_Ref));
      }

      instructions.Add(Instruction.Create(OpCodes.Brtrue, target));
      instructions.Add(Instruction.Create(OpCodes.Ldstr, paramName));
      instructions.Add(Instruction.Create(OpCodes.Ldstr, message));
      instructions.Add(Instruction.Create(OpCodes.Newobj, constructorReference));
      instructions.Add(Instruction.Create(OpCodes.Throw));

      return instructions.ToArray();
    }

    // todo: ref parameters
    private static void EmitParametersCheck([NotNull] MethodDefinition methodDefinition,
                                     [NotNull] HashSet<MetadataToken> notNullAttributes,
                                     [NotNull] MethodReference argumentNullCtor) {
      if (!methodDefinition.HasParameters) return;

      Stack<Instruction[]> checks = null;
      Instruction firstInstruction = null;

      var parameters = methodDefinition.Parameters; // walk parameters in reverse order
      for (var index1 = parameters.Count - 1; index1 >= 0; index1--) {
        var parameterDefinition = parameters[index1];
        if (parameterDefinition.HasCustomAttributes) {
          foreach (var customAttribute in parameterDefinition.CustomAttributes) {
            var metadataToken = customAttribute.AttributeType.MetadataToken;
            if (notNullAttributes.Contains(metadataToken)) {

              // todo: check parameter type is nullable/can be over brtrue
              // todo: check parameter type is byref type, check ref-parameter
              var parameterType = parameterDefinition.ParameterType;

              // init target of null check brtrue jump
              if (firstInstruction == null) {
                var instrictions = methodDefinition.Body.Instructions;
                if (instrictions.Count == 0) return;

                firstInstruction = instrictions[0];
              }

              var message = string.Format(
                "[NotNull] contract violation in method {0}",
                methodDefinition.GetXmlDocId());

              var nullCheckInstructions = EmitNullCheckInstruction(
                parameterDefinition, argumentNullCtor,
                firstInstruction, parameterDefinition.Name,
                message); // xmldoc?

              checks = checks ?? new Stack<Instruction[]>();
              checks.Push(nullCheckInstructions);
              firstInstruction = nullCheckInstructions[0];
            }
          }
        }
      }

      if (checks != null) {
        var instructions = methodDefinition.Body.Instructions;
        var oldBody = instructions.ToArray();

        instructions.Clear();

        while (checks.Count > 0)
          foreach (var instruction in checks.Pop())
            instructions.Add(instruction);

        foreach (var instruction in oldBody)
          instructions.Add(instruction);

        methodDefinition.Body.OptimizeMacros(); // :O
      }
    }
  }
}