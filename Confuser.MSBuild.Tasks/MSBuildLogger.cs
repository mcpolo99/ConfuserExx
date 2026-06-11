using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;

namespace Confuser.MSBuild.Tasks {
	internal sealed class MSBuildMelLogger : Microsoft.Extensions.Logging.ILogger {
		private readonly TaskLoggingHelper loggingHelper;

		internal MSBuildMelLogger(TaskLoggingHelper loggingHelper) =>
			this.loggingHelper = loggingHelper ?? throw new ArgumentNullException(nameof(loggingHelper));

		public IDisposable BeginScope<TState>(TState state) => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
			Exception exception, Func<TState, Exception, string> formatter) {
			var message = formatter(state, exception);
			switch (logLevel) {
				case LogLevel.Trace:
				case LogLevel.Debug:
					loggingHelper.LogMessage(MessageImportance.Low, message);
					break;
				case LogLevel.Information:
					loggingHelper.LogMessage(MessageImportance.Normal, message);
					break;
				case LogLevel.Warning:
					loggingHelper.LogWarning(message);
					if (exception != null) loggingHelper.LogWarningFromException(exception);
					break;
				case LogLevel.Error:
				case LogLevel.Critical:
					loggingHelper.LogError(message);
					if (exception != null) loggingHelper.LogErrorFromException(exception);
					break;
			}
		}
	}

	internal sealed class MSBuildProgressReporter : Confuser.Core.IProgressReporter {
		internal bool HasError { get; private set; }

		void Confuser.Core.IProgressReporter.Progress(int progress, int overall) { }

		void Confuser.Core.IProgressReporter.EndProgress() { }

		void Confuser.Core.IProgressReporter.Finish(bool successful) {
			if (!successful) {
				HasError = true;
			}
		}
	}
}
