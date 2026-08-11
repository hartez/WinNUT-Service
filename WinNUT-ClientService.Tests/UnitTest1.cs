using NUTDotNetServer;

namespace WinNUT_ClientService.Tests
{
	public class UnitTest1
	{
		[Fact]
		public void Test1()
		{
			var server = new NUTServer(0);

			var ups = new ServerUPS("TestUPS");

			var charge = new NUTDotNetShared.UPSVariable("battery.charge", NUTDotNetShared.VarFlags.Number);
			charge.Value = "95";

			ups.Variables.Add(charge);

			server.UPSs.Add(ups);

			//var svc = new UPSMonitor();



			//svc.Check();
		}
	}
}
