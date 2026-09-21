using System;
using System.Windows;

namespace CrossFramework.SelfTest {
	// Shared across the WPF subject apps (linked, not duplicated). Shows the real main window
	// (invisible + off-screen) inside the already-running Application, lets it live briefly, then
	// ends the process. Any unhandled exception (dispatcher or background) makes it report failure.
	//
	// Contract: prints START / SHOWN / END and exits 42 on a clean run; on a crash prints
	// "CRASH: ..." and exits 1. Termination is driven by a thread-pool watchdog rather than a UI
	// timer, so it always ends even if the dispatcher never closes the window.
	internal static class WpfSelfTestHost {
		public static void Run(Application app, Window window, bool induceCrash = false) {
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
				Fail("AppDomain", e.ExceptionObject as Exception);
			app.DispatcherUnhandledException += (sender, e) =>
				Fail("Dispatcher", e.Exception);

			Console.WriteLine("START");

			window.ShowInTaskbar = false;
			window.ShowActivated = false;
			window.WindowState = WindowState.Minimized;
			window.Opacity = 0d;
			window.WindowStartupLocation = WindowStartupLocation.Manual;
			window.Left = -32000;
			window.Top = -32000;

			window.Loaded += (sender, e) => {
				Console.WriteLine("SHOWN: " + window.Title);
				if (induceCrash)
					throw new InvalidOperationException("Induced self-test crash");
			};

			// Guaranteed terminator, independent of the dispatcher (see WinForms host). Rooted for
			// the app's lifetime by the running Application, which holds this host on its stack.
			_watchdog = new System.Threading.Timer(
				_ => Succeed(), null, 2000, System.Threading.Timeout.Infinite);

			window.Show();
		}

		static System.Threading.Timer _watchdog;

		static void Succeed() {
			Console.WriteLine("END");
			Console.Out.Flush();
			Environment.Exit(42);
		}

		static void Fail(string source, Exception ex) {
			Console.WriteLine("CRASH: [" + source + "] " + (ex != null ? ex.Message : "unknown"));
			Console.Out.Flush();
			Environment.Exit(1);
		}
	}
}
