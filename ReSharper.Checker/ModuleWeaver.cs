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

        // collect [NotNull] fields, .initonly and not
        //foreach (var fieldDefinition in typeDefinition.Fields) {
        //  
        //}

        // if any
        // inspect ctors for delegating calls
        // if no such calls - emit checks at every ctor end
        // if there is - emit read checks for some fielfs

        // for all the write access - emit write checks

        // todo: base fields read/write checks (always?)

        foreach (var methodDefinition in typeDefinition.Methods) {
          //if (methodDefinition.Name != "BuggyMethod") {
          //  continue;
          //}

          if (methodDefinition.HasBody) {
            Attributes.CollectFrom(methodDefinition, notNulls);
            if (notNulls.Count > 0) {
              // todo: ability to turn on/off
              EmitMethodEnterChecks(methodDefinition, notNulls);
              EmitMethodExitChecks(methodDefinition, notNulls);
              notNulls.Clear();
            }
          }
        }
      }
    }

    private void EmitMethodEnterChecks(
      [NotNull] MethodDefinition methodDefinition, [NotNull] HashSet<int> positions) {

      var inputСhecks = new Stack<Instruction[]>();
      Instruction firstInstruction = null;

      // walk over parameters in reverse order to build checks stack
      var parameters = methodDefinition.Parameters;
      for (var index1 = parameters.Count - 1; index1 >= 0; index1--) {
        var parameterDefinition = parameters[index1];
        if (parameterDefinition.IsOut) continue;

        if (!positions.Contains(parameterDefinition.Index)) continue;

        var parameterType = parameterDefinition.ParameterType;
        if (!ChecksEmitUtil.IsNullableType(parameterType)) {
          LogInfo(string.Format(
            "Invalid annotation usage in member {0} at parameter '{1}'.",
            methodDefinition.GetXmlDocId(), parameterDefinition.Name));
          continue;
        }

        if (firstInstruction == null) {
          var instrictions = methodDefinition.Body.Instructions;
          if (instrictions.Count == 0) return; // shit happens
          firstInstruction = instrictions[0];
        }

        var checkInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
          parameterDefinition, parameterType, ArgumentNullCtor, firstInstruction,
          parameterDefinition.Name, "[NotNull] requirement violation");

        inputСhecks.Push(checkInstructions);
        firstInstruction = checkInstructions[0];
      }

      if (inputСhecks.Count == 0) return;

      var instructions = methodDefinition.Body.Instructions;
      var oldBody = instructions.ToArray();
      instructions.Clear();

      while (inputСhecks.Count > 0)
        foreach (var instruction in inputСhecks.Pop())
          instructions.Add(instruction);

      foreach (var instruction in oldBody)
        instructions.Add(instruction);
    }

    private void EmitMethodExitChecks(
      [NotNull] MethodDefinition methodDefinition, [NotNull] HashSet<int> positions) {

      var outputChecks = new Queue<Instruction[]>();
      Instruction lastInstruction = null;

      // walk over parameters in reverse order to build checks stack
      var parameters = methodDefinition.Parameters;
      for (var index1 = parameters.Count - 1; index1 >= 0; index1--) {
        var parameterDefinition = parameters[index1];
        if (!positions.Contains(parameterDefinition.Index)) continue;

        var parameterType = parameterDefinition.ParameterType;
        if (parameterType.IsByReference) {
          if (!ChecksEmitUtil.IsNullableType(parameterType)) {
            LogInfo(string.Format(
              "Invalid annotation usage in member {0} at parameter '{1}'.",
              methodDefinition.GetXmlDocId(), parameterDefinition.Name));
            continue;
          }

          lastInstruction = lastInstruction ?? Instruction.Create(OpCodes.Ret);
          var target = (outputChecks.Count > 0) ? outputChecks.Peek()[0] : lastInstruction;

          var checkInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
            parameterDefinition, parameterType, ArgumentNullCtor, target,
            parameterDefinition.Name, "[NotNull] ensures violation");

          outputChecks.Enqueue(checkInstructions);
        }
      }

      var returnType = methodDefinition.MethodReturnType;
      var checkReturnValue = false;

      if (positions.Contains(-1)) { // check annotated return
        if (ChecksEmitUtil.IsNullableType(returnType.ReturnType) && !returnType.ReturnType.IsByReference) {
          lastInstruction = lastInstruction ?? Instruction.Create(OpCodes.Ret);
          var target = (outputChecks.Count > 0) ? outputChecks.Peek()[0] : lastInstruction;

          var checkInstructions = ChecksEmitUtil.EmitNullCheckInstructions(
            null, returnType.ReturnType, ArgumentNullCtor, target,
            "$return$", "Return value [NotNull] contract violation");

          outputChecks.Enqueue(checkInstructions);
          checkReturnValue = true;
        } else {
          LogInfo(string.Format(
            "Invalid annotation usage in member {0} at return value.", methodDefinition.GetXmlDocId()));
        }
      }


      if (outputChecks.Count == 0) return;

      var instructions = methodDefinition.Body.Instructions;
      var retInstruction = instructions[instructions.Count - 1];

      // replace ret with nop or dup (if return value check required)
      retInstruction.OpCode = checkReturnValue ? OpCodes.Dup : OpCodes.Nop;
      retInstruction.Operand = null;

      foreach (var instruction in outputChecks.Dequeue())
        instructions.Add(instruction);

      instructions.Add(lastInstruction);
    }
  }
}