using Confuser.Core;
using Confuser.Core.Diagnostics;
using Confuser.Core.Project;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Confuser.Core.Test.Diagnostics {
	public class DiagnosticReportTest {
		static DiagnosticCollector CollectorFor(ConfuserProject project) {
			var collector = new DiagnosticCollector(NullLogger.Instance) { Project = project };
			return collector;
		}

		[Fact]
		public void Generate_ResultFailed_WhenRunUnsuccessful() {
			var collector = CollectorFor(new ConfuserProject());
			((IProgressReporter)collector).Finish(false);

			Assert.Contains("## Result: FAILED", collector.GenerateReport());
		}

		[Fact]
		public void Generate_ResultSuccess_WhenRunSuccessful() {
			var collector = CollectorFor(new ConfuserProject());
			((IProgressReporter)collector).Finish(true);

			Assert.Contains("## Result: SUCCESS", collector.GenerateReport());
		}

		[Fact]
		public void Generate_ListsProtections_FromProjectRules() {
			var project = new ConfuserProject();
			var rule = new Rule();
			rule.Add(new SettingItem<Protection>("rename"));
			rule.Add(new SettingItem<Protection>("constants"));
			project.Rules.Add(rule);

			var report = CollectorFor(project).GenerateReport();

			Assert.Contains("rename", report);
			Assert.Contains("constants", report);
		}

		[Fact]
		public void Generate_NeverLeaksStrongNamePassword() {
			var project = new ConfuserProject { BaseDirectory = @"C:\proj", OutputDirectory = @"C:\proj\out" };
			project.Add(new ProjectModule {
				Path = "App.dll",
				SNKeyPath = @"C:\proj\key.snk",
				SNKeyPassword = "SuperSecret123",
				SNSigKeyPassword = "AlsoSecret456"
			});

			var report = CollectorFor(project).GenerateReport();

			Assert.DoesNotContain("SuperSecret123", report);
			Assert.DoesNotContain("AlsoSecret456", report);
			// but the module itself is still listed for context
			Assert.Contains("App.dll", report);
		}

		[Fact]
		public void Generate_ListsPacker_OrNoneWhenAbsent() {
			var withPacker = new ConfuserProject { Packer = new SettingItem<Packer>("compressor") };
			Assert.Contains("compressor", CollectorFor(withPacker).GenerateReport());

			var noPacker = new ConfuserProject();
			Assert.Contains("Packer: (none)", CollectorFor(noPacker).GenerateReport());
		}

		[Fact]
		public void Generate_UsesDynamicFence_WhenLogContainsBacktickFence() {
			var collector = CollectorFor(new ConfuserProject());
			collector.LogInformation("here is a ``` fence inside a message");

			var report = collector.GenerateReport();

			// a plain ``` fence would be broken by the message; the report must use a longer fence
			Assert.Contains("````", report);
		}

		[Theory]
		[InlineData(@"C:\Users\alice\proj\App.dll", @"C:\Users\alice", "%USER%")]
		[InlineData(@"c:\users\ALICE\proj\App.dll", @"C:\Users\alice", "%USER%")]
		public void Redact_ReplacesUserProfilePrefix(string text, string userProfile, string expectedMarker) {
			var result = DiagnosticRedactor.Redact(text, userProfile);
			Assert.DoesNotContain("alice", result, System.StringComparison.OrdinalIgnoreCase);
			Assert.Contains(expectedMarker, result);
		}

		[Fact]
		public void Redact_LeavesTextWithoutProfileUntouched() {
			Assert.Equal("no paths here", DiagnosticRedactor.Redact("no paths here", @"C:\Users\alice"));
		}
	}
}
