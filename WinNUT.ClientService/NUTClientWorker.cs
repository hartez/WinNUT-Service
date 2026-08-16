using NUTDotNetClient;
using WinNUT.gPRC.Services;

namespace WinNUT.ClientService;

public class NUTClientWorker(NUTClientConfig config, ILogger<NUTClientWorker> logger, 
    UPSMonitor upsMonitor) : BackgroundService
{
	private readonly NUTClientConfig _config = config;
	private readonly UPSMonitor _upsMonitor = upsMonitor;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
		var interval = _config.PollingInterval;
        
        await _upsMonitor.GetDeviceInfo();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogWarning("Checking UPS status at: {time}", DateTimeOffset.Now);
            }

			await _upsMonitor.CheckUPSStatus();

			await Task.Delay(interval, stoppingToken);
        }

		_upsMonitor.Shutdown();
    }
}

public record NUTClientConfig(string Host, int Port = 3493, int PollingInterval = 5000); // add serial number
