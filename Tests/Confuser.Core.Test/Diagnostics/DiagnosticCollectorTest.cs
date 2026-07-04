using System;
using System.Collections.Generic;
using Confuser.Core;
using Confuser.Core.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Confuser.Core.Test.Diagnostics {
	public class DiagnosticCollectorTest {
		[Fact]
		public void Log_CapturesEntry_EvenWhenInnerLevelWouldFilterIt() {
			// GIVEN a collector wrapping an inner logger
			var collector = new DiagnosticCollector(NullLogger.Instance);

			// WHEN a debug message is logged (would be filtered from display)
			collector.LogDebug("hidden detail {0}", 42);

			// THEN the collector still captures it in full
			var snapshot = collector.Snapshot();
			Assert.Single(snapshot);
			Assert.Equal(LogLevel.Debug, snapshot[0].Level);
			Assert.Equal("hidden detail 42", snapshot[0].Message);
		}

		[Fact]
		public void Log_ForwardsToInnerLogger() {
			var inner = new ListLogger();
			var collector = new DiagnosticCollector(inner);

			collector.LogInformation("forward me");

			Assert.Contains("forward me", inner.Messages);
		}

		[Fact]
		public void Log_CapturesExceptionText() {
			var collector = new DiagnosticCollector(NullLogger.Instance);
			var ex = new InvalidOperationException("boom");

			collector.LogError(ex, "failed");

			var entry = Assert.Single(collector.Snapshot());
			Assert.Equal("failed", entry.Message);
			Assert.Contains("boom", entry.Exception);
		}

		[Fact]
		public void IsEnabled_ReturnsTrue_ToCaptureAllLevels() {
			var collector = new DiagnosticCollector(NullLogger.Instance);
			Assert.True(collector.IsEnabled(LogLevel.Trace));
		}

		[Fact]
		public void Finish_RecordsSuccess() {
			var collector = new DiagnosticCollector(NullLogger.Instance);
			((IProgressReporter)collector).Finish(true);
			Assert.True(collector.Successful);
		}

		[Fact]
		public void Finish_LastWins_SoNestedPackerRunDoesNotClobberTopLevelResult() {
			var collector = new DiagnosticCollector(NullLogger.Instance);
			var reporter = (IProgressReporter)collector;

			// nested packer stub finishes first (success), then the top-level run fails
			reporter.Finish(true);
			reporter.Finish(false);

			Assert.False(collector.Successful);
		}

		[Fact]
		public void Finish_ForwardsToInnerReporter() {
			var spy = new SpyReporter();
			var collector = new DiagnosticCollector(NullLogger.Instance, spy);

			((IProgressReporter)collector).Finish(true);

			Assert.Equal(1, spy.FinishCount);
			Assert.True(spy.LastSuccessful);
		}

		[Fact]
		public void Progress_ForwardsToInnerReporter() {
			var spy = new SpyReporter();
			var collector = new DiagnosticCollector(NullLogger.Instance, spy);
			var reporter = (IProgressReporter)collector;

			reporter.Progress(3, 10);
			reporter.EndProgress();

			Assert.Equal(1, spy.ProgressCount);
			Assert.Equal(1, spy.EndProgressCount);
		}

		[Fact]
		public void Snapshot_KeepsOnlyMostRecentEntries_AndCountsDropped() {
			var collector = new DiagnosticCollector(NullLogger.Instance, capacity: 3);

			for (int i = 0; i < 5; i++)
				collector.LogInformation("entry {0}", i);

			var snapshot = collector.Snapshot();
			Assert.Equal(3, snapshot.Count);
			Assert.Equal(2, collector.DroppedCount);
			Assert.Equal("entry 2", snapshot[0].Message);
			Assert.Equal("entry 4", snapshot[2].Message);
		}

		[Fact]
		public void GenerateReport_NeverThrows_AndContainsCoreSections() {
			var collector = new DiagnosticCollector(NullLogger.Instance);
			collector.LogInformation("did a thing");
			((IProgressReporter)collector).Finish(false);

			var report = collector.GenerateReport();

			Assert.Contains("## System", report);
			Assert.Contains("## Result", report);
			Assert.Contains("did a thing", report);
		}

		sealed class ListLogger : ILogger {
			public List<string> Messages { get; } = new List<string>();
			public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
			public bool IsEnabled(LogLevel logLevel) => true;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
				Func<TState, Exception, string> formatter) => Messages.Add(formatter(state, exception));

			sealed class NullScope : IDisposable {
				public static readonly NullScope Instance = new NullScope();
				public void Dispose() { }
			}
		}

		sealed class SpyReporter : IProgressReporter {
			public int ProgressCount;
			public int EndProgressCount;
			public int FinishCount;
			public bool LastSuccessful;

			public void Progress(int progress, int overall) => ProgressCount++;
			public void EndProgress() => EndProgressCount++;
			public void Finish(bool successful) {
				FinishCount++;
				LastSuccessful = successful;
			}
		}
	}
}
