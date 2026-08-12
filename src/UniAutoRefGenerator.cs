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
        public record FieldData(string Name, string Type, bool IsDebugEnabled);

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

                        if (!autoFindAttribute.ConstructorArguments.IsEmpty)
                        {
                            var firstArgument = autoFindAttribute.ConstructorArguments[0];
                            var argValue = firstArgument.Value?.ToString();

                            if (argValue == "Enable" || argValue == "1")
                            {
                                IsDebugEnabled = true;
                            }
                        }

                        string fieldName = fieldSymbol.Name;
                        string fieldType = fieldSymbol.Type.ToDisplayString();

                        fieldsBuilder.Add(new FieldData(fieldName, fieldType, IsDebugEnabled));
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
                        string debugText = "";

                        if (field.IsDebugEnabled)
                        {
                            debugText = $"#if UNITY_EDITOR || DEBUG\n if ({varName} == null) Debug.Log(\"[AutoFind] The {varName} in {method.ClassName} is not found.\");\n#endif";
                        }

                        findStringBuilder.AppendLine($"        {varName} = GetComponent<{field.Type}>();");
                        if (!string.IsNullOrEmpty(debugText))
                        {
                            findStringBuilder.AppendLine(debugText);
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
