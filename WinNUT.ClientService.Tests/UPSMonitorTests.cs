using NSubstitute;
using NUTDotNetClient;
using NUTDotNetServer;
using WinNUT.ClientService;
using WinNUT.gPRC;

namespace WinNUT_ClientService.Tests
{
	public class UPSMonitorTests
	{
		protected NUTServer TestServer { get; }
		protected NUTClient TestClient { get; }
		protected IClientGUIService ClientGuiService { get; }
		protected IShutdownService ShutdownService { get; }

		public UPSMonitorTests()
		{
			TestServer = new NUTServer(0);
			TestServer.Start();

			// We have to wait until the server is accepting connections before we can do anything with it
			while (!TestServer.IsListening)
			{
				Thread.Sleep(20);
			}

			var ups = new ServerUPS("TestUPS");
			TestServer.UPSs.Add(ups);

			SetTestServerCharge(100);
			SetTestServerStatus("OL");
			SetTestServerBatteryRemainingTime(4092);

			TestClient = new NUTClient("localhost", TestServer.ListenPort);

			ClientGuiService = Substitute.For<IClientGUIService>();
			ShutdownService = Substitute.For<IShutdownService>();
		}

		[Fact]
		public void ChargeThresholdMustBe100OrLess()
		{
			Assert.Throws<ArgumentException>(() => new MonitorConfig(ChargeThreshold: 101));
		}

		[Fact]
		public void CanConfigurePollingInterval()
		{
			var monitor = new UPSMonitor(TestClient, new MonitorConfig(PollingIntervalSeconds: 10), ClientGuiService, ShutdownService);
			Assert.Equal(10, monitor.PollingIntervalSeconds);
		}

		//[Fact]
		//public void CanConfigureUPSSerialNumber()
		//{
		//	var monitor = new UPSMonitor(TestClient, new MonitorConfig(UPSSerialNumber: "8675309"), ClientGuiService, ShutdownService);
		//	Assert.Equal(10, monitor.PollingIntervalSeconds);
		//}


		/* Notification Tests */

		[Fact]
		public async Task SendsStatusOnUPSCheck()
		{
			var monitor = new UPSMonitor(TestClient, new MonitorConfig(), ClientGuiService, ShutdownService);

			await monitor.CheckUPSStatus();

			await ClientGuiService.Received().PushStatusUpdate(Arg.Any<StatusNotification>());
		}

		[Theory]
		
		[InlineData(80, 100, 600, 1000, "OL", false)]
		[InlineData(80, 75, 600, 1000, "OL", false)]
		[InlineData(80, 100, 600, 500, "OL", false)]
		[InlineData(80, 75, 600, 500, "OL", false)]

		[InlineData(80, 100, 600, 1000, "OB", false)]
		[InlineData(80, 75, 600, 1000, "OB", true)]
		[InlineData(80, 100, 600, 500, "OB", true)]
		[InlineData(80, 75, 600, 500, "OB", true)]
		public async Task Shutdown(uint chargeThreshold, uint charge, uint remainingTimeThreshold, uint remainingTime, string status, bool shouldTrigger)
		{
			uint shutdownDelay = 500;
			var config = new MonitorConfig(RemainingThresholdSeconds: remainingTimeThreshold, ChargeThreshold: chargeThreshold, ShutdownDelaySeconds: shutdownDelay);
			var monitor = new UPSMonitor(TestClient, config, ClientGuiService, ShutdownService);

			SetTestServerCharge(charge);
			SetTestServerBatteryRemainingTime(remainingTime);
			SetTestServerStatus(status);

			await monitor.CheckUPSStatus();

			if (shouldTrigger)
			{
				await ClientGuiService.Received().PushShutdownNotification(Arg.Is<ShutdownInitiatedNotification>(n => n.Remaining == shutdownDelay));
				ShutdownService.Received().InitiateShutdown(Arg.Is<uint>(500));
			}
			else
			{
				await ClientGuiService.DidNotReceive().PushShutdownNotification(Arg.Any<ShutdownInitiatedNotification>());
				ShutdownService.DidNotReceive().InitiateShutdown(Arg.Any<uint>());
			}
		}

		/* Test Helpers */

		void SetTestServerVariable(string ups, string name, string value, NUTDotNetShared.VarFlags flags)
		{
			var device = TestServer.GetUPSByName(ups);

			if (device.Variables.Any(v => v.Name == name))
			{
				var variable = device.GetVariableByName(name);
				if (variable != null)
				{
					TestServer.GetUPSByName(ups).Variables.Remove(variable);
				}
			}

			device.Variables.Add(
				new NUTDotNetShared.UPSVariable(name, flags)
				{
					Value = value
				});
		}

		void SetTestServerCharge(uint charge, string ups = "TestUPS")
		{
			SetTestServerVariable(ups, "battery.charge", charge.ToString(), NUTDotNetShared.VarFlags.Number);
		}

		void SetTestServerStatus(string status, string ups = "TestUPS")
		{
			SetTestServerVariable(ups, "ups.status", status, NUTDotNetShared.VarFlags.String);
		}

		void SetTestServerBatteryRemainingTime(uint remaining, string ups = "TestUPS")
		{
			SetTestServerVariable(ups, "battery.runtime", remaining.ToString(), NUTDotNetShared.VarFlags.Number);
		}
	}
}
