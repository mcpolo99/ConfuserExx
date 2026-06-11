using System;

namespace Confuser.Core {
	/// <summary>
	///     An <see cref="ILogger" /> implementation that doesn't actually do any logging.
	/// </summary>
	internal sealed class NullLogger : ILogger {
		/// <summary>
		///     The singleton instance of <see cref="NullLogger" />.
		/// </summary>
		public static readonly NullLogger Instance = new NullLogger();

		NullLogger() { }

		/// <inheritdoc />
		public void Debug(string msg) { }

		/// <inheritdoc />
		public void DebugFormat(string format, params object[] args) { }

		/// <inheritdoc />
		public void Info(string msg) { }

		/// <inheritdoc />
		public void InfoFormat(string format, params object[] args) { }

		/// <inheritdoc />
		public void Warn(string msg) { }

		/// <inheritdoc />
		public void WarnFormat(string format, params object[] args) { }

		/// <inheritdoc />
		public void WarnException(string msg, Exception ex) { }

		/// <inheritdoc />
		public void Error(string msg) { }

		/// <inheritdoc />
		public void ErrorFormat(string format, params object[] args) { }

		/// <inheritdoc />
		public void ErrorException(string msg, Exception ex) { }
	}
}
