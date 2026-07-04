using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Confuser.Analyzers {
	/// <summary>
	///     CX001 — flags <c>Import(...)</c> whose argument is a member resolved via reflection on
	///     the host runtime (<c>someType.GetMethod(...)</c>, <c>GetConstructor</c>, <c>GetField</c>,
	///     <c>GetProperty</c> where the receiver is a <see cref="System.Type" />). Importing a
	///     host-runtime member into a target module produces a reference to the wrong corlib
	///     (e.g. the obfuscator's .NET rather than the target's), which breaks the protected
	///     assembly. Resolve the member through the target module's corlib instead.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class HostRuntimeImportAnalyzer : DiagnosticAnalyzer {
		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticIds.HostRuntimeImport,
			"Importing a member resolved via host reflection",
			"Import() argument is resolved with Type.{0}() on the host runtime; resolve the member through the target module's corlib to avoid a wrong-corlib reference",
			DiagnosticIds.Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Importing a System.Reflection member (from Type.GetMethod/GetConstructor/GetField/" +
				"GetProperty) into a dnlib module references the obfuscator's runtime corlib rather than the " +
				"target's, producing a broken assembly. Resolve the member through the target module's corlib.");

		static readonly ImmutableHashSet<string> ReflectionGetters =
			ImmutableHashSet.Create("GetMethod", "GetConstructor", "GetField", "GetProperty");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		static void Analyze(SyntaxNodeAnalysisContext context) {
			var invocation = (InvocationExpressionSyntax)context.Node;
			if (invocation.Expression is not MemberAccessExpressionSyntax member ||
				member.Name.Identifier.ValueText != "Import")
				return;

			foreach (var argument in invocation.ArgumentList.Arguments) {
				if (argument.Expression is not InvocationExpressionSyntax inner ||
					inner.Expression is not MemberAccessExpressionSyntax innerMember)
					continue;

				var getterName = innerMember.Name.Identifier.ValueText;
				if (!ReflectionGetters.Contains(getterName))
					continue;

				if (context.SemanticModel.GetSymbolInfo(inner, context.CancellationToken).Symbol is not IMethodSymbol getter ||
					getter.ContainingType?.ToDisplayString() != "System.Type")
					continue;

				context.ReportDiagnostic(Diagnostic.Create(Rule, innerMember.Name.GetLocation(), getterName));
				return;
			}
		}
	}
}
