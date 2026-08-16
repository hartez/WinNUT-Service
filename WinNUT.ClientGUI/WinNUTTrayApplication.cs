using Grpc.Core;
using Grpc.Net.Client;
using System.Text;
using WinNUT.gPRC;

namespace WinNUT.ClientGUI
{
	public class WinNUTTrayApplication : ApplicationContext
	{
		private readonly NotifyIcon _trayIcon;
		CancellationTokenSource _cancellationTokenSource = new();
		StatusNotification? _latestStatus;
		DeviceInfo? _deviceInfo;

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
			var client = new gPRC.ClientGUI.ClientGUIClient(channel);
			var stream = client.Subscribe(new SubscribeRequest { Subscriber = "WinNUT.ClientGUI Tray Application" });

			await foreach (var notification in stream.ResponseStream.ReadAllAsync(token))
			{

				switch (notification.NotificationTypeCase)
				{
					case Notification.NotificationTypeOneofCase.None:
						break;
					case Notification.NotificationTypeOneofCase.ShutDown:
						ShowShutdownNotification();
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

		private async void ShowShutdownNotification()
		{
			// TODO convert this to a notification about shutdown with an option to delay it
			MessageBox.Show("Message about shutdown", "Shutdown Initiated");
		}

		private async void ShowUPSStatus()
		{
			MessageBox.Show("Some status info, eventually?", "UPS Status");
		}

		private void ShowDeviceInfo()
		{
			StringBuilder sb = new();

			if (_latestStatus != null)
			{
				sb.Append($"Battery: {_latestStatus.Charge}%\n");
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
