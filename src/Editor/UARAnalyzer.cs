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
        // Use Unshipped and Shipped files.
        public const string PartialDiagnosticId = "UAR010";

        private static readonly DiagnosticDescriptor PartialModifierRule = new(
            id: PartialDiagnosticId,
            title: "Class must be partial",
            messageFormat: "Class '{0}' uses [AutoFind] but is not marked as partial",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(PartialModifierRule);

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
                        return name == "AutoRef" ||
                            name == "AutoRefAttribute" ||
                            name.EndsWith(".AutoRef") ||
                            name.EndsWith(".AutoRefAttribute");
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

        }


    }
}
