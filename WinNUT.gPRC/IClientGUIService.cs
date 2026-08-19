

namespace WinNUT.gPRC
{
	public interface IClientGUIService
	{
		Func<Task<ShutdownNotification>>? HandleShutdownDelayRequest { get; set; }

		void Cleanup();
		Task PushShutdownNotification(ShutdownInitiated notification);
		Task PushStatusUpdate(DeviceStatus status);
		Task PushDeviceInfo(DeviceInfo deviceInfo);
		Task PushShutdownCanceledNotification(ShutdownCanceled notification);
		Task PushError(Error error);
	}
}
