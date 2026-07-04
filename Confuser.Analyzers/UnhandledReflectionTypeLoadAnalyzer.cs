using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Confuser.Analyzers {
	/// <summary>
	///     CX004 — flags <c>Assembly.GetTypes()</c> / <c>Module.GetTypes()</c> that are not guarded
	///     against <see cref="System.Reflection.ReflectionTypeLoadException" />. That exception is
	///     thrown whenever a contained type cannot be loaded (common for plugin assemblies with
	///     unresolved dependencies) and caused packer/plugin startup crashes.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class UnhandledReflectionTypeLoadAnalyzer : DiagnosticAnalyzer {
		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticIds.UnhandledReflectionTypeLoad,
			"GetTypes() without ReflectionTypeLoadException handling",
			"'{0}.GetTypes()' can throw ReflectionTypeLoadException when a contained type is unresolvable; wrap it in a try/catch",
			DiagnosticIds.Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Assembly.GetTypes() and Module.GetTypes() throw ReflectionTypeLoadException when any " +
				"contained type cannot be loaded. Guard the call (and prefer ex.Types on the exception) to avoid " +
				"startup crashes when loading plugin or external assemblies.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		static void Analyze(SyntaxNodeAnalysisContext context) {
			var invocation = (InvocationExpressionSyntax)context.Node;
			if (invocation.Expression is not MemberAccessExpressionSyntax member ||
				member.Name.Identifier.ValueText != "GetTypes")
				return;

			if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method ||
				method.Name != "GetTypes" || !method.Parameters.IsEmpty)
				return;

			var containingType = method.ContainingType?.ToDisplayString();
			if (containingType != "System.Reflection.Assembly" && containingType != "System.Reflection.Module")
				return;

			var rtle = context.Compilation.GetTypeByMetadataName("System.Reflection.ReflectionTypeLoadException");
			if (IsGuarded(invocation, context.SemanticModel, rtle, context.CancellationToken))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, member.Name.GetLocation(), containingType));
		}

		static bool IsGuarded(SyntaxNode node, SemanticModel model, INamedTypeSymbol? rtle, System.Threading.CancellationToken ct) {
			for (var current = node.Parent; current != null; current = current.Parent) {
				if (current is TryStatementSyntax tryStmt &&
					tryStmt.Block.Span.Contains(node.Span) &&
					CatchesReflectionTypeLoad(tryStmt, model, rtle, ct))
					return true;

				// Do not walk past the enclosing method / lambda / local-function boundary.
				if (current is BaseMethodDeclarationSyntax ||
					current is AnonymousFunctionExpressionSyntax ||
					current is LocalFunctionStatementSyntax)
					break;
			}

			return false;
		}

		static bool CatchesReflectionTypeLoad(TryStatementSyntax tryStmt, SemanticModel model, INamedTypeSymbol? rtle,
			System.Threading.CancellationToken ct) {
			foreach (var clause in tryStmt.Catches) {
				// A general 'catch { }' (no declared type) catches everything.
				if (clause.Declaration is null)
					return true;

				var caught = model.GetTypeInfo(clause.Declaration.Type, ct).Type as INamedTypeSymbol;
				if (caught is null)
					continue;

				// The catch guards the call if ReflectionTypeLoadException is assignable to the caught
				// type (i.e. the caught type is RTLE or one of its base types such as SystemException /
				// Exception). If we cannot resolve RTLE, fall back to name matching on the base chain.
				if (rtle is not null) {
					for (var t = rtle; t is not null; t = t.BaseType) {
						if (SymbolEqualityComparer.Default.Equals(t, caught))
							return true;
					}
				}
				else {
					var name = caught.ToDisplayString();
					if (name == "System.Exception" || name == "System.SystemException" ||
						name == "System.Reflection.ReflectionTypeLoadException")
						return true;
				}
			}

			return false;
		}
	}
}
