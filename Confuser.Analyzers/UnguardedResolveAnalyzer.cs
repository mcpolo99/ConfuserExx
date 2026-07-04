using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Confuser.Analyzers {
	/// <summary>
	///     CX002 — flags the result of a non-throwing dnlib <c>ResolveTypeDef()</c> /
	///     <c>ResolveMethodDef()</c> being dereferenced immediately, with no null check. Those
	///     methods return <c>null</c> for external or unresolvable references, so a direct
	///     dereference crashes with a <see cref="System.NullReferenceException" /> on assemblies
	///     that reference types outside the obfuscation set.
	/// </summary>
	/// <remarks>
	///     Only a genuine dereference of the result is flagged — <c>x.ResolveTypeDef().Member</c> or
	///     <c>x.ResolveMethodDef()[i]</c>. A null-conditional (<c>?.</c>), an assignment, a
	///     <c>return</c>, or passing the result as an argument (e.g. to dnlib's null-tolerant
	///     <c>SigComparer.Equals</c>) is not a crash and is intentionally not reported.
	/// </remarks>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class UnguardedResolveAnalyzer : DiagnosticAnalyzer {
		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticIds.UnguardedResolve,
			"Resolve result dereferenced without a null check",
			"'{0}()' returns null for unresolvable references; check the result for null before dereferencing it",
			DiagnosticIds.Category,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "dnlib's ResolveTypeDef()/ResolveMethodDef() return null when a reference cannot be " +
				"resolved (external or unresolvable types). Dereferencing the result directly crashes obfuscation " +
				"on such assemblies; guard it with a null check or the null-conditional operator.");

		static readonly ImmutableHashSet<string> ResolveMethods =
			ImmutableHashSet.Create("ResolveTypeDef", "ResolveMethodDef");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
		}

		static void Analyze(SyntaxNodeAnalysisContext context) {
			var invocation = (InvocationExpressionSyntax)context.Node;
			if (invocation.Expression is not MemberAccessExpressionSyntax member ||
				!ResolveMethods.Contains(member.Name.Identifier.ValueText) ||
				invocation.ArgumentList.Arguments.Count != 0)
				return;

			if (!IsImmediatelyDereferenced(invocation))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, member.Name.GetLocation(),
				member.Name.Identifier.ValueText));
		}

		/// <summary>
		///     Returns <c>true</c> only when <paramref name="invocation" /> is the receiver of a
		///     plain member access or element access — i.e. its result is dereferenced right away
		///     without a null guard. A null-conditional access makes the parent a
		///     <see cref="ConditionalAccessExpressionSyntax" />, which is safe and returns false.
		/// </summary>
		static bool IsImmediatelyDereferenced(InvocationExpressionSyntax invocation) {
			switch (invocation.Parent) {
				case MemberAccessExpressionSyntax parentMember when parentMember.Expression == invocation:
					return true;
				case ElementAccessExpressionSyntax parentElement when parentElement.Expression == invocation:
					return true;
				default:
					return false;
			}
		}
	}
}
