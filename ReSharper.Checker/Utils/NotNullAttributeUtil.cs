using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Checker.Properties;
using Mono.Cecil;

namespace JetBrains.ReSharper.Checker {
  public class NotNullAttributeUtil {
    [NotNull] private static readonly string NotNullShortName = typeof(NotNullAttribute).Name;
    [NotNull] private static readonly string SysAttributeFqn = typeof(Attribute).FullName;

    public static bool IsAttribute([NotNull] TypeReference reference) {
      if (reference.Name != NotNullShortName) return false;

      if (reference.IsValueType)       return false;
      if (reference.IsArray)           return false;
      if (reference.IsGenericInstance) return false;
      if (reference.IsPointer)         return false;
      if (reference.IsNested)          return false;
      if (reference.IsByReference)     return false;

      var definition = reference.Resolve();
      if (definition != null) {
        if (definition.HasGenericParameters) return false;
        if (definition.IsAbstract)           return false;
        if (!definition.IsClass)             return false;

        var baseReference = definition.BaseType;
        if (baseReference == null)                     return false;
        if (baseReference.FullName != SysAttributeFqn) return false;
      }

      return true;
    }

    [NotNull]
    public static HashSet<MetadataToken> FindAttributes([NotNull] ModuleDefinition module) {
      var notNullTokens = new HashSet<MetadataToken>();

      foreach (var reference in module.GetTypeReferences()) {
        if (IsAttribute(reference)) {
          notNullTokens.Add(reference.MetadataToken);
        }
      }

      foreach (var definition in module.GetTypes()) {
        if (IsAttribute(definition)) {
          notNullTokens.Add(definition.MetadataToken);
        }
      }

      return notNullTokens;
    }
  }
}