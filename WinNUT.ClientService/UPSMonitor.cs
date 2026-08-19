using NUTDotNetClient;
using NUTDotNetShared;
using System.Runtime.CompilerServices;
using WinNUT.gPRC;

namespace WinNUT.ClientService;

public class UPSMonitor
{
	ClientUPS? _ups;
	readonly NUTClient _nutClient;
	readonly MonitorConfig _config;
	readonly IClientGUIService _clientGUIService;
	readonly IShutdownService _shutdownService;
	readonly ILogger<UPSMonitor> _logger;
	List<UPSVariable>? _varList;

	public int PollingIntervalSeconds => _config.PollingIntervalSeconds;
	public int ReconnectIntervalAttemptSeconds => _config.ReconnectAttemptIntervalSeconds;

	public bool IsConnected { get; set; }

	public UPSMonitor(NUTClient nutClient, MonitorConfig config, IClientGUIService clientGUIService, IShutdownService shutdownService, ILogger<UPSMonitor> logger)
	{
		_shutdownService = shutdownService;
		_logger = logger;
		_clientGUIService = clientGUIService;
		_clientGUIService.HandleShutdownDelayRequest = ShutdownDelayRequested;

		_nutClient = nutClient;
		_config = config;
	}

	public async Task<bool> Connect()
	{
		try
		{
			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Attempting to connect to NUT server at {address}", _nutClient.Host);
			}

			IsConnected = false;

			if (!_nutClient.IsConnected)
			{
				_nutClient.Connect();
			}

			var upsList = _nutClient.GetUPSes(true);

			if (upsList.Count == 0)
			{
				_ups = null;
				IsConnected = false;

				throw new Exception("NUT server connected, but the UPS list was empty.");
			}

			var targetSerialNumber = _config.UPSSerialNumber;

			if (string.IsNullOrEmpty(targetSerialNumber))
			{
				_ups = upsList.First();
			}
			else
			{
				FindUPSBySerialNumber(targetSerialNumber, upsList);
			}

			IsConnected = true;

			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Successfully connected to NUT server at {address}", _nutClient.Host);
			}

			return true;
		}
		catch (Exception ex)
		{
			await HandleError(ex);
		}

		return false;
	}

	void FindUPSBySerialNumber(string serialNumber, List<ClientUPS> upsList)
	{
		const string serialNumberVariable = "device.serial";

		foreach (var ups in upsList)
		{
			var variables = ups.GetVariables(forceUpdate: true);

			if (variables.Any(v => v.Name == serialNumberVariable))
			{
				var upsSerialNumber = ups.GetVariableByName(serialNumberVariable).Value;

				if (_logger.IsEnabled(LogLevel.Information))
				{
					_logger.LogInformation("Found a UPS with serial number {number}", upsSerialNumber);
				}

				if (upsSerialNumber == serialNumber)
				{
					_ups = ups;
					break;
				}
			}
		}

		if (_ups == null)
		{
			throw new Exception($"NUT server connected, but the UPS with serial number {serialNumber} was not found.");
		}
	}

	public async Task GetDeviceInfo()
	{
		try
		{
			if (!IsConnected)
			{
				throw (new InvalidOperationException("Not connected"));
			}

			if (_ups == null)
			{
				throw new InvalidOperationException("UPS not available");
			}

			_varList ??= [.. _ups.GetVariables(true)];

			var result = new Dictionary<string, string>();

			foreach (var upsVar in _varList.Where(v => v.Name.StartsWith("device.")))
			{
				var withValue = _ups.GetVariable(upsVar.Name, true);
				result[withValue.Name] = withValue.Value;
			}

			DeviceInfo deviceInfo = new();
			deviceInfo.DeviceAttributes.Add(result);

			await _clientGUIService.PushDeviceInfo(deviceInfo);
		}
		catch (Exception ex)
		{
			await HandleError(ex);
		}
	}

	public async Task CheckUPSStatus()
	{
		try
		{
			if (!IsConnected)
			{
				throw (new InvalidOperationException("Not connected"));
			}

			if (_ups == null)
			{
				throw new InvalidOperationException("UPS not available");
			}

			var chargeVariable = _ups.GetVariable("battery.charge", true);
			var charge = int.Parse(chargeVariable.Value);

			var runtimeVariable = _ups.GetVariable("battery.runtime", true);
			var runtime = int.Parse(runtimeVariable.Value);

			var statusVariable = _ups.GetVariable("ups.status", true);
			var statusFlags = statusVariable.Value;

			var statusUpdate = new DeviceStatus
			{
				Charge = charge,
				Flags = statusFlags,
				BatterySecondsRemaining = runtime
			};

			await _clientGUIService.PushStatusUpdate(statusUpdate);

			if (statusUpdate.IsOnline())
			{
				if (_shutdownService.ShutdownInitiated)
				{
					// We're back online, we can cancel the pending shutdown
					await _shutdownService.CancelShutdown();
					await _clientGUIService.PushShutdownCanceledNotification(new ShutdownCanceled());
				}

				// On mains power, no need to worry
				return;
			}

			if (_shutdownService.ShutdownInitiated)
			{
				// No need to check the shutdown thresholds, we're already in the midst of one
				return;
			}

			// Check the thresholds to see if we need to initiate a shutdown
			if (charge < _config.ChargeThreshold || runtime < _config.RemainingThresholdSeconds)
			{
				var shutdownDelay = _config.ShutdownDelaySeconds;

				var notification = new ShutdownInitiated() { Remaining = shutdownDelay, AllowDelayRequest = true };
				await _shutdownService.InitiateShutdown(shutdownDelay);
				await _clientGUIService.PushShutdownNotification(notification);
			}
		}
		catch (Exception ex)
		{
			await HandleError(ex);
		}
	}

	async Task HandleError(Exception ex, [CallerMemberName] string callerName = "")
	{
		IsConnected = false;

		if (_logger.IsEnabled(LogLevel.Error))
		{
			_logger.LogError("Error in {method}: {message}", callerName, ex);
		}

		await _clientGUIService.PushError(new Error() { Description = ex.Message });
	}

	async Task<ShutdownNotification> ShutdownDelayRequested()
	{
		if (!_shutdownService.ShutdownInitiated)
		{
			return new ShutdownNotification { ShutDownCanceled = new ShutdownCanceled() };
		}

		var timeremaining = _shutdownService.SecondsUntilShutdown;
		var shutdownDelay = _config.ShutdownDelaySeconds + timeremaining;

		await _shutdownService.CancelShutdown();

		var notification = new ShutdownInitiated() { Remaining = shutdownDelay, AllowDelayRequest = false };
		await _shutdownService.InitiateShutdown(shutdownDelay);

		return new ShutdownNotification { ShutdownInitiated = notification };
	}

	public void Cleanup()
	{
		try
		{
			if (_nutClient.IsConnected)
			{
				_nutClient.Disconnect();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError("Error disconnecting: {message}", ex.Message);
		}

		_clientGUIService.HandleShutdownDelayRequest = null;
		_clientGUIService.Cleanup();
	}
}

