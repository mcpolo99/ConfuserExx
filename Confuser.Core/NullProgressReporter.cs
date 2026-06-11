namespace Confuser.Core {
	/// <summary>
	///     An <see cref="IProgressReporter" /> implementation that discards all progress reports.
	/// </summary>
	internal sealed class NullProgressReporter : IProgressReporter {
		public static readonly NullProgressReporter Instance = new NullProgressReporter();

		NullProgressReporter() { }

		public void Progress(int progress, int overall) { }
		public void EndProgress() { }
		public void Finish(bool successful) { }
	}
}
