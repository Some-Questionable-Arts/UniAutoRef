using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace UniAutoRef.Editor
{
    [Generator]
    public class UniAutoRefGenerator : IIncrementalGenerator
    {
        // const strint (TKey)

        private const string FINDIN_KEY = "2";

        // Records

        // For Field
        /// <summary>
        /// Field Data for field (marked by attribute)
        /// </summary>
        /// <param name="Name">Name of field</param>
        /// <param name="Type">Type of field</param>
        /// <param name="Arguments">Arguments attribute constructor</param>
        /// <param name="ArchitectureType">True - new Architecture [AutoRef],
        /// False - old Architecture [AutoFind]</param>
        public record FieldData(string Name, string Type, Dictionary<string, string?> Arguments, ArchitectureType ArchitectureType);

        // For Methods
        public record MethodModel(
            string ClassName,
            string NamespaceName,
            ImmutableArray<FieldData> FieldsToFill
        );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<MethodModel> methodModels = context.SyntaxProvider
                .CreateSyntaxProvider(
                predicate: IsTargetClass,
                transform: TransformClass
                ).Where(model => model != null)
                .Select((model, _) => model!);

            IncrementalValueProvider<ImmutableArray<MethodModel>> collectedMethods = methodModels.Collect();

            context.RegisterSourceOutput(collectedMethods,
                (spc, methods) => Execute(methods, spc));
        }

        private static bool IsTargetClass(SyntaxNode node, CancellationToken _) =>
             node is ClassDeclarationSyntax classSyntax &&
             classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

        private static MethodModel? TransformClass(GeneratorSyntaxContext ctx, CancellationToken _)
        {
            var classSyntax = (ClassDeclarationSyntax)ctx.Node;

            var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classSyntax);
            if (classSymbol == null) return null;

            var fieldsBuilder = ImmutableArray.CreateBuilder<FieldData>();

            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IFieldSymbol fieldSymbol)
                {
                    string fieldName = fieldSymbol.Name;
                    string fieldType = fieldSymbol.Type.ToDisplayString();

                    // New attribute (Thats all for new Attributes (if i add))
                    var autoRefAttribute = fieldSymbol.GetAttributes().FirstOrDefault(attr =>
                        attr.AttributeClass?.Name == "AutoRef" ||
                        attr.AttributeClass?.Name == "AutoRefAttribute");

                    if (autoRefAttribute != null)
                    {
                        string? findIn = "";

                        if (!autoRefAttribute.ConstructorArguments.IsEmpty)
                        {
                            var firstArgument = autoRefAttribute.ConstructorArguments[0];

                            var firstArgValue = firstArgument.Value?.ToString();

                            findIn = firstArgValue?.ToString();
                        }

                        fieldsBuilder.Add(new FieldData(fieldName, fieldType, new Dictionary<string, string?>
                        {
                            { FINDIN_KEY, findIn }
                        }, ArchitectureType.AutoRef));
                    }
                }

            }
            return new MethodModel(
                classSymbol.Name,
                classSymbol.ContainingNamespace.ToDisplayString(),
                fieldsBuilder.ToImmutable()
            );

        }

        private static void Execute(ImmutableArray<MethodModel> methods, SourceProductionContext context)
        {
            if (methods.IsEmpty) return;

            var autoRefArchitectureMethods = ImmutableArray.CreateBuilder<MethodModel>();

            foreach (var method in methods)
            {
                var autoRefFields = method.FieldsToFill
                    .Where(method => method.ArchitectureType == ArchitectureType.AutoRef)
                    .ToImmutableArray();

                if (!autoRefFields.IsEmpty)
                {
                    autoRefArchitectureMethods.Add(method with { FieldsToFill = autoRefFields });
                }
            }

            ExecuteAutoRef(autoRefArchitectureMethods.ToImmutable(), context);

        }

        // "Execute" for [AutoRef] attribute
        private static void ExecuteAutoRef(ImmutableArray<MethodModel> methods, SourceProductionContext context)
        {
            // Making generated class "UARAutoRefRegistry"

            var registryCode = new StringBuilder();

            registryCode.AppendLine("#if UNITY_EDITOR");
            registryCode.AppendLine("public static partial class AutoRefRegistry");
            registryCode.AppendLine("{");
            registryCode.AppendLine("\tpublic static readonly System.Type[] TargetTypes =");
            registryCode.AppendLine("\t{");

            if (!methods.IsEmpty)
            {
                foreach (var method in methods)
                {
                    bool hasNamespace = !string.IsNullOrEmpty(method.NamespaceName) && method.NamespaceName != "<global namespace>";

                    var code = new StringBuilder();

                    code.AppendLine("// <auto-generated/>\nusing UnityEngine;");

                    if (hasNamespace)
                    {
                        code.AppendLine($"namespace {method.NamespaceName}\n{{\n\t");
                    }

                    code.AppendLine($"public partial class {method.ClassName} : UniAutoRef.IAutoReference");
                    code.AppendLine("{");
                    code.AppendLine("#if UNITY_EDITOR");
                    code.AppendLine("\tpublic void AutoFind_Execute()");
                    code.AppendLine("\t{");

                    foreach (var field in method.FieldsToFill)
                    {
                        if (field != null)
                        {
                            // Argument checks here

                            field.Arguments.TryGetValue(FINDIN_KEY, out var findIn);

                            string componentMethod = findIn switch
                            {
                                "0" => $"GetComponent<{field.Type}>()",
                                "1" => $"GetComponentInChildren<{field.Type}>()",
                                "2" => $"GetComponentInParent<{field.Type}>()",
                                "3" => $"FindFirstObjectByType<{field.Type}>()",
                                _ => $"GetComponent<{field.Type}>()"
                            };

                            code.AppendLine($"\t\t{field.Name} = {componentMethod};");
                            code.AppendLine($"\t\tif ({field.Name} == null) Debug.Log($\"<b><color=#EEF18E>[AutoRef] Cannot find reference to {field.Name} in {method.ClassName} (Instance in hierarchy: {{gameObject.name}})</color></b>\");");

                            registryCode.AppendLine($"\t\ttypeof({method.NamespaceName}.{method.ClassName}),");
                        }
                    }

                    code.AppendLine("\t}");
                    code.AppendLine("}");
                    code.AppendLine("#endif");

                    if (hasNamespace)
                    {
                        code.AppendLine($"}}");
                    }

                    context.AddSource($"{method.ClassName}_Generated.g.cs", SourceText.From(code.ToString(), Encoding.UTF8));
                }
            }

            registryCode.AppendLine("\t};");
            registryCode.AppendLine("}");
            registryCode.AppendLine("#endif");

            context.AddSource("AutoFindRegistry.g.cs", SourceText.From(registryCode.ToString(), Encoding.UTF8));
        }
    }
}
