using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace UniAutoRef
{
    [Generator]
    public class UniAutoRefGenerator : IIncrementalGenerator
    {
        // Records.

        // For Field
        public record FieldData(string Name, string Type, bool IsDebugEnabled, string FindIn);

        // For Methods
        public record MethodModel(string MethodName,
            string ClassName,
            string NamespaceName,
            System.Collections.Immutable.ImmutableArray<FieldData> FieldsToFill
        );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<MethodModel> methodModels = context.SyntaxProvider
                .CreateSyntaxProvider(
                predicate: IsTargetMethod,
                transform: TransformMethod
                ).Where(model => model != null)
                .Select((model, _) => model!);

            IncrementalValueProvider<ImmutableArray<MethodModel>> collectedMethods = methodModels.Collect();

            context.RegisterSourceOutput(collectedMethods,
                (spc, methods) => Execute(methods, spc));
        }

        private static bool IsTargetMethod(SyntaxNode node, CancellationToken _) =>
        node is MethodDeclarationSyntax method &&
            (method.Identifier.ValueText == "GeneratedAwake") &&
             method.Modifiers.Any(SyntaxKind.PartialKeyword);

        private static MethodModel? TransformMethod(GeneratorSyntaxContext ctx, CancellationToken _)
        {
            var methodSyntax = (MethodDeclarationSyntax)ctx.Node;

            var methodSymbol = ctx.SemanticModel.GetDeclaredSymbol(methodSyntax);
            if (methodSymbol == null) return null;

            var fieldsBuilder = System.Collections.Immutable.ImmutableArray.CreateBuilder<FieldData>();
            var classSymbol = methodSymbol.ContainingType;

            foreach (var member in classSymbol.GetMembers())
            {
                if (member is IFieldSymbol fieldSymbol)
                {
                    var autoFindAttribute = fieldSymbol.GetAttributes().FirstOrDefault(attr =>
                        attr.AttributeClass?.Name == "AutoFind" ||
                        attr.AttributeClass?.Name == "AutoFindAttribute");

                    if (autoFindAttribute != null)
                    {
                        bool IsDebugEnabled = false;
                        string? findIn = "";

                        if (!autoFindAttribute.ConstructorArguments.IsEmpty)
                        {
                            // Arguments in attribute

                            var firstArgument = autoFindAttribute.ConstructorArguments[0];
                            var secondArgument = autoFindAttribute.ConstructorArguments[1];

                            // Debug (Enable / Disable) (enum)
                            var firstArgValue = firstArgument.Value?.ToString();

                            // FindIn (enum)
                            var secondArgValue = secondArgument.Value?.ToString();

                            if (firstArgValue == "Enable" || firstArgValue == "1")
                            {
                                IsDebugEnabled = true;
                            }

                            findIn = secondArgValue?.ToString();
                        }

                        string fieldName = fieldSymbol.Name;
                        string fieldType = fieldSymbol.Type.ToDisplayString();

                        fieldsBuilder.Add(new FieldData(fieldName, fieldType, IsDebugEnabled, findIn ?? "0"));
                    }
                }

            }

            return new MethodModel(
                methodSymbol.Name,
                classSymbol.Name,
                classSymbol.ContainingNamespace.ToDisplayString(),
                fieldsBuilder.ToImmutable()
            );

        }

        private static void Execute(ImmutableArray<MethodModel> methods, SourceProductionContext context)
        {
            if (methods.IsEmpty) return;

            foreach (var method in methods)
            {
                bool hasNamespace = !string.IsNullOrEmpty(method.NamespaceName) && method.NamespaceName != "<global namespace>";

                var findStringBuilder = new StringBuilder();

                foreach (var field in method.FieldsToFill)
                {
                    string varName = field.Name;

                    if (field != null)
                    {
                        string componentMethod = field.FindIn switch
                        {
                            "0" => $"GetComponent<{field.Type}>()",
                            "1" => $"GetComponentInChildren<{field.Type}>()",
                            "2" => $"GetComponentInParent<{field.Type}>()",
                            "3" => $"FindFirstObjectByType<{field.Type}>()",
                            _ => $"GetComponent<{field.Type}>()"
                        };

                        findStringBuilder.AppendLine($"\t{varName} = {componentMethod};");

                        // Argument checks here

                        if (field.IsDebugEnabled)
                        {
                            findStringBuilder.AppendLine($"#if UNITY_EDITOR || DEBUG\n if ({varName} == null) Debug.Log(\"[AutoFind] The {varName} in {method.ClassName} is not found.\");\n#endif");
                        }

                    }

                    var code = new StringBuilder($@"
using UnityEngine;

public partial class {method.ClassName}
{{
    partial void GeneratedAwake()
    {{
{findStringBuilder}
    }}
}}
");

                    context.AddSource($"{method.ClassName}_generatedLog.g.cs", SourceText.From(code.ToString(), Encoding.UTF8));

                }
            }
        }
    }
}
