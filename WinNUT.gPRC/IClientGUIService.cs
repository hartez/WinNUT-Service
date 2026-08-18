

namespace WinNUT.gPRC
{
	public interface IClientGUIService
	{
		Func<Task<ShutdownNotification>>? HandleShutdownDelayRequest { get; set; }

		void Shutdown();
		Task PushShutdownNotification(ShutdownInitiatedNotification notification);
		Task PushStatusUpdate(StatusNotification notification);
		Task PushDeviceInfo(DeviceInfo deviceInfo);
		Task PushShutdownCanceledNotification(ShutdownCanceledNotification notification);
	}
}
