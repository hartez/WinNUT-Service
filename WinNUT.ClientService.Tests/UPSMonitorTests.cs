using Microsoft.Extensions.Logging;
using NSubstitute;
using NUTDotNetClient;
using WinNUT.ClientService;
using WinNUT.gPRC;
using WinNUT.Simulator;

namespace WinNUT_ClientService.Tests
{
	public class UPSMonitorTests
	{
		protected TestServer TestServer { get; }
		protected NUTClient TestClient { get; }
		protected IClientGUIService ClientGuiService { get; }
		protected IShutdownService ShutdownService { get; }
		protected ILogger<UPSMonitor> Logger { get; }

		public UPSMonitorTests()
		{
			TestServer = new(0);
			TestServer.StartAndWaitUntilListening();

			TestServer.Charge = 100;
			TestServer.Status = "OL";
			TestServer.BatteryRemainingTime = 4092;

			TestClient = new NUTClient("localhost", TestServer.ListenPort);

			ClientGuiService = Substitute.For<IClientGUIService>();
			ShutdownService = Substitute.For<IShutdownService>();
			Logger = Substitute.For<ILogger<UPSMonitor>>();
		}

		[Fact]
		public void ChargeThresholdMustBe100OrLess()
		{
			Assert.Throws<ArgumentException>(() => new MonitorConfig(ChargeThreshold: 101));
		}

		[Fact]
		public void CanConfigurePollingInterval()
		{
			var monitor = new UPSMonitor(TestClient, new MonitorConfig(PollingIntervalSeconds: 10), ClientGuiService, ShutdownService, Logger);
			Assert.Equal(10, monitor.PollingIntervalSeconds);
		}

		/* Notification Tests */

		[Fact]
		public async Task SendsStatusOnUPSCheck()
		{
			var monitor = new UPSMonitor(TestClient, new MonitorConfig(), ClientGuiService, ShutdownService, Logger);

			await monitor.CheckUPSStatus();

			await ClientGuiService.Received().PushStatusUpdate(Arg.Any<DeviceStatus>());
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
		public async Task InitiatesShutdownWhenConditionsAreMet(int chargeThreshold, int charge, int remainingTimeThreshold, int remainingTime, string status, bool shutdownExpected)
		{
			int shutdownDelay = 500;
			var config = new MonitorConfig(RemainingThresholdSeconds: remainingTimeThreshold, ChargeThreshold: chargeThreshold, ShutdownDelaySeconds: shutdownDelay);
			var monitor = new UPSMonitor(TestClient, config, ClientGuiService, ShutdownService, Logger);

			TestServer.Charge = charge;
			TestServer.BatteryRemainingTime = remainingTime;
			TestServer.Status = status;

			await monitor.CheckUPSStatus();

			if (shutdownExpected)
			{
				await ClientGuiService.Received().PushShutdownNotification(Arg.Is<ShutdownInitiated>(n => n.Remaining == shutdownDelay));
				await ShutdownService.Received().InitiateShutdown(Arg.Is<int>(shutdownDelay));
			}
			else
			{
				await ClientGuiService.DidNotReceive().PushShutdownNotification(Arg.Any<ShutdownInitiated>());
				await ShutdownService.DidNotReceive().InitiateShutdown(Arg.Any<int>());
			}
		}
	}
}
