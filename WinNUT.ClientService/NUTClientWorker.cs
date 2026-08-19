namespace WinNUT.ClientService;

public class NUTClientWorker(ILogger<NUTClientWorker> Logger,
	UPSMonitor UPSMonitor) : BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			if (!UPSMonitor.IsConnected)
			{
				if (await UPSMonitor.Connect()) 
				{
					await UPSMonitor.GetDeviceInfo();
				}
			}

			while (!stoppingToken.IsCancellationRequested && UPSMonitor.IsConnected)
			{
				if (Logger.IsEnabled(LogLevel.Information))
				{
					Logger.LogInformation("Checking UPS status at: {time}", DateTimeOffset.Now);
				}

				await UPSMonitor.CheckUPSStatus();

				await Task.Delay(UPSMonitor.PollingIntervalSeconds * 1000, stoppingToken);
			}

			if (!stoppingToken.IsCancellationRequested)
			{
				// We're here because the monitor became disconnected
				// Wait a bit, then try to reconnect
				await Task.Delay(UPSMonitor.ReconnectIntervalAttemptSeconds * 1000, stoppingToken);
			}
		}

		UPSMonitor.Cleanup();
	}
}