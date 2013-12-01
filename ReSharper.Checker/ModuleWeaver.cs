using System;
using System.Collections.Generic;
using System.Xml.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace JetBrains.ReSharper.Checker {
  [UsedImplicitly]
  public sealed class ModuleWeaver {
    

    public ModuleWeaver() {
      Config = new XElement("Foo");
      LogInfo = Console.WriteLine;
    }

    [NotNull, UsedImplicitly] public ModuleDefinition ModuleDefinition { get; set; }
    [NotNull, UsedImplicitly] public IAssemblyResolver AssemblyResolver { get; set; }
    [NotNull, UsedImplicitly] public XElement Config { get; set; }
    [NotNull, UsedImplicitly] public Action<string> LogInfo { get; set; }

    [NotNull] private HashSet<MetadataToken> NotNullAttributes { get; set; }
    [NotNull] private MethodReference ArgumentNullCtor { get; set; }


    public void Execute() {
      LogInfo("DEBUG");
      LogInfo(Config.ToString());

      NotNullAttributes = NotNullAttributeUtil.FindAttributes(ModuleDefinition);
      if (NotNullAttributes.Count == 0) return;

      var argumentNullCtor = ArgumentNullExceptionUtil.FindConstructor(ModuleDefinition);
      if (argumentNullCtor == null) return;

      ArgumentNullCtor = argumentNullCtor;

      foreach (var typeDefinition in ModuleDefinition.GetTypes()) {
        // todo: check OnlyPublicTypes flag

        foreach (var methodDefinition in typeDefinition.Methods) {


          if (methodDefinition.HasBody) {
            EmitParametersCheck(methodDefinition);
          }
        }
      }

    }

    private bool IsAnnotatedWithNotNull([NotNull] ICustomAttributeProvider provider) {
      if (!provider.HasCustomAttributes) return false;

      foreach (var attribute in provider.CustomAttributes) {
        var metadataToken = attribute.AttributeType.MetadataToken;
        if (NotNullAttributes.Contains(metadataToken)) return true;
      }

      return false;
    }

    private void EmitParametersCheck([NotNull] MethodDefinition methodDefinition) {
      if (!methodDefinition.HasParameters) return;

      Stack<Instruction[]> inputСhecks = null;
      Instruction firstInstruction = null;

      var parameters = methodDefinition.Parameters; // walk parameters in reverse order
      for (var index1 = parameters.Count - 1; index1 >= 0; index1--) {
        var parameterDefinition = parameters[index1];
        if (IsAnnotatedWithNotNull(parameterDefinition)) {
          // todo: check parameter type is nullable/can be over brtrue
          // todo: check parameter type is byref type, check ref-parameter
          var parameterType = parameterDefinition.ParameterType;

          // init target of null check brtrue jump
          if (firstInstruction == null) {
            var instrictions = methodDefinition.Body.Instructions;
            if (instrictions.Count == 0) return;

            firstInstruction = instrictions[0];
          }

          const string message = "[NotNull] contract violation";

          var nullCheckInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
            parameterDefinition, ArgumentNullCtor,
            firstInstruction, parameterDefinition.Name, message);

          inputСhecks = inputСhecks ?? new Stack<Instruction[]>();
          inputСhecks.Push(nullCheckInstructions);

          firstInstruction = nullCheckInstructions[0];
        }
      }

      var instructions = methodDefinition.Body.Instructions;

      if (inputСhecks != null) {
        
        var oldBody = instructions.ToArray();

        instructions.Clear();

        while (inputСhecks.Count > 0)
          foreach (var instruction in inputСhecks.Pop()) instructions.Add(instruction);

        foreach (var instruction in oldBody) instructions.Add(instruction);
      }

      var returnType = methodDefinition.MethodReturnType;
      if (IsAnnotatedWithNotNull(methodDefinition)) {
        var retInstruction = instructions[instructions.Count - 1];

        // replace ret with dup
        retInstruction.OpCode = OpCodes.Dup;
        retInstruction.Operand = null;

        // in
        var newRetInstruction = Instruction.Create(OpCodes.Ret);
        

        var checkInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
          null, ArgumentNullCtor, newRetInstruction, "return value", "[NotNull] contract");

        foreach (var instruction in checkInstructions) {
          instructions.Add(instruction);
        }

        instructions.Add(newRetInstruction);
      }
    }
  }
}