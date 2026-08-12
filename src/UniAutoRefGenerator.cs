using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace UniAutoRef
{
    [Generator]
    public class UniAutoRefGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<MethodDeclarationSyntax> methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsTargetMethod(node),
                transform: (ctx, _) => (MethodDeclarationSyntax)ctx.Node
            );

            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<MethodDeclarationSyntax> Methods)> compilationAndMethods =
               context.CompilationProvider.Combine(methodDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndMethods,
                (spc, source) => Execute(source.Compilation, source.Methods, spc));
        }

        private static bool IsTargetMethod(SyntaxNode node)
        {
            return node is MethodDeclarationSyntax method &&
                   (method.Identifier.ValueText == "GeneratedAwake") &&
                   method.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        private static void Execute(Compilation compilation, ImmutableArray<MethodDeclarationSyntax> methods, SourceProductionContext context)
        {
            if (methods.IsEmpty) return;

            foreach (var method in methods)
            {
                if (!(method.Parent is ClassDeclarationSyntax classDeclaration)) return;

                string className = classDeclaration.Identifier.ValueText;

                var fieldsToFill = classDeclaration.DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field => field.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .Any(attr => attr.Name.ToString() == "AutoFind" || attr.Name.ToString() == "AutoFindAttribute")
                );

                var findStringBuilder = new StringBuilder();
                var semanticModel = compilation.GetSemanticModel(method.SyntaxTree);

                foreach (var field in fieldsToFill)
                {
                    foreach (var variable in field.Declaration.Variables)
                    {
                        string varName = variable.Identifier.ValueText;
                        if (semanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol)
                        {
                            var autoFindAttribute = fieldSymbol.GetAttributes().
                                FirstOrDefault(ad => ad.AttributeClass?.Name == "AutoFind" || ad.AttributeClass?.Name == "AutoFindAttribute");

                            if (autoFindAttribute != null)
                            {
                                string debugText = "";

                                if (!autoFindAttribute.ConstructorArguments.IsEmpty)
                                {
                                    TypedConstant firstArgument = autoFindAttribute.ConstructorArguments[0];
                                    if (firstArgument.Value?.ToString() == "Enable" || firstArgument.Value?.ToString() == "1")
                                    {
                                        debugText = $@"
#if UNITY_EDITOR || DEBUG
    if ({{varName}} == null) Debug.Log(""[AutoFind] The {{varName}} in {{className}} is not found."");
#endif
";

                                    }
                                }
                                string varType = fieldSymbol.Type.Name;

                                findStringBuilder.AppendLine($"        {varName} = GetComponent<{varType}>();");
                                if (!string.IsNullOrEmpty(debugText))
                                {
                                    findStringBuilder.AppendLine(debugText);
                                }
                            }

                        }
                    }

                    var code = new StringBuilder($@"
using UnityEngine;

public partial class {className}
{{
    partial void GeneratedAwake()
    {{
{findStringBuilder}
    }}
}}
");

                    context.AddSource($"{className}_generatedLog.g.cs", SourceText.From(code.ToString(), Encoding.UTF8));

                }
            }
        }
    }
}
