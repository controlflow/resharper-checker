using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Cecil;

namespace JetBrains.ReSharper.Checker {
  // todo: huge problem - annotations tokens are per-module
  // todo: generalize

  public sealed class NotNullAttributeHelper {
    [NotNull] private readonly HashSet<MetadataToken> myNotNullTokens;
    [NotNull] private static readonly string NotNullShortName = typeof(NotNullAttribute).Name;
    [NotNull] private static readonly string SysAttributeFqn = typeof(Attribute).FullName;

    private NotNullAttributeHelper([NotNull] HashSet<MetadataToken> notNullTokens) {
      myNotNullTokens = notNullTokens;
    }

    public bool NotNullAttributeFound {
      get { return myNotNullTokens.Count > 0; }
    }

    public bool IsNotNullAttribute(MetadataToken token) {
      return myNotNullTokens.Contains(token);
    }

    public bool IsNotNullAnnotated([NotNull] ICustomAttributeProvider provider) {
      if (provider.HasCustomAttributes) {
        foreach (var attribute in provider.CustomAttributes) {
          var metadataToken = attribute.AttributeType.MetadataToken;
          if (IsNotNullAttribute(metadataToken)) return true;
        }
      }

      return false;
    }

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

    [NotNull] public static NotNullAttributeHelper FindAttributes([NotNull] ModuleDefinition module) {
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

      if (module.HasAssemblyReferences) {
        //foreach (var nameReference in module.AssemblyReferences) {
        //  
        //}
      }

      // todo: referenced modules, yeah!
      // todo: pass assembly resolver here, yes

      return new NotNullAttributeHelper(notNullTokens);
    }

    public void CollectFrom([NotNull] MethodDefinition definition, [NotNull] HashSet<int> notNulls) {
      if (definition.HasParameters) {
        foreach (var parameter in definition.Parameters)
          if (IsNotNullAnnotated(parameter)) notNulls.Add(parameter.Index);
      }

      if (!definition.IsConstructor) {
        if (IsNotNullAnnotated(definition) ||
            IsNotNullAnnotated(definition.MethodReturnType)) {
          notNulls.Add(-1); // return value
        }
      }

      if (definition.IsVirtual) {
        var declaringType = definition.DeclaringType;
        if (declaringType != null) { // may be global method, yeah

          if (!definition.IsNewSlot) { // do not look for overriden methods
            var baseReference = declaringType.BaseType;
            if (baseReference != null) {
              var baseDefinition = baseReference.Resolve(); // <--
              if (baseDefinition != null) {
                var baseMethod = MetadataResolver.GetMethod(baseDefinition.Methods, definition);
                if (baseMethod != null) CollectFrom(baseMethod, notNulls);
              }
            }
          }

          if (definition.HasOverrides) {
            foreach (var overridenReference in definition.Overrides) {
              var overridenDefinition = overridenReference.Resolve(); // <--
              if (overridenDefinition != null) CollectFrom(overridenDefinition, notNulls);
            }
          } else {
            foreach (var interfaceReference in declaringType.Interfaces) {
              var interfaceDefinition = interfaceReference.Resolve(); // <--
              if (interfaceDefinition != null) {
                var interfaceMethod = MetadataResolver.GetMethod(interfaceDefinition.Methods, definition);
                if (interfaceMethod != null) CollectFrom(interfaceMethod, notNulls);
              }
            }
          }
        }
      }
    }
  }
}