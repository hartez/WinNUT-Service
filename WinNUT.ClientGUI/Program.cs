namespace WinNUT.ClientGUI
{
	internal static class Program
	{
		[STAThread]
		static void Main()
		{
			ApplicationConfiguration.Initialize();

			using var app = new WinNUTTrayApplication();

			Task.Run(() => app.Subscribe());

			Application.Run(app);
		}
	}
}