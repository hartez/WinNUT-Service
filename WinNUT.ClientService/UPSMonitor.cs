using NUTDotNetClient;
using NUTDotNetShared;
using WinNUT.gPRC;

namespace WinNUT.ClientService;

public class UPSMonitor
{
	readonly ClientUPS _ups;
	readonly NUTClient _client;
	private readonly MonitorConfig _config;
	private readonly IClientGUIService _clientGUIService;
	private readonly IShutdownService _shutdownService;
	private List<UPSVariable>? _varList;

	public int PollingIntervalSeconds => _config.PollingIntervalSeconds;

	public UPSMonitor(NUTClient nutClient, MonitorConfig config, IClientGUIService clientGUIService, IShutdownService shutdownService)
	{
		_client = nutClient;
		_config = config;
		if (!_client.IsConnected)
		{
			_client.Connect();
		}

		var upsList = _client.GetUPSes(true);

		// TODO use id/serial number
		// Take the first UPS if there's only one by default
		// If a serial number is specified use that one
		_ups = upsList.First();
		_clientGUIService = clientGUIService;
		_shutdownService = shutdownService;

		_clientGUIService.HandleShutdownDelayRequest = ShutdownDelayRequested;
	}

	private async Task<ShutdownNotification> ShutdownDelayRequested()
	{
		if(!_shutdownService.ShutdownInitiated)
		{
			return new ShutdownNotification{ ShutDownCanceled = new ShutdownCanceledNotification() };
		}

		var timeremaining = _shutdownService.SecondsUntilShutdown;
		var shutdownDelay = (uint)(TimeSpan.FromMinutes(3).Seconds + timeremaining);

		await _shutdownService.CancelShutdown();
		
		var notification = new ShutdownInitiatedNotification() { Remaining = shutdownDelay, AllowDelayRequest = false };
		await _shutdownService.InitiateShutdown(shutdownDelay);

		return new ShutdownNotification{ ShutdownInitiated = notification };
	}

	public async Task GetDeviceInfo()
	{
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

	public async Task CheckUPSStatus()
	{
		var chargeVariable = _ups.GetVariable("battery.charge", true);
		var charge = uint.Parse(chargeVariable.Value);

		var runtimeVariable = _ups.GetVariable("battery.runtime", true);
		var runtime = uint.Parse(runtimeVariable.Value);

		var statusVariable = _ups.GetVariable("ups.status", true);
		var status = statusVariable.Value;

		var statusUpdate = new StatusNotification
		{
			Charge = charge,
			Status = status,
			BatterySecondsRemaining = runtime
		};

		await _clientGUIService.PushStatusUpdate(statusUpdate);

		var statusFlags = StatusHelpers.GetStatusFlags(status);

		if(statusFlags.Contains("OL"))
		{
			if(_shutdownService.ShutdownInitiated)
			{
				// We're back online, we can cancel the pending shutdown
				await _shutdownService.CancelShutdown();
			}

			// On mains power, no need to worry
			return;
		}

		// Check the thresholds to see if we need to initiate a shutdown
		if (charge < _config.ChargeThreshold || runtime < _config.RemainingThresholdSeconds)
		{
			uint shutdownDelay = _config.ShutdownDelaySeconds;

			var notification = new ShutdownInitiatedNotification() { Remaining = shutdownDelay, AllowDelayRequest = true };
			await _shutdownService.InitiateShutdown(shutdownDelay);
			await _clientGUIService.PushShutdownNotification(notification);
		}
	}

	public void Shutdown()
	{
		_clientGUIService.HandleShutdownDelayRequest = null;
		_clientGUIService.Shutdown();
	}
}
