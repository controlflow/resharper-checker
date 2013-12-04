using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using Mono.Cecil;

namespace JetBrains.ReSharper.Checker {
  public static class XmlDocUtil {
    [NotNull] public static string GetXmlDocId(
      [NotNull] this FieldReference fieldReference, bool shortNameOnly = false) {
      var builder = new StringBuilder();

      if (!shortNameOnly) {
        var ownerName = fieldReference.DeclaringType.FullName;
        builder.Append("F:").Append(ownerName)
               .Replace('+', '.').Append('.');
      }

      return builder.Append(fieldReference.Name).ToString();
    }

    [NotNull] public static string GetXmlDocId(
      [NotNull] this MethodReference methodReference, bool shortNameOnly = false) {

      var builder = new StringBuilder();
      if (!shortNameOnly) {
        var ownerName = methodReference.DeclaringType.FullName;
        builder.Append("M:").Append(ownerName)
               .Replace('+', '.').Append('.');
      }

      builder.Append(methodReference.Name.Replace('.', '#'));

      var typeParametersCount = methodReference.GenericParameters.Count;
      if (typeParametersCount > 0) {
        builder.Append("``").Append(typeParametersCount);
      }

      var parameters = methodReference.Parameters;
      if (parameters.Count > 0) {
        builder.Append('(');

        for (var index = 0; index < parameters.Count; index++) {
          var parameter = parameters[index];
          if (index > 0) builder.Append(',');

          builder.AppendType(parameter.ParameterType, shortNameOnly);
        }

        builder.Append(')');
      }

      return builder.ToString();
    }

    [NotNull] public static string GetXmlDocId(
      [NotNull] this TypeReference type, bool shortNameOnly = false) {

      var builder = new StringBuilder();
      return builder.AppendType(type, shortNameOnly).ToString();
    }

    [NotNull] static StringBuilder AppendType(
      [NotNull] this StringBuilder builder, [NotNull] TypeReference typeReference, bool shortNameOnly = false) {

      if (typeReference.IsPointer) {
        return builder.AppendType(typeReference.GetElementType(), shortNameOnly).Append('*');
      }

      if (typeReference.IsByReference) {
        return builder.AppendType(typeReference.GetElementType(), shortNameOnly).Append('@');
      }

      if (typeReference.IsGenericParameter) {
        var parameter = (GenericParameter) typeReference;
        return builder
          .Append((parameter.Type == GenericParameterType.Method) ? "``" : "`")
          .Append(parameter.Position);
      }

      if (typeReference.IsArray) {
        builder.AppendType(typeReference.GetElementType(), shortNameOnly);

        var arrayType = (ArrayType) typeReference;
        var elementTypeName = arrayType.ElementType.Name;
        var suffix = arrayType.Name.Substring(elementTypeName.Length);

        return builder.Append(suffix);
      }

      builder.Append(shortNameOnly ? typeReference.Name : typeReference.FullName);

      var genericParameters = typeReference.GenericParameters;
      if (genericParameters.Count == 0) return builder;

      // todo: will not work on nested generic types
      var typeArgs = genericParameters.Count.ToString(CultureInfo.InvariantCulture);
      builder.Length -= (typeArgs.Length + 1);

      builder.Append('{');

      for (var index = 0; index < genericParameters.Count; index++) {
        if (index != 0) builder.Append(',');
        builder.AppendType(genericParameters[index], shortNameOnly);
      }

      return builder.Append('}');
    } 
  }
}