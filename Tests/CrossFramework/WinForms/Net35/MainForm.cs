using System;
using System.Drawing;
using System.Windows.Forms;

namespace CrossFramework.WinForms {
	public class MainForm : Form {
		private Button button;
		private Label label;

		public MainForm() {
			Text = "ConfuserEx WinForms Test (net35)";
			Width = 300;
			Height = 200;

			label = new Label {
				Text = "Not clicked",
				Location = new Point(20, 20),
				AutoSize = true
			};

			button = new Button {
				Text = "Click Me",
				Location = new Point(20, 60)
			};
			button.Click += OnButtonClick;

			Controls.Add(label);
			Controls.Add(button);
		}

		private void OnButtonClick(object sender, EventArgs e) {
			label.Text = "Clicked!";
		}

		public string GetLabelText() {
			return label.Text;
		}
	}
}
