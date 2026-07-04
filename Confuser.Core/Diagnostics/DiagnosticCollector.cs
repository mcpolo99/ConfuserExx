using System;
using System.Collections.Generic;
using Confuser.Core.Project;
using Microsoft.Extensions.Logging;

namespace Confuser.Core.Diagnostics {
	/// <summary>
	///     A single captured log entry, rendered to text at capture time.
	/// </summary>
	public readonly struct DiagnosticLogEntry {
		public DiagnosticLogEntry(LogLevel level, string message, string exception) {
			Level = level;
			Message = message;
			Exception = exception;
		}

		/// <summary>The severity of the entry.</summary>
		public LogLevel Level { get; }

		/// <summary>The rendered message text.</summary>
		public string Message { get; }

		/// <summary>The rendered exception (including stack trace), or <c>null</c> if none.</summary>
		public string Exception { get; }
	}

	/// <summary>
	///     Wraps the real <see cref="ILogger" /> and <see cref="IProgressReporter" /> used during an
	///     obfuscation run, passing every call through while capturing a full-verbosity transcript,
	///     timing and outcome. On completion — success or failure — it can produce a self-contained
	///     markdown diagnostic report suitable for a bug report.
	/// </summary>
	/// <remarks>
	///     <para>
	///         Capture is intentionally independent of the inner logger's level: the collector reports
	///         <see cref="IsEnabled" /> as <c>true</c> and keeps every entry, so a report from a default
	///         (Information) run still contains the Debug detail needed to diagnose a failure. Display
	///         filtering is preserved because entries are only forwarded to the inner logger when it is
	///         enabled for that level.
	///     </para>
	///     <para>
	///         The entry buffer is bounded (see <see cref="DefaultCapacity" />); once full, the oldest
	///         entries are dropped and counted in <see cref="DroppedCount" /> so the report can note the
	///         loss rather than silently mislead.
	///     </para>
	///     <para>
	///         <see cref="Finish" /> is last-wins: a packer runs a nested engine pass with the same
	///         collector, so the top-level run — which finishes last — determines the reported outcome
	///         and elapsed time.
	///     </para>
	/// </remarks>
	public sealed class DiagnosticCollector : ILogger, IProgressReporter {
		/// <summary>The default maximum number of log entries retained.</summary>
		public const int DefaultCapacity = 2000;

		readonly ILogger inner;
		readonly IProgressReporter innerReporter;
		readonly int capacity;
		readonly object sync = new object();
		readonly Queue<DiagnosticLogEntry> entries;
		readonly DateTime begin = DateTime.UtcNow;
		int dropped;
		bool? successful;
		TimeSpan elapsed;

		/// <summary>
		///     Initializes a new collector.
		/// </summary>
		/// <param name="inner">The logger to forward display output to. Required.</param>
		/// <param name="innerReporter">The progress reporter to forward to, or <c>null</c>.</param>
		/// <param name="capacity">The maximum number of log entries to retain.</param>
		public DiagnosticCollector(ILogger inner, IProgressReporter innerReporter = null, int capacity = DefaultCapacity) {
			this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
			this.innerReporter = innerReporter;
			this.capacity = capacity < 1 ? 1 : capacity;
			entries = new Queue<DiagnosticLogEntry>(Math.Min(this.capacity, 64));
		}

		/// <summary>
		///     The project being processed, used to populate the report's configuration section.
		/// </summary>
		public ConfuserProject Project { get; set; }

		/// <summary>
		///     The run outcome: <c>true</c> on success, <c>false</c> on failure, <c>null</c> if the run
		///     never reported completion.
		/// </summary>
		public bool? Successful {
			get { lock (sync) return successful; }
		}

		/// <summary>The elapsed time recorded at the last <see cref="Finish" /> call.</summary>
		public TimeSpan Elapsed {
			get { lock (sync) return elapsed; }
		}

		/// <summary>The number of log entries dropped because the buffer was full.</summary>
		public int DroppedCount {
			get { lock (sync) return dropped; }
		}

		/// <summary>
		///     Returns an immutable copy of the currently retained log entries, oldest first.
		/// </summary>
		public IReadOnlyList<DiagnosticLogEntry> Snapshot() {
			lock (sync) return new List<DiagnosticLogEntry>(entries);
		}

		/// <summary>
		///     Produces the markdown diagnostic report. Never throws.
		/// </summary>
		public string GenerateReport() => DiagnosticReport.Generate(this);

		#region ILogger

		public IDisposable BeginScope<TState>(TState state) => inner.BeginScope(state);

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
			Func<TState, Exception, string> formatter) {
			string message;
			try {
				message = formatter != null ? formatter(state, exception) : state?.ToString() ?? string.Empty;
			}
			catch {
				message = state?.ToString() ?? string.Empty;
			}

			var entry = new DiagnosticLogEntry(logLevel, message, exception?.ToString());
			lock (sync) {
				entries.Enqueue(entry);
				while (entries.Count > capacity) {
					entries.Dequeue();
					dropped++;
				}
			}

			// Forward to the inner logger for display; it applies its own level filter.
			if (inner.IsEnabled(logLevel))
				inner.Log(logLevel, eventId, state, exception, formatter);
		}

		#endregion

		#region IProgressReporter

		public void Progress(int progress, int overall) => innerReporter?.Progress(progress, overall);

		public void EndProgress() => innerReporter?.EndProgress();

		public void Finish(bool successful) {
			lock (sync) {
				this.successful = successful;
				elapsed = DateTime.UtcNow - begin;
			}

			innerReporter?.Finish(successful);
		}

		#endregion
	}
}
