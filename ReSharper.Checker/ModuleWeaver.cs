using System;
using System.Collections.Generic;
using System.Xml.Linq;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
          //if (methodDefinition.Name != "BuggyMethod") {
          //  continue;
          //}

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

      // todo: inherited attributes!



      return false;
    }

    

    private void EmitParametersCheck([NotNull] MethodDefinition methodDefinition) {
      if (!methodDefinition.HasParameters) return;

      Stack<Instruction[]> inputСhecks = null;
      Queue<Instruction[]> outputChecks = null;
      Instruction firstInstruction = null, lastInstruction = null;

      var parameters = methodDefinition.Parameters; // walk parameters in reverse order
      for (var index1 = parameters.Count - 1; index1 >= 0; index1--) {
        var parameterDefinition = parameters[index1];
        if (IsAnnotatedWithNotNull(parameterDefinition)) {
          // todo: check parameter type is nullable/can be over brtrue
          // todo: check parameter type is byref type, check ref-parameter
          var parameterType = parameterDefinition.ParameterType;
          if (!ChecksEmitUtil.IsNullableType(parameterType))
            continue;

          if (!parameterDefinition.IsOut) {
            // init target of null check brtrue jump
            if (firstInstruction == null) {
              var instrictions = methodDefinition.Body.Instructions;
              if (instrictions.Count == 0) return; // shit happens
              firstInstruction = instrictions[0];
            }

            var nullCheckInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
              parameterDefinition, ArgumentNullCtor, firstInstruction,
              parameterDefinition.Name, "[NotNull] requirement contract violation");

            inputСhecks = inputСhecks ?? new Stack<Instruction[]>();
            inputСhecks.Push(nullCheckInstructions);

            firstInstruction = nullCheckInstructions[0];
          }

          // out parameters
          if (parameterType.IsByReference) {
            lastInstruction = lastInstruction ?? Instruction.Create(OpCodes.Ret);
            var target = (outputChecks != null) ? outputChecks.Peek()[0] : lastInstruction;

            var nullCheckInstructions2 = ChecksEmitUtil.EmitNullCheckInstructions(
              parameterDefinition, ArgumentNullCtor, target,
              parameterDefinition.Name, "[NotNull] ensires contract violation");

            outputChecks = outputChecks ?? new Queue<Instruction[]>();
            outputChecks.Enqueue(nullCheckInstructions2);
          }
        }
      }

      var instructions = methodDefinition.Body.Instructions;

      if (inputСhecks != null) {
        var oldBody = instructions.ToArray();
        instructions.Clear();

        while (inputСhecks.Count > 0)
          foreach (var instruction in inputСhecks.Pop())
            instructions.Add(instruction);

        foreach (var instruction in oldBody)
          instructions.Add(instruction);
      }

      var returnType = methodDefinition.MethodReturnType;
      var emitReturnValueCheck = false;

      if (ChecksEmitUtil.IsNullableType(returnType.ReturnType) && !returnType.ReturnType.IsByReference) {
        if (IsAnnotatedWithNotNull(methodDefinition) || IsAnnotatedWithNotNull(returnType)) {
          lastInstruction = lastInstruction ?? Instruction.Create(OpCodes.Ret);
          var target = (outputChecks != null) ? outputChecks.Peek()[0] : lastInstruction;

          var checkInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
            null, ArgumentNullCtor, target, "$return", "[NotNull] ensures contract violation");

          outputChecks = outputChecks ?? new Queue<Instruction[]>();
          outputChecks.Enqueue(checkInstructions);
          emitReturnValueCheck = true;
        }
      }

      if (lastInstruction != null) {
        var retInstruction = instructions[instructions.Count - 1];

        // replace ret with dup or nop
        retInstruction.OpCode = emitReturnValueCheck ? OpCodes.Dup : OpCodes.Nop;
        retInstruction.Operand = null;

        foreach (var instruction in outputChecks.Dequeue())
          instructions.Add(instruction);

        instructions.Add(lastInstruction);
      }
    }
  }
}