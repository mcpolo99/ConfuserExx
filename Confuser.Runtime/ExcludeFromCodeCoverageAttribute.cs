#if NETFRAMEWORK && (NET20 || NET35)
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis {
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
	internal sealed class ExcludeFromCodeCoverageAttribute : Attribute {
	}
}
#endif
