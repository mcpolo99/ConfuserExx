using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Confuser.Analyzers {
	/// <summary>
	///     CX003: Flags usage of ResolveThrow/ResolveTypeDefThrow/ResolveMethodDefThrow.
	///     These throw on resolution failure instead of returning null — audit for intentionality.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class ResolveThrowAuditAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "CX003";

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			"ResolveThrow usage — consider non-throwing variant",
			"'{0}' throws on resolution failure. Consider using the non-throwing variant with a null check.",
			"Confuser.Reliability",
			DiagnosticSeverity.Info,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		static readonly string[] ThrowMethodNames = {
			"ResolveThrow", "ResolveTypeDefThrow", "ResolveMethodDefThrow"
		};

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
		}

		void AnalyzeInvocation(OperationAnalysisContext context) {
			var invocation = (IInvocationOperation)context.Operation;
			var methodName = invocation.TargetMethod.Name;
			if (ThrowMethodNames.Contains(methodName)) {
				context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), methodName));
			}
		}
	}

	/// <summary>
	///     CX001: Detects typeof(X).GetMethod() passed to module.Import() — imports from host runtime
	///     instead of the target assembly's framework version.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class TypeofImportAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "CX001";

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			"typeof().GetMethod() with module.Import() uses host runtime",
			"Importing '{0}' via typeof() reflection resolves from the host runtime, not the target assembly's framework. Use dnlib type resolution instead.",
			"Confuser.Correctness",
			DiagnosticSeverity.Error,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
		}

		void AnalyzeInvocation(OperationAnalysisContext context) {
			var invocation = (IInvocationOperation)context.Operation;
			if (invocation.TargetMethod.Name != "Import")
				return;

			// Check if any argument contains typeof(...).GetMethod(...)
			foreach (var arg in invocation.Arguments) {
				if (IsTypeofGetMethodPattern(arg.Value))
					context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(),
						invocation.TargetMethod.ToDisplayString()));
			}
		}

		static bool IsTypeofGetMethodPattern(IOperation operation) {
			// Unwrap conversions
			while (operation is IConversionOperation conv)
				operation = conv.Operand;

			if (operation is IInvocationOperation innerCall) {
				var name = innerCall.TargetMethod.Name;
				if (name == "GetMethod" || name == "GetField" || name == "GetProperty") {
					// Check if receiver is typeof(...)
					var receiver = innerCall.Instance;
					if (receiver is ITypeOfOperation)
						return true;
					// typeof() result may be stored in variable — check arguments
					foreach (var arg in innerCall.Arguments) {
						if (arg.Value is ITypeOfOperation)
							return true;
					}
				}
			}
			return false;
		}
	}

	/// <summary>
	///     CX004: Detects System.Reflection Assembly.GetTypes()/Module.GetTypes() calls
	///     not wrapped in a try-catch for ReflectionTypeLoadException.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class GetTypesWithoutCatchAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "CX004";

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			"GetTypes() without ReflectionTypeLoadException catch",
			"'{0}' can throw ReflectionTypeLoadException when types have unresolvable dependencies. Wrap in try-catch.",
			"Confuser.Reliability",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
		}

		void AnalyzeInvocation(OperationAnalysisContext context) {
			var invocation = (IInvocationOperation)context.Operation;
			if (invocation.TargetMethod.Name != "GetTypes")
				return;

			// Only flag System.Reflection types, not dnlib
			var containingType = invocation.TargetMethod.ContainingType?.ToString();
			if (containingType == null)
				return;
			if (!containingType.StartsWith("System.Reflection."))
				return;

			// Check if inside a try-catch that handles ReflectionTypeLoadException
			if (IsInsideCatchingTry(invocation.Syntax))
				return;

			context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), containingType + ".GetTypes()"));
		}

		static bool IsInsideCatchingTry(SyntaxNode node) {
			var current = node.Parent;
			while (current != null) {
				if (current is TryStatementSyntax trySyntax) {
					foreach (var catchClause in trySyntax.Catches) {
						var typeName = catchClause.Declaration?.Type?.ToString();
						if (typeName == null) // bare catch
							return true;
						if (typeName.Contains("ReflectionTypeLoadException") || typeName == "Exception")
							return true;
					}
				}
				current = current.Parent;
			}
			return false;
		}
	}

	/// <summary>
	///     CX002: Detects ResolveTypeDef()/ResolveMethodDef() calls where the result
	///     is used without a null check.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class ResolveWithoutNullCheckAnalyzer : DiagnosticAnalyzer {
		public const string DiagnosticId = "CX002";

		static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			"Resolve result used without null check",
			"'{0}' can return null when resolution fails. Check for null before use.",
			"Confuser.Reliability",
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		static readonly string[] ResolveMethodNames = {
			"ResolveTypeDef", "ResolveMethodDef"
		};

		public override void Initialize(AnalysisContext context) {
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
		}

		void AnalyzeInvocation(SyntaxNodeAnalysisContext context) {
			var invocation = (InvocationExpressionSyntax)context.Node;
			var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
			if (memberAccess == null)
				return;

			var methodName = memberAccess.Name.Identifier.Text;
			if (!ResolveMethodNames.Contains(methodName))
				return;

			// Check if the result is immediately accessed without null check
			var parent = invocation.Parent;

			// Pattern: foo.ResolveTypeDef().Something — direct member access on result
			if (parent is MemberAccessExpressionSyntax outerAccess && outerAccess.Expression == invocation) {
				// Check if it's ?. (null-conditional)
				if (!(parent.Parent is ConditionalAccessExpressionSyntax))
					context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
			}

			// Pattern: var x = foo.ResolveTypeDef(); — check if assigned and used without null check
			// This is complex data-flow analysis; for now, only flag direct member access chains.
			// The while-loop pattern (where null terminates iteration) is safe and not flagged.
		}
	}
}
