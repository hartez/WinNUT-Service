namespace WinNUT.gPRC
{
	public interface IClientGUIService
	{
		public void Shutdown();
		public Task PushShutdownNotification();
		public Task PushStatusUpdate(StatusNotification notification);
		public Task PushDeviceInfo(DeviceInfo deviceInfo);
	}
}
