namespace WinNUT.ClientService;

public interface IShutdownService
{
	public Task InitiateShutdown(uint delaySeconds);
	public Task CancelShutdown();
	public bool ShutdownInitiated { get; }
	public int SecondsUntilShutdown { get; }
}
