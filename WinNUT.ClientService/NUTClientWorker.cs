using System.Runtime.CompilerServices;

namespace WinNUT.ClientService;

public class NUTClientWorker(ILogger<NUTClientWorker> logger, 
    UPSMonitor upsMonitor) : BackgroundService
{
	private readonly UPSMonitor _upsMonitor = upsMonitor;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _upsMonitor.GetDeviceInfo();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogWarning("Checking UPS status at: {time}", DateTimeOffset.Now);
            }

			await _upsMonitor.CheckUPSStatus();

			await Task.Delay(_upsMonitor.PollingIntervalSeconds * 1000, stoppingToken);
        }

		_upsMonitor.Shutdown();
    }
}

public record NUTClientConfig(string Host = "localhost", int Port = 3493);

public record MonitorConfig(string UPSSerialNumber = "", int PollingIntervalSeconds = 5, 
    uint ChargeThreshold = 50, uint RemainingThresholdSeconds = 300, uint ShutdownDelaySeconds = 180) 
{
	public uint ChargeThreshold {get; init; } = ChargeThreshold <= 100 ? ChargeThreshold : throw new ArgumentException($"{nameof(ChargeThreshold)} must be 100 or less");
}
