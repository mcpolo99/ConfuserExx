namespace Confuser.Analyzers {
	/// <summary>
	///     Diagnostic IDs for the ConfuserEx-specific analyzers. Each catches a bug class that has
	///     actually been fixed in the codebase, to prevent regressions at compile time.
	/// </summary>
	internal static class DiagnosticIds {
		/// <summary>Imports a member from the host runtime instead of the target's corlib.</summary>
		public const string HostRuntimeImport = "CX001";

		/// <summary><c>ResolveTypeDef()</c>/<c>ResolveMethodDef()</c> used without a null check.</summary>
		public const string UnguardedResolve = "CX002";

		/// <summary>Usage of a <c>...Throw</c> resolve helper (audit / awareness).</summary>
		public const string ResolveThrowAudit = "CX003";

		/// <summary><c>GetTypes()</c> without handling <c>ReflectionTypeLoadException</c>.</summary>
		public const string UnhandledReflectionTypeLoad = "CX004";

		public const string Category = "ConfuserEx";
	}
}
