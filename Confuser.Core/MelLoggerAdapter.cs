using System;
using Microsoft.Extensions.Logging;
using MelILogger = Microsoft.Extensions.Logging.ILogger;
using MelLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Confuser.Core {
	/// <summary>
	///     Adapts a <see cref="MelILogger" /> (Microsoft.Extensions.Logging) to the
	///     <see cref="ILogger" /> interface used internally by Confuser.
	/// </summary>
	public sealed class MelLoggerAdapter : ILogger {
		readonly MelILogger inner;

		public MelLoggerAdapter(MelILogger logger) {
			inner = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public void Debug(string msg) => inner.Log(MelLogLevel.Debug, msg);

		public void DebugFormat(string format, params object[] args) =>
			inner.Log(MelLogLevel.Debug, format, args);

		public void Info(string msg) => inner.Log(MelLogLevel.Information, msg);

		public void InfoFormat(string format, params object[] args) =>
			inner.Log(MelLogLevel.Information, format, args);

		public void Warn(string msg) => inner.Log(MelLogLevel.Warning, msg);

		public void WarnFormat(string format, params object[] args) =>
			inner.Log(MelLogLevel.Warning, format, args);

		public void WarnException(string msg, Exception ex) =>
			inner.Log(MelLogLevel.Warning, ex, msg);

		public void Error(string msg) => inner.Log(MelLogLevel.Error, msg);

		public void ErrorFormat(string format, params object[] args) =>
			inner.Log(MelLogLevel.Error, format, args);

		public void ErrorException(string msg, Exception ex) =>
			inner.Log(MelLogLevel.Error, ex, msg);
	}
}
