using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
	Confuser.Analyzers.HostRuntimeImportAnalyzer,
	Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Confuser.Analyzers.Test {
	public class HostRuntimeImportAnalyzerTest {
		const string Stub = @"
using System;
class Mod { public object Import(object member) => member; }
";

		[Fact]
		public async Task Flags_ImportOfTypeVariableGetMethod() {
			string source = Stub + @"
class C {
    void M(Mod mod, Type t) {
        mod.Import(t.{|#0:GetMethod|}(""X""));
    }
}";
			var expected = Verify.Diagnostic("CX001").WithLocation(0).WithArguments("GetMethod");
			await Verify.VerifyAnalyzerAsync(source, expected);
		}

		[Fact]
		public async Task Flags_ImportOfTypeofGetConstructor() {
			string source = Stub + @"
class C {
    void M(Mod mod) {
        mod.Import(typeof(string).{|#0:GetConstructor|}(Type.EmptyTypes));
    }
}";
			var expected = Verify.Diagnostic("CX001").WithLocation(0).WithArguments("GetConstructor");
			await Verify.VerifyAnalyzerAsync(source, expected);
		}

		[Fact]
		public async Task DoesNotFlag_ImportOfNonReflectionMember() {
			// Resolving through a non-System.Type API (the safe path) must not be flagged.
			string source = Stub + @"
class Resolved { public object FindMethod(string n) => null; }
class C {
    void M(Mod mod, Resolved r) {
        mod.Import(r.FindMethod(""X""));
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task DoesNotFlag_NonImportCall() {
			string source = Stub + @"
class C {
    void M(Type t) {
        var m = t.GetMethod(""X"");
    }
}";
			await Verify.VerifyAnalyzerAsync(source);
		}
	}
}
