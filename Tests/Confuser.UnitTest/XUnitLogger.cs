using System;
using Confuser.Core;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Confuser.UnitTest {
	public sealed class XunitLogger : Microsoft.Extensions.Logging.ILogger, IProgressReporter {
		private readonly ITestOutputHelper _outputHelper;
		private readonly Action<string> _outputAction;

		public XunitLogger(ITestOutputHelper outputHelper) : this(outputHelper, null) { }

		public XunitLogger(ITestOutputHelper outputHelper, Action<string> outputAction) {
			_outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
			_outputAction = outputAction;
		}

		public IDisposable BeginScope<TState>(TState state) => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
			Exception exception, Func<TState, Exception, string> formatter) {
			var message = formatter(state, exception);

			if (logLevel >= LogLevel.Error)
				throw new Exception(message, exception);

			ProcessOutput(message);
		}

		void IProgressReporter.Progress(int progress, int overall) { }

		void IProgressReporter.EndProgress() { }

		void IProgressReporter.Finish(bool successful) => ProcessOutput("[DONE]");

		private void ProcessOutput(string message) {
			_outputAction?.Invoke(message);
			_outputHelper.WriteLine(message);
		}
	}
}
