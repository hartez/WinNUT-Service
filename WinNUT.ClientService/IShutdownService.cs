namespace WinNUT.ClientService;

public interface IShutdownService 
{
	public void InitiateShutdown(uint delaySeconds);
	public void CancelShutdown();
}
