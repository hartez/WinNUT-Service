namespace WinNUT.gPRC
{
	public interface IClientGUIService
	{
		void Shutdown();
		Task PushShutdownNotification(ShutdownNotification notification);
		Task PushStatusUpdate(StatusNotification notification);
		Task PushDeviceInfo(DeviceInfo deviceInfo);
	}
}
