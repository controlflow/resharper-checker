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
      Config = new XElement("ReSharper.Checker", new XAttribute("Mode", "Declarations"));
      LogInfo = Console.WriteLine;
    }

    [NotNull, UsedImplicitly] public ModuleDefinition ModuleDefinition { get; set; }
    [NotNull, UsedImplicitly] public IAssemblyResolver AssemblyResolver { get; set; }
    [NotNull, UsedImplicitly] public XElement Config { get; set; }
    [NotNull, UsedImplicitly] public Action<string> LogInfo { get; set; }

    [NotNull] private AttributeHelper Attributes { get; set; }
    [NotNull] private MethodReference ArgumentNullCtor { get; set; }


    public void Execute() {
      LogInfo("DEBUG");
      LogInfo(Config.ToString());

      Attributes = AttributeHelper.Create(ModuleDefinition);
      if (!Attributes.AnyAttributesFound) return;

      var argumentNullCtor = ArgumentNullExceptionUtil.FindConstructor(ModuleDefinition);
      if (argumentNullCtor == null) return;

      ArgumentNullCtor = argumentNullCtor;
      var notNulls = new HashSet<int>();

      foreach (var typeDefinition in ModuleDefinition.GetTypes()) {
        // todo: check OnlyPublicTypes flag
        

        foreach (var methodDefinition in typeDefinition.Methods) {
          //if (methodDefinition.Name != "BuggyMethod") {
          //  continue;
          //}

          if (methodDefinition.HasBody) {
            Attributes.CollectFrom(methodDefinition, notNulls);
            if (notNulls.Count > 0) {
              EmitParametersCheck(methodDefinition, notNulls);
              notNulls.Clear();
            }
          }
        }
      }

    }

    private void EmitParametersCheck(
      [NotNull] MethodDefinition methodDefinition, [NotNull] HashSet<int> notNulls) {

      Stack<Instruction[]> inputСhecks = null;
      Queue<Instruction[]> outputChecks = null;
      Instruction firstInstruction = null, lastInstruction = null;

      var parameters = methodDefinition.Parameters; // walk parameters in reverse order
      for (var index1 = parameters.Count - 1; index1 >= 0; index1--) {
        var parameterDefinition = parameters[index1];
        if (notNulls.Contains(parameterDefinition.Index)) {

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
              parameterDefinition, parameterType, ArgumentNullCtor, firstInstruction,
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
              parameterDefinition, parameterType, ArgumentNullCtor, target,
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

      if (notNulls.Contains(-1) &&
          ChecksEmitUtil.IsNullableType(returnType.ReturnType) &&
          !returnType.ReturnType.IsByReference /* ugh! */) {

        lastInstruction = lastInstruction ?? Instruction.Create(OpCodes.Ret);
        var target = (outputChecks != null) ? outputChecks.Peek()[0] : lastInstruction;

        var checkInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
          null, returnType.ReturnType, ArgumentNullCtor,
          target, "$return", "[NotNull] ensures contract violation");

        outputChecks = outputChecks ?? new Queue<Instruction[]>();
        outputChecks.Enqueue(checkInstructions);
        emitReturnValueCheck = true;
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