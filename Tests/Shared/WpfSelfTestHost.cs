using System;
using System.Windows;
using System.Windows.Threading;

namespace CrossFramework.SelfTest {
	// Shared across the WPF subject apps (linked, not duplicated). Shows the real main window
	// off-screen inside the already-running Application, lets it live briefly, then closes it.
	// Any unhandled exception (dispatcher or background) makes the process report failure.
	//
	// Contract: prints START / SHOWN / END and exits 42 on a clean run; on a crash prints
	// "CRASH: ..." and exits 1. Call from Application.OnStartup; the app's own Run loop keeps
	// pumping, and closing the window shuts the app down with exit code 42.
	internal static class WpfSelfTestHost {
		public static void Run(Application app, Window window, bool induceCrash = false) {
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
				Fail("AppDomain", e.ExceptionObject as Exception);
			app.DispatcherUnhandledException += (sender, e) =>
				Fail("Dispatcher", e.Exception);

			Console.WriteLine("START");

			window.ShowInTaskbar = false;
			window.WindowStartupLocation = WindowStartupLocation.Manual;
			window.Left = -32000;
			window.Top = -32000;

			window.Loaded += (sender, e) => {
				Console.WriteLine("SHOWN: " + window.Title);
				if (induceCrash)
					throw new InvalidOperationException("Induced self-test crash");

				var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
				timer.Tick += (s, args) => {
					timer.Stop();
					window.Close();
				};
				timer.Start();
			};

			window.Closed += (sender, e) => {
				Console.WriteLine("END");
				// WPF's generated Main is void and ignores Run()'s return value, so set the
				// process exit code explicitly rather than relying on Shutdown's argument.
				Environment.ExitCode = 42;
				app.Shutdown();
			};

			window.Show();
		}

		static void Fail(string source, Exception ex) {
			Console.WriteLine("CRASH: [" + source + "] " + (ex != null ? ex.Message : "unknown"));
			Console.Out.Flush();
			Environment.Exit(1);
		}
	}
}
