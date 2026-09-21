using System;

namespace CrossFramework.SelfTest {
	// Shared across the console subject apps (linked, not duplicated). Runs the app and reports
	// failure if anything goes unhandled. Console apps have no window, so "running" simply means
	// executing to completion; the crash path proves the unhandled-exception handler reports it.
	//
	// Contract: prints START / RUN / END and exits 42 on a clean run; on a crash prints
	// "CRASH: ..." and exits 1. System.Console is fully qualified because the subject apps live in
	// the CrossFramework.Console namespace, which would otherwise shadow it.
	internal static class ConsoleSelfTestHost {
		public static int Run(bool induceCrash) {
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
				var ex = e.ExceptionObject as Exception;
				System.Console.WriteLine("CRASH: [AppDomain] " + (ex != null ? ex.Message : "unknown"));
				System.Console.Out.Flush();
				Environment.Exit(1);
			};

			System.Console.WriteLine("START");
			System.Console.WriteLine("RUN");
			if (induceCrash)
				throw new InvalidOperationException("Induced self-test crash");
			System.Console.WriteLine("END");
			return 42;
		}
	}
}
