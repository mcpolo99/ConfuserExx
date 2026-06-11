namespace Confuser.Core {
	/// <summary>
	///     Reports progress and completion of a protection process.
	/// </summary>
	public interface IProgressReporter {
		/// <summary>
		///     Reports the progress of protection.
		/// </summary>
		/// <param name="progress">The amount of work done.</param>
		/// <param name="overall">The total work amount.</param>
		void Progress(int progress, int overall);

		/// <summary>
		///     Signals the end of a progress sequence.
		/// </summary>
		void EndProgress();

		/// <summary>
		///     Signals the finish of the protection process.
		/// </summary>
		/// <param name="successful">Whether the protection process succeeded.</param>
		void Finish(bool successful);
	}
}
