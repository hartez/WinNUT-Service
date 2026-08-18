using Grpc.Core;
using Grpc.Net.Client;
using System.Text;
using WinNUT.gPRC;

namespace WinNUT.ClientGUI
{
	public class WinNUTTrayApplication : ApplicationContext
	{
		private bool _disposed;
		private readonly NotifyIcon _trayIcon;
		CancellationTokenSource _cancellationTokenSource = new();
		StatusNotification? _latestStatus;
		DeviceInfo? _deviceInfo;

		gPRC.ClientGUI.ClientGUIClient? _client;

		public WinNUTTrayApplication()
		{
			var menu = new ContextMenuStrip();
			menu.Items.Add("UPS Info", null, (_, _) => ShowDeviceInfo());
			menu.Items.Add("Exit", null, (_, _) => ExitApplication());

			_trayIcon = new NotifyIcon
			{
				Icon = Resources.battery, // TODO Update icons based on UPS status/charge
				Text = "UPS Status Unknown",
				ContextMenuStrip = menu,
				Visible = true
			};

			_trayIcon.DoubleClick += (_, _) => ShowDeviceInfo();
		}

		public async Task StartListening()
		{
			var token = _cancellationTokenSource.Token;

			using var channel = CreateChannel();
			_client = new gPRC.ClientGUI.ClientGUIClient(channel);
			var stream = _client.Subscribe(new SubscribeRequest { Subscriber = "WinNUT.ClientGUI Tray Application" });

			await foreach (var notification in stream.ResponseStream.ReadAllAsync(token))
			{

				switch (notification.NotificationTypeCase)
				{
					case Notification.NotificationTypeOneofCase.None:
						break;
					case Notification.NotificationTypeOneofCase.ShutdownInitiated:
						await ShowShutdownNotification(notification.ShutdownInitiated);
						break;
					case Notification.NotificationTypeOneofCase.Status:
						_latestStatus = notification.Status;
						UpdateIcon(_latestStatus);
						break;
					case Notification.NotificationTypeOneofCase.DeviceInfo:
						UpdateDeviceInfo(notification.DeviceInfo);
						break;
				}
			}
		}

		private void UpdateDeviceInfo(DeviceInfo deviceInfo)
		{
			_deviceInfo = deviceInfo;
		}

		private void UpdateIcon(StatusNotification statusNotification)
		{
			if (statusNotification == null)
			{
				_trayIcon.Text = "UPS Status Unknown";
				_trayIcon.Icon = Resources.battery;
				return;
			}

			_trayIcon.Icon = Resources.battery;
			_trayIcon.Text = statusNotification.ToDisplay();
		}

		private void ExitApplication()
		{
			_cancellationTokenSource.Cancel();
			_trayIcon.Visible = false;
			Application.Exit();
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (disposing)
			{
				_cancellationTokenSource.Cancel();
				_trayIcon.Visible = false;
			}

			base.Dispose(disposing);
		}

		private async Task ShowShutdownNotification(ShutdownInitiatedNotification notification)
		{
			if (notification.AllowDelayRequest)
			{
				var result = MessageBox.Show("Shutdown has started. Wanna delay it?", "Shutdown Initiated",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2,
					MessageBoxOptions.ServiceNotification);

				if (result == DialogResult.Yes && _client != null)
				{
					await _client.RequestShutdownDelayAsync(new DelayRequest());
				}

				// TODO while there's a pending shutdown, show "request delay" in menu
			}
			else
			{
				MessageBox.Show("Shutdown has started.", "Shutdown Initiated",
					MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
					MessageBoxOptions.ServiceNotification);
			}
		}

		private void ShowDeviceInfo()
		{
			StringBuilder sb = new();

			if (_latestStatus != null)
			{
				sb.Append($"Battery: {_latestStatus.Charge}%\n");
				sb.Append($"Battery Time Remaining: {(int)(_latestStatus.BatterySecondsRemaining / 60)} minutes\n");
				sb.Append($"UPS Status: {StatusHelpers.TranslateStatus(_latestStatus.Status, "\n")}\n");
			}

			if (_deviceInfo != null)
			{
				foreach (var attr in _deviceInfo.DeviceAttributes)
				{
					sb.Append($"{attr.Key}: {attr.Value}\n");
				}
			}
			else
			{
				sb.Append("Device Information Unavailable");
			}

			MessageBox.Show(sb.ToString(), "UPS Info");
		}

		public static GrpcChannel CreateChannel()
		{
			var connectionFactory = new NamedPipesConnectionFactory(Values.PipeName);
			var socketsHttpHandler = new SocketsHttpHandler
			{
				ConnectCallback = connectionFactory.ConnectAsync
			};

			return GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
			{
				HttpHandler = socketsHttpHandler
			});
		}
	}
}
