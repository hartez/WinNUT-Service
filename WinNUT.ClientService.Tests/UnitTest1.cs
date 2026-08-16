using NSubstitute;
using NUTDotNetClient;
using NUTDotNetServer;
using WinNUT.ClientService;
using WinNUT.gPRC;

namespace WinNUT_ClientService.Tests
{
	public class UnitTest1
	{
		[Fact]
		public async Task Test1()
		{
			var testServer = new NUTServer(0);
			testServer.Start();
			// Just like in the server tests - we need to wait for the TcpListener to find a port before we can use it.
			while (!testServer.IsListening)
			{
				System.Threading.Thread.Sleep(20);
			}

			var ups = new ServerUPS("TestUPS");
			var charge = new NUTDotNetShared.UPSVariable("battery.charge", NUTDotNetShared.VarFlags.Number);
			charge.Value = "95";
			ups.Variables.Add(charge);
			testServer.UPSs.Add(ups);

			var testClient = new NUTClient("localhost", testServer.ListenPort);

			var clientGuiService = Substitute.For<IClientGUIService>();
			var shutdownService = Substitute.For<IShutdownService>();

			var svc = new UPSMonitor(testClient, clientGuiService, shutdownService);

			await svc.CheckUPSStatus();

			await clientGuiService.Received().PushStatusUpdate(Arg.Any<StatusNotification>());
		}
	}
}
