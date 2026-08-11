using NUTDotNetClient;
using System.Diagnostics;

namespace WinNUT_ClientService;

public class NUTClientWorker(NUTClientConfig config, ILogger<NUTClientWorker> logger) : BackgroundService
{
	private readonly NUTClientConfig _config = config;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = new NUTClient(_config.Host, _config.Port);
        client.Connect();

        var upsList = client.GetUPSes(true);
        var ups = upsList.First();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogWarning("Worker running at: {time}", DateTimeOffset.Now);
            }

            var vars = ups.GetVariables();

            foreach (var v in vars) 
            {
                logger.LogWarning($"{v.Name}: {v.Value}");
            }

			var startInfo = new ProcessStartInfo()
			{
				FileName = "cmd.exe",
				Arguments = $"/K echo \"Hello at {DateTime.Now}\" > C:\\Users\\hartez\\source\\repos\\WinNUTService\\WinNUT-ClientService\\bin\\Release\\net10.0-windows\\publish\\test.txt",
				//Arguments = $"/K shutdown /s /t 600"
			};

			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;

			try
            {
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error trying to run cmd");
            }

            await Task.Delay(120000, stoppingToken);
        }
    }
}

public record NUTClientConfig(string Host, int Port = 3493); // add serial number
// polling interval (seconds)
// add battery time remaining threshold, charge threshold, time threshold

public class UPSMonitor // Action onThresholdReached()
{
    public UPSMonitor(NUTClientConfig config) 
    {
        _client = new NUTClient(config.Host, config.Port);
    }

    NUTClient _client;

    //EnsureConnection()
    // Take the first UPS if there's only one by default
    // If a serial number is specified use that one

    async Task CheckUPSStatus() 
    {
        _client.Connect();

        var upsList = _client.GetUPSes();
        var ups = upsList.First();

        var chargeVariable = ups.GetVariable("battery.charge", true);
        var charge = int.Parse(chargeVariable.Value);


    }
    // Get updated variables
    // check thresholds
    // if one is past, invoke shutdown
}

// need tests with fake server, set variables, invoke check method, ensure shutdown is or isn't happening 