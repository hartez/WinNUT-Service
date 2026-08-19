using NUTDotNetServer;

namespace WinNUT.Simulator
{
	public class TestServer : NUTServer
	{
		readonly TestUPS _ups = new("TestUPS", description: "Simulated UPS");

		public int Charge
		{
			get => int.Parse(GetTestServerVariable("battery.charge"));
			set => SetVariable("battery.charge", value.ToString(), NUTDotNetShared.VarFlags.Number);
		}

		public string Status
		{
			get => GetTestServerVariable("ups.status");
			set => SetVariable("ups.status", value.ToString(), NUTDotNetShared.VarFlags.String);
		}

		public int BatteryRemainingTime
		{
			get => int.Parse(GetTestServerVariable("battery.runtime"));
			set => SetVariable("battery.runtime", value.ToString(), NUTDotNetShared.VarFlags.Number);
		}

		public string SerialNumber
		{
			get => GetTestServerVariable("device.serial");
			set => SetVariable("device.serial", value.ToString(), NUTDotNetShared.VarFlags.String);
		}

		public TestServer(ushort listenPort = 3493, bool singleQuery = false) : base(listenPort, singleQuery)
		{
			UPSs.Add(_ups);
		}

		public void StartAndWaitUntilListening()
		{
			Start();

			// We have to wait until the server is accepting connections before we can do anything with it
			while (!IsListening)
			{
				Thread.Sleep(20);
			}
		}

		void SetVariable(string name, string value, NUTDotNetShared.VarFlags flags)
		{
			if (_ups.Variables.Any(v => v.Name == name))
			{
				var variable = _ups.GetVariableByName(name);
				if (variable != null)
				{
					_ups.Variables.Remove(variable);
				}
			}

			_ups.Variables.Add(
				new NUTDotNetShared.UPSVariable(name, flags)
				{
					Value = value
				});
		}

		string GetTestServerVariable(string name)
		{
			return _ups.GetVariableByName(name).Value;
		}
	}

	public class TestUPS(string name, string? description = null) : ServerUPS(name, description)
	{
	}
}
