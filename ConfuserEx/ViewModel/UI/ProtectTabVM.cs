using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Confuser.Core;
using Confuser.Core.Diagnostics;
using Confuser.Core.Project;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ConfuserEx.ViewModel {
	internal class ProtectTabVM : TabViewModel, IProgressReporter {
		readonly Paragraph documentContent;
		CancellationTokenSource cancelSrc;
		DiagnosticCollector collector;
		double? progress = 0;
		bool? result;

		public ProtectTabVM(AppVM app)
			: base(app, "Protect!") {
			documentContent = new Paragraph();
			LogDocument = new FlowDocument();
			LogDocument.Blocks.Add(documentContent);
		}

		public ICommand ProtectCmd {
			get { return new RelayCommand(DoProtect, () => !App.NavigationDisabled); }
		}

		public ICommand CancelCmd {
			get { return new RelayCommand(DoCancel, () => App.NavigationDisabled); }
		}

		public ICommand CopyReportCmd {
			get { return new RelayCommand(DoCopyReport, () => Result != null && collector != null); }
		}

		public double? Progress {
			get { return progress; }
			set { SetProperty(ref progress, value, "Progress"); }
		}

		public FlowDocument LogDocument { get; private set; }

		public bool? Result {
			get { return result; }
			set { SetProperty(ref result, value, "Result"); }
		}

		void DoProtect() {
			var parameters = new ConfuserParameters();
			parameters.Project = ((IViewModel<ConfuserProject>)App.Project).Model;
			if (File.Exists(App.FileName))
				Environment.CurrentDirectory = Path.GetDirectoryName(App.FileName);

			documentContent.Inlines.Clear();

			var serilogLogger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Sink(new FlowDocumentSink(documentContent))
				.CreateLogger();

			// The logger factory must outlive the async protection run — ConfuserEngine.Run
			// executes on a background thread, so we dispose it in the continuation below
			// rather than with a method-scoped 'using' (which would dispose it too early).
			var loggerFactory = LoggerFactory.Create(builder =>
				builder.AddSerilog(serilogLogger, dispose: true));
			var melLogger = loggerFactory.CreateLogger("ConfuserEx");

			// The collector wraps the logger and this progress reporter so a diagnostic report —
			// covering both successful and failed runs — can be copied afterwards. It captures the
			// full transcript regardless of the display level and forwards everything through.
			collector = new DiagnosticCollector(melLogger, this) { Project = parameters.Project };
			parameters.Logger = collector;
			parameters.ProgressReporter = collector;

			cancelSrc = new CancellationTokenSource();
			Result = null;
			Progress = null;
			begin = DateTime.Now;
			App.NavigationDisabled = true;

			ConfuserEngine.Run(parameters, cancelSrc.Token)
						  .ContinueWith(_ => {
							  loggerFactory.Dispose();
							  Application.Current.Dispatcher.BeginInvoke(new Action(() => {
								  Progress = 0;
								  App.NavigationDisabled = false;
								  CommandManager.InvalidateRequerySuggested();
							  }));
						  });
		}

		void DoCancel() {
			cancelSrc.Cancel();
		}

		void DoCopyReport() {
			if (collector == null)
				return;

			try {
				Clipboard.SetText(collector.GenerateReport());
			}
			catch {
				// The clipboard can be transiently locked by another process; a failed copy
				// should never crash the app. The user can simply retry.
			}
		}

		#region IProgressReporter

		DateTime begin;

		void IProgressReporter.Progress(int progress, int overall) {
			Progress = (double)progress / overall;
		}

		void IProgressReporter.EndProgress() {
			Progress = null;
		}

		void IProgressReporter.Finish(bool successful) {
			DateTime now = DateTime.Now;
			string timeString = string.Format(
				"at {0}, {1}:{2:d2} elapsed.",
				now.ToShortTimeString(),
				(int)now.Subtract(begin).TotalMinutes,
				now.Subtract(begin).Seconds);

			Application.Current.Dispatcher.BeginInvoke(new Action(() => {
				if (successful) {
					documentContent.Inlines.Add(new Run("Finished " + timeString) { Foreground = Brushes.Lime });
				}
				else {
					documentContent.Inlines.Add(new Run("Failed " + timeString) { Foreground = Brushes.Red });
				}
				documentContent.Inlines.Add(new LineBreak());
			}));

			Result = successful;
		}

		#endregion
	}
}
