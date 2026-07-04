using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Confuser.Analyzers {
	/// <summary>
	///     CX003 — an awareness/audit rule that surfaces every call to a dnlib <c>Resolve…Throw</c>
	///     helper (<c>ResolveThrow</c>, <c>ResolveTypeDefThrow</c>, <c>ResolveMethodDefThrow</c>,
	///     <c>ResolveFieldThrow</c>). These throw when a reference cannot be resolved, which crashes
	///     obfuscation on assemblies with external/unresolvable members. Most uses are intentional;
	///     this reports at <see cref="DiagnosticSeverity.Info" /> so the spots can be evaluated
	///     without adding build noise.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class ResolveThrowAuditAnalyzer : DiagnosticAnalyzer {
		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticIds.ResolveThrowAudit,
			"Throwing resolve helper used",
			"'{0}' throws when resolution fails; confirm the target is always resolvable or prefer the non-throwing overload with a null check",
			DiagnosticIds.Category,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: "dnlib's Resolve...Throw helpers throw when a type/member reference cannot be " +
				"resolved. This surfaces each usage for review; external or unresolvable references should " +
				"use the non-throwing Resolve... overload with a null check instead.");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		static void Analyze(SyntaxNodeAnalysisContext context) {
			var invocation = (InvocationExpressionSyntax)context.Node;
			if (invocation.Expression is not MemberAccessExpressionSyntax member)
				return;

			var name = member.Name.Identifier.ValueText;
			if (!IsResolveThrowName(name))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, member.Name.GetLocation(), name));
		}

		static bool IsResolveThrowName(string name) =>
			name.Length > "ResolveThrow".Length - 1 &&
			name.StartsWith("Resolve", System.StringComparison.Ordinal) &&
			name.EndsWith("Throw", System.StringComparison.Ordinal);
	}
}
