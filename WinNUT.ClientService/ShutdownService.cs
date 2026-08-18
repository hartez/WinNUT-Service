using System.Diagnostics;

namespace WinNUT.ClientService;

public class ShutdownService(ILogger<ShutdownService> logger) : IShutdownService
{
	private readonly ILogger<ShutdownService> _logger = logger;

	public bool ShutdownInitiated => _targetShutdownTime != null;
	public int SecondsUntilShutdown => _targetShutdownTime == null 
		? int.MaxValue 
		: (int)_targetShutdownTime.Value.Subtract(DateTime.Now).TotalSeconds;

	private DateTime? _targetShutdownTime;

	public async Task CancelShutdown()
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
			
			var process = Process.Start(startInfo);

			if(process != null){
				await process.WaitForExitAsync();
			}

			_targetShutdownTime = null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error trying to run cmd");
		}
	}

	public async Task InitiateShutdown(uint delaySeconds)
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
			_targetShutdownTime = DateTime.Now.AddSeconds(delaySeconds);
			
			var process = Process.Start(startInfo);

			if(process != null){
				await process.WaitForExitAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error trying to run cmd");
		}
	}
}
