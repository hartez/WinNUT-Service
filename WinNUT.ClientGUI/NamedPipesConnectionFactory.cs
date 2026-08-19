using System.IO.Pipes;
using System.Security.Principal;

namespace WinNUT.ClientGUI
{
	public class NamedPipesConnectionFactory(string pipeName)
	{
		private readonly string _pipeName = pipeName;

		public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _,
			CancellationToken cancellationToken = default)
		{
			var clientStream = new NamedPipeClientStream(
				serverName: ".",
				pipeName: _pipeName,
				direction: PipeDirection.InOut,
				options: PipeOptions.WriteThrough | PipeOptions.Asynchronous,
				impersonationLevel: TokenImpersonationLevel.Anonymous);

			try
			{
				await clientStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
				return clientStream;
			}
			catch
			{
				clientStream.Dispose();
				throw;
			}
		}
	}
}
