using System;
using System.Windows.Forms;

namespace CrossFramework.WinForms {
	static class Program {
		[STAThread]
		static int Main(string[] args) {
			// When run with --verify, skip UI and just validate the assembly loads correctly
			if (args.Length > 0 && args[0] == "--verify") {
				Console.WriteLine("START");
				var form = new MainForm();
				Console.WriteLine("Label: " + form.GetLabelText());
				Console.WriteLine("Title: " + form.Text);
				Console.WriteLine("END");
				form.Dispose();
				return 42;
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
			return 0;
		}
	}
}
