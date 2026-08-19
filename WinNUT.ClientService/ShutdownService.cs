using System.Diagnostics;

namespace WinNUT.ClientService;

public class ShutdownService(ILogger<ShutdownService> logger) : IShutdownService
{
	readonly ILogger<ShutdownService> _logger = logger;

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
			Arguments = $"/c shutdown /a",
			CreateNoWindow = true,
			UseShellExecute = false
		};

		try
		{
			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Aborting shutdown with arguments {args}", startInfo.Arguments);
			}

			var process = Process.Start(startInfo);

			if (process != null)
			{
				await process.WaitForExitAsync();
			}

			_targetShutdownTime = null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error trying to run cmd");
		}
	}

	public async Task InitiateShutdown(int delaySeconds)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = $"/c shutdown /s /t {delaySeconds}",
			CreateNoWindow = true,
			UseShellExecute = false
		};

		try
		{
			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Initiating shutdown with arguments {args}", startInfo.Arguments);
			}

			_targetShutdownTime = DateTime.Now.AddSeconds(delaySeconds);

			var process = Process.Start(startInfo);

			if (process != null)
			{
				await process.WaitForExitAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error trying to run cmd");
		}
	}
}
