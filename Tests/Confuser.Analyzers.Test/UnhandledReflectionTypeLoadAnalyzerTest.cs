using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	Confuser.Analyzers.UnhandledReflectionTypeLoadAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Confuser.Analyzers.Test {
	public class UnhandledReflectionTypeLoadAnalyzerTest {
		[Fact]
		public async Task Flags_UnguardedAssemblyGetTypes() {
			const string source = @"
using System.Reflection;
class C {
    void M(Assembly asm) {
        var t = asm.{|#0:GetTypes|}();
    }
}";
			var expected = Verify.Diagnostic("CX004")
				.WithLocation(0)
				.WithArguments("System.Reflection.Assembly");
			await Verify.VerifyAnalyzerAsync(source, expected);
		}

		[Fact]
		public async Task DoesNotFlag_WhenGuardedByReflectionTypeLoadException() {
			const string source = @"
using System;
using System.Reflection;
class C {
    void M(Assembly asm) {
        try { var t = asm.GetTypes(); }
        catch (ReflectionTypeLoadException) { }
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task DoesNotFlag_WhenGuardedByBaseException() {
			const string source = @"
using System;
using System.Reflection;
class C {
    void M(Assembly asm) {
        try { var t = asm.GetTypes(); }
        catch (Exception) { }
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task DoesNotFlag_UnrelatedGetTypesMethod() {
			// A GetTypes() on a non-reflection type must not be flagged.
			const string source = @"
class Other { public int[] GetTypes() => new int[0]; }
class C {
    void M(Other o) {
        var t = o.GetTypes();
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}
	}
}
