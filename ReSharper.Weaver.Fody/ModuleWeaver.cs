using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ReSharper.Weaver.Fody
{
  public sealed class ModuleWeaver
  {
    private readonly ModuleDefinition myModuleDefinition;
    private readonly IAssemblyResolver myAssemblyResolver;

    public ModuleWeaver(ModuleDefinition moduleDefinition, IAssemblyResolver assemblyResolver)
    {
      myModuleDefinition = moduleDefinition;
      myAssemblyResolver = assemblyResolver;
    }

    public void Execute()
    {
      var consoleDef = myModuleDefinition.Import(typeof(Console)).Resolve();
      var writeLine = myModuleDefinition.Import(consoleDef.Methods.First(m =>
        !m.IsConstructor && m.Name == "WriteLine" && m.Parameters.Count == 1 &&
        m.Parameters[0].ParameterType.FullName == typeof(String).FullName));

      foreach (var typeDefinition in myModuleDefinition.GetTypes())
      {
        foreach (var methodDefinition in typeDefinition.Methods)
        {
          if (methodDefinition.HasBody && !methodDefinition.IsConstructor)
          {
            var methodBody = methodDefinition.Body;

            var str = Instruction.Create(OpCodes.Ldstr, "REWRITE LOL");
            var wl = Instruction.Create(OpCodes.Call, writeLine);

            methodBody.Instructions.Insert(0, str);
            methodBody.Instructions.Insert(1, wl);

            
          }
        }
      }
    }
  }
}