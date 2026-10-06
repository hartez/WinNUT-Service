using Grpc.Core;

namespace WinNUT.gPRC.Services
{
	public class ClientGUIService(ILogger<ClientGUIService> logger) : ClientGUI.ClientGUIBase, IClientGUIService
	{
		IServerStreamWriter<Notification>? _responseStream;
		readonly CancellationTokenSource _cancellationTokenSource = new();
		readonly ILogger<ClientGUIService> _logger = logger;
		DeviceStatus? _lastStatusUpdate;
		Error? _lastError;
		DeviceInfo? _deviceInfo;

		public Func<Task<ShutdownNotification>>? HandleShutdownDelayRequest { get; set; }

		public override async Task Subscribe(SubscribeRequest request, IServerStreamWriter<Notification> responseStream, ServerCallContext context)
		{
			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Received subscription request from {subscriber}", request.Subscriber);
			}

			_responseStream = responseStream;

			// If we already have device info, give that to the client
			if (_deviceInfo != null)
			{
				await PushDeviceInfo(_deviceInfo);
			}

			// Go ahead and push our latest update (or error) to the newly connected client
			if (_lastStatusUpdate != null)
			{
				await PushStatusUpdate(_lastStatusUpdate);
			}
			else if (_lastError != null)
			{
				await PushError(_lastError);
			}

			while (!context.CancellationToken.IsCancellationRequested && !_cancellationTokenSource.IsCancellationRequested)
			{
				await Task.Delay(1000, _cancellationTokenSource.Token);
			}

			_responseStream = null;

			return;
		}

		public override async Task<ShutdownNotification> RequestShutdownDelay(DelayRequest request, ServerCallContext context)
		{
			if (HandleShutdownDelayRequest != null)
			{
				return await HandleShutdownDelayRequest.Invoke();
			}

			return new ShutdownNotification { ShutDownCanceled = new ShutdownCanceled() };
		}

		public async Task PushShutdownNotification(ShutdownInitiated notification)
		{
			if (_responseStream == null)
			{
				return;
			}

			await _responseStream.WriteAsync(new Notification() { ShutdownInitiated = notification });
		}

		public async Task PushShutdownCanceledNotification(ShutdownCanceled notification)
		{
			if (_responseStream == null)
			{
				return;
			}

			await _responseStream.WriteAsync(new Notification() { ShutDownCanceled = notification });
		}

		public async Task PushStatusUpdate(DeviceStatus status)
		{
			_lastStatusUpdate = status;
			_lastError = null;

			if (_responseStream == null)
			{
				return;
			}

			await _responseStream.WriteAsync(new Notification() { DeviceStatus = status });
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

		public void Cleanup()
		{
			_cancellationTokenSource.Cancel();
		}

		public async Task PushError(Error error)
		{
			_lastError = error;
			_lastStatusUpdate = null;

			if (_responseStream == null)
			{
				return;
			}

			await _responseStream.WriteAsync(new Notification() { Error = error });
		}
	}
}
