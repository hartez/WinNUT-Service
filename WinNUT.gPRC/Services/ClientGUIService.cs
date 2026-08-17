using Grpc.Core;

namespace WinNUT.gPRC.Services
{
	public class ClientGUIService : ClientGUI.ClientGUIBase, IClientGUIService
	{
		private IServerStreamWriter<Notification>? _responseStream;
		private readonly CancellationTokenSource _cancellationTokenSource = new();
		private readonly ILogger<ClientGUIService> _logger;
		private StatusNotification? _lastStatusUpdate;
		private DeviceInfo? _deviceInfo;

		public ClientGUIService(ILogger<ClientGUIService> logger)
		{
			_logger = logger;
		}

		public override async Task Subscribe(SubscribeRequest request, IServerStreamWriter<Notification> responseStream, ServerCallContext context)
		{
			_logger.LogInformation($"Received subscription request from {request.Subscriber}");

			_responseStream = responseStream;

			// If we already have device info, give that to the client
			if (_deviceInfo != null)
			{
				await PushDeviceInfo(_deviceInfo);
			}

			// Go ahead and push our latest update to the newly connected client
			if (_lastStatusUpdate != null)
			{
				await PushStatusUpdate(_lastStatusUpdate);
			}

			while (!context.CancellationToken.IsCancellationRequested && !_cancellationTokenSource.IsCancellationRequested)
			{
				await Task.Delay(100, _cancellationTokenSource.Token);
			}

			return;
		}

		public override Task<ShutdownNotification> RequestShutdownDelay(DelayRequest request, ServerCallContext context)
		{
			return base.RequestShutdownDelay(request, context);
		}

		public async Task PushShutdownNotification(ShutdownNotification notification)
		{
			if (_responseStream == null) 
			{ 
				return; 
			}

			await _responseStream.WriteAsync(new Notification() { ShutDown = notification });
		}

		public async Task PushStatusUpdate(StatusNotification notification)
		{
			_lastStatusUpdate = notification;

			if (_responseStream == null)
			{
				return;
			}

			await _responseStream.WriteAsync(new Notification() { Status = notification });
		}

		public async Task PushDeviceInfo(DeviceInfo deviceInfo)
		{
			_deviceInfo = deviceInfo;

			if (_responseStream == null)
			{
				return;
			}

			await _responseStream.WriteAsync(new Notification() { DeviceInfo = deviceInfo });
		}

		public void Shutdown()
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
