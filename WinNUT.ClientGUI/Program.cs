using Grpc.Net.Client;
using WinNUT.gPRC.Services;

namespace WinNUT.ClientGUI
{
	internal static class Program
	{
		[STAThread]
		static void Main()
		{
			ApplicationConfiguration.Initialize();

			var app = new WinNUTTrayApplication();

			Task.Run(() => app.StartListening());

			Application.Run(app);
		}
	}
}