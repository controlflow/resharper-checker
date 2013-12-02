using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mono.Cecil;

namespace JetBrains.ReSharper.Checker {
  public sealed class AttributeHelper {
    [NotNull] private readonly Dictionary<ModuleDefinition, HashSet<MetadataToken>> myTokens;
    [NotNull] private static readonly string NotNullShortName = typeof(NotNullAttribute).Name;
    [NotNull] private static readonly string SysAttributeFqn = typeof(Attribute).FullName;

    private AttributeHelper() {
      myTokens = new Dictionary<ModuleDefinition, HashSet<MetadataToken>>();
    }

    public bool AnyAttributesFound {
      get { return myTokens.Count > 0; }
    }

    public bool IsNotNullAnnotated(
      [NotNull] ICustomAttributeProvider provider, [NotNull] HashSet<MetadataToken> tokens) {

      if (provider.HasCustomAttributes) {
        foreach (var attribute in provider.CustomAttributes) {
          var metadataToken = attribute.AttributeType.MetadataToken;
          if (tokens.Contains(metadataToken)) return true;
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

    [CanBeNull] private static HashSet<MetadataToken> InspectModule([NotNull] ModuleDefinition module) {
      HashSet<MetadataToken> tokens = null;

      foreach (var reference in module.GetTypeReferences()) {
        if (IsAttribute(reference)) {
          tokens = tokens ?? new HashSet<MetadataToken>();
          tokens.Add(reference.MetadataToken);
        }
      }

      foreach (var definition in module.GetTypes()) {
        if (IsAttribute(definition)) {
          tokens = tokens ?? new HashSet<MetadataToken>();
          tokens.Add(definition.MetadataToken);
        }
      }

      return tokens;
    }

    [NotNull] public static AttributeHelper Create([NotNull] ModuleDefinition module) {
      var attributes = new AttributeHelper();

      // current module
      {
        var tokens = InspectModule(module);
        if (tokens != null) {
          attributes.myTokens[module] = tokens;
        }
      }

      // and all the referenced modules
      if (module.HasAssemblyReferences) {
        foreach (var assemblyNameReference in module.AssemblyReferences) {
          var assemblyDefinition = module.AssemblyResolver.Resolve(assemblyNameReference);
          if (assemblyDefinition == null) continue;

          foreach (var referencedModule in assemblyDefinition.Modules) {
            if (attributes.myTokens.ContainsKey(referencedModule)) continue;

            var tokens = InspectModule(referencedModule);
            if (tokens != null) {
              attributes.myTokens[referencedModule] = tokens;
            }
          }
        }
      }

      return attributes;
    }

    public void CollectFrom([NotNull] MethodDefinition definition, [NotNull] HashSet<int> annotated) {
      HashSet<MetadataToken> tokens;
      if (!myTokens.TryGetValue(definition.Module, out tokens)) return;

      if (definition.HasParameters) {
        foreach (var parameter in definition.Parameters) {
          if (IsNotNullAnnotated(parameter, tokens)) {
            annotated.Add(parameter.Index);
          }
        }
      }

      if (!definition.IsConstructor) {
        if (IsNotNullAnnotated(definition, tokens) ||
            IsNotNullAnnotated(definition.MethodReturnType, tokens)) {
          annotated.Add(-1); // return value
        }
      }

      if (definition.IsVirtual) {
        var declaringType = definition.DeclaringType;
        if (declaringType == null) return; // may be global method, yeah

        if (!definition.IsNewSlot) { // do not look for overriden methods
          var baseReference = declaringType.BaseType;
          if (baseReference != null) {
            var baseDefinition = baseReference.Resolve();
            if (baseDefinition != null) {
              var baseMethod = MetadataResolver.GetMethod(baseDefinition.Methods, definition);
              if (baseMethod != null) CollectFrom(baseMethod, annotated);
            }
          }
        }

        if (definition.HasOverrides) {
          foreach (var overridenReference in definition.Overrides) {
            var overridenDefinition = overridenReference.Resolve();
            if (overridenDefinition == null) continue;

            CollectFrom(overridenDefinition, annotated);
          }
        } else { // implicit interface implementations
          foreach (var interfaceReference in declaringType.Interfaces) {
            var interfaceDefinition = interfaceReference.Resolve();
            if (interfaceDefinition == null) continue;

            var interfaceMethod = MetadataResolver.GetMethod(interfaceDefinition.Methods, definition);
            if (interfaceMethod == null) continue;

            CollectFrom(interfaceMethod, annotated);
          }
        }
      }
    }
  }
}