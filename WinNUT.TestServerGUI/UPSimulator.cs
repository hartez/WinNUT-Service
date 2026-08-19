using WinNUT.Simulator;

namespace WinNUT.SimulatorGUI
{
	public partial class UPSSimulator : Form
	{
		protected TestServer TestServer { get; }

		public UPSSimulator()
		{
			TestServer = new TestServer();
			TestServer.StartAndWaitUntilListening();

			InitializeComponent();

			TestServer.Charge = 98;
			TestServer.Status = "OL";
			TestServer.BatteryRemainingTime = 4092;
			TestServer.SerialNumber = "12345";
		}

		private void UPSSimulator_Load(object sender, EventArgs e)
		{
			UpdateControls();
		}

		void UpdateControls()
		{
			Charge.Value = TestServer.Charge;
			BatteryRemaining.Value = TestServer.BatteryRemainingTime / 60;
			SerialNumber.Text = TestServer.SerialNumber;

			switch (TestServer.Status)
			{
				case ("OB"):
					OnBattery.Checked = true;
					break;
				case ("OL"):
					Online.Checked = true;
					break;
			}
		}

		private void Update_Click(object sender, EventArgs e)
		{
			TestServer.Charge = (int)Charge.Value;
			TestServer.BatteryRemainingTime = (int)(BatteryRemaining.Value * 60);
			TestServer.SerialNumber = SerialNumber.Text;

			if (OnBattery.Checked)
			{
				TestServer.Status = "OB";
			}

			if (Online.Checked)
			{
				TestServer.Status = "OL";
			}
		}
	}
}