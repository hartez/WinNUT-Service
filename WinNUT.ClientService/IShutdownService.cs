namespace WinNUT.ClientService;

public interface IShutdownService
{
	public Task InitiateShutdown(int delaySeconds);
	public Task CancelShutdown();
	public bool ShutdownInitiated { get; }
	public int SecondsUntilShutdown { get; }
}
