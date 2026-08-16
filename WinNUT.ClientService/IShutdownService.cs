namespace WinNUT.ClientService;

public interface IShutdownService 
{
	public void InitiateShutdown(int delaySeconds);
	public void CancelShutdown();
}
