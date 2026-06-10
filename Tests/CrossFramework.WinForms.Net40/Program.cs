using System;
using System.Windows.Forms;

namespace CrossFramework.WinForms {
	static class Program {
		[STAThread]
		static int Main(string[] args) {
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
			Application.Run(new MainForm());
			return 0;
		}
	}
}
