using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UniAutoRef.Editor
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UARAnalyzer : DiagnosticAnalyzer
    {
        public const string PartialDiagnosticId = "UAR010";
        public const string GeneratedAwakeDiagnosticId = "UAR011";
        public const string GAwakeNotCalledDiagnosticId = "UAR012";

        private static readonly DiagnosticDescriptor PartialModifierRule = new(
            id: PartialDiagnosticId,
            title: "Class must be partial",
            messageFormat: "Class '{0}' uses [AutoFind] but is not marked as partial",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor GeneratedAwakeModifierRule = new(
            id: GeneratedAwakeDiagnosticId,
            title: "Class must have method \"GeneratedAwake\"",
            messageFormat: "Class '{0}' need to have \"GeneratedAwake\" method for using \"[AutoFind]\"",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor GAwakeNotCalledModifierRule = new(
            id: GAwakeNotCalledDiagnosticId,
            title: "Method \"GeneratedAwake\" need to be called (in \"Awake\")",
            messageFormat: "Class `{0}` need to call method \"GeneratedAwake\" (in \"Awake\")",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(PartialModifierRule, GeneratedAwakeModifierRule, GAwakeNotCalledModifierRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            bool hasFieldsWithAutoFind = classDeclaration.Members
                .OfType<FieldDeclarationSyntax>()
                .Any(field => field.AttributeLists
                    .SelectMany(attrList => attrList.Attributes)
                    .Any(attr => {
                        var name = attr.Name.ToString();
                        return name == "AutoFind" ||
                            name == "AutoFindAttribute" ||
                            name.EndsWith(".AutoFind") ||
                            name.EndsWith(".AutoFindAttribute");
            }));

            if (!hasFieldsWithAutoFind) return;

            bool isPartial = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

            if (!isPartial)
            {
                var partialDiagnostic = Diagnostic.Create(
                    PartialModifierRule,
                    classDeclaration.Identifier.GetLocation(),
                    classDeclaration.Identifier.Text
                );

                context.ReportDiagnostic(partialDiagnostic);
            }

            bool hasGeneratedAwake = classDeclaration.Members
                .OfType<MethodDeclarationSyntax>()
                .Any(m => m.Identifier.Text == "GeneratedAwake");

            if (!hasGeneratedAwake)
            {
                var generatedAwakeDiagnostic = Diagnostic.Create(
                    GeneratedAwakeModifierRule,
                    classDeclaration.Identifier.GetLocation(),
                    classDeclaration.Identifier.Text
                );
                context.ReportDiagnostic(generatedAwakeDiagnostic);
            }

            var methodInvocations = classDeclaration.DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            bool isMethodCalled = methodInvocations.Any(invocation =>
            {
                if (invocation.Expression is IdentifierNameSyntax identifierName)
                {
                    return identifierName.Identifier.Text == "GeneratedAwake";
                }
                else if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    return memberAccess.Name.Identifier.Text == "GeneratedAwake";
                }

                return false;
            });

            if (!isMethodCalled)
            {
                var gAwakeNotCalledDiagnostic = Diagnostic.Create(
                    GAwakeNotCalledModifierRule,
                    classDeclaration.Identifier.GetLocation(),
                    classDeclaration.Identifier.Text
                );
                context.ReportDiagnostic(gAwakeNotCalledDiagnostic);
            }

        }


    }
}
