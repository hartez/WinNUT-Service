using System.Diagnostics;

namespace WinNUT.ClientService;

public class ShutdownService : IShutdownService
{
	private readonly ILogger<ShutdownService> _logger;

	public ShutdownService(ILogger<ShutdownService> logger)
	{
		_logger = logger;
	}

	public void CancelShutdown()
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = $"/K shutdown /a",
			CreateNoWindow = true,
			UseShellExecute = false
		};

		try
		{
			_logger.LogInformation($"Aborting shutdown with arguments {startInfo.ArgumentList}");
			Process.Start(startInfo);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error trying to run cmd");
		}
	}

	public void InitiateShutdown(int delaySeconds)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = $"/K shutdown /s /t {delaySeconds}",
			CreateNoWindow = true,
			UseShellExecute = false
		};

		try
		{
			_logger.LogInformation($"Initiating shutdown with arguments {startInfo.ArgumentList}");
			Process.Start(startInfo);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error trying to run cmd");
		}
	}
}
