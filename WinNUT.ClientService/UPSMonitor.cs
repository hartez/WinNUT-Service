using NUTDotNetClient;
using NUTDotNetShared;
using System.Runtime.CompilerServices;
using WinNUT.gPRC;
using WinNUT.gPRC.Services;

namespace WinNUT.ClientService;

public class UnsupportedUPSVariable(string name, VarFlags flags = VarFlags.None) : UPSVariable(name, flags)
{
}

public class UPSMonitor
{
	readonly ClientUPS _ups;
	readonly NUTClient _client;
	private readonly IClientGUIService _clientGUIService;
	private readonly IShutdownService _shutdownService;
	private List<UPSVariable>? _varList;


	public UPSMonitor(NUTClient nutClient, IClientGUIService clientGUIService, IShutdownService shutdownService)
	{
		_client = nutClient;
		_client.Connect();

		var upsList = _client.GetUPSes(true);

		// TODO use id/serial number
		// Take the first UPS if there's only one by default
		// If a serial number is specified use that one
		_ups = upsList.First();
		_clientGUIService = clientGUIService;
		_shutdownService = shutdownService;
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

		var statusVariable = _ups.GetVariable("ups.status", true);
		var status = statusVariable.Value;

		var statusUpdate = new StatusNotification
		{
			Charge = charge,
			Status = status
		};

		// TODO check thresholds and possibly invoke shutdown


		await _clientGUIService.PushStatusUpdate(statusUpdate);

	}

	void InitiateSystemShutdown()
	{
		// TODO the delay should be determined by how much time is left on the battery, etc.
		_shutdownService.InitiateShutdown(600);
	}

	public void Shutdown()
	{
		_clientGUIService.Shutdown();
	}
}
