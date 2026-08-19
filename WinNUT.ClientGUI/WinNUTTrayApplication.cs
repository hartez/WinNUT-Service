using Grpc.Core;
using Grpc.Net.Client;
using System.Text;
using WinNUT.gPRC;

namespace WinNUT.ClientGUI
{
	public class WinNUTTrayApplication : ApplicationContext
	{
		bool _disposed;

		readonly NotifyIcon _trayIcon;
		readonly ToolStripItem _requestDelay;

		readonly CancellationTokenSource _cancellationTokenSource = new();

		DeviceInfo? _deviceInfo;
		DeviceStatus? _latestStatus;
		Error? _latestError;

		bool _mayRequestDelay = false;

		gPRC.ClientGUI.ClientGUIClient? _client;

		readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

		public WinNUTTrayApplication()
		{
			var menu = new ContextMenuStrip();
			menu.Items.Add("UPS Info", null, (_, _) => ShowDeviceInfo());

			_requestDelay = menu.Items.Add("Request Delay", null, async (_, _) => await RequestDelay());
			_requestDelay.Visible = _mayRequestDelay;

			menu.Items.Add("Exit", null, (_, _) => ExitApplication());

			_trayIcon = new NotifyIcon
			{
				Icon = Resources.battery_unknown,
				Text = "UPS Status Unknown",
				ContextMenuStrip = menu,
				Visible = true
			};

			_trayIcon.DoubleClick += (_, _) => ShowDeviceInfo();
		}

		public async Task Listen()
		{
			var token = _cancellationTokenSource.Token;

			using var channel = CreateChannel();
			_client = new gPRC.ClientGUI.ClientGUIClient(channel);
			using var stream = _client.Subscribe(new SubscribeRequest { Subscriber = "WinNUT.ClientGUI Tray Application" });
			
			await foreach (var notification in stream.ResponseStream.ReadAllAsync(token))
			{
				switch (notification.NotificationTypeCase)
				{
					case Notification.NotificationTypeOneofCase.None:
						break;
					case Notification.NotificationTypeOneofCase.ShutdownInitiated:
						await ShowShutdownNotification(notification.ShutdownInitiated);
						break;
					case Notification.NotificationTypeOneofCase.DeviceStatus:
						UpdateStatus(notification.DeviceStatus);
						break;
					case Notification.NotificationTypeOneofCase.DeviceInfo:
						UpdateDeviceInfo(notification.DeviceInfo);
						break;
					case Notification.NotificationTypeOneofCase.ShutDownCanceled:
						UpdateDelayable(false);
						break;
					case Notification.NotificationTypeOneofCase.Error:
						UpdateError(notification.Error);
						break;
				}
			}

			return;
		}

		void UpdateUI()
		{
			_synchronizationContext.Post(_ =>
			{
				UpdateIcon();
				UpdateToolTip();
				UpdateMenu();
			}, null);
		}

		void UpdateDeviceInfo(DeviceInfo deviceInfo)
		{
			_deviceInfo = deviceInfo;
		}

		void UpdateDelayable(bool mayRequestDelay)
		{
			_mayRequestDelay = mayRequestDelay;

			UpdateUI();
		}

		void UpdateError(Error error)
		{
			_latestStatus = null;
			_latestError = error;

			_mayRequestDelay = false;

			UpdateUI();
		}

		void UpdateStatus(DeviceStatus status)
		{
			_latestStatus = status;
			_latestError = null;

			_mayRequestDelay = !status.IsOnline();

			UpdateUI();
		}

		void UpdateMenu()
		{
			_requestDelay.Visible = _mayRequestDelay;
		}

		void UpdateToolTip()
		{
			if (_latestStatus == null)
			{
				if (_latestError == null)
				{
					_trayIcon.Text = "UPS Status Unknown";
				}
				else
				{
					_trayIcon.Text = "An error occurred connecting to the UPS. Double click for details.";
				}

				return;
			}

			_trayIcon.Text = _latestStatus.ToDisplay();
		}

		void UpdateIcon()
		{
			if (_latestStatus == null)
			{
				_trayIcon.Icon = Resources.battery_unknown;
				return;
			}

			var charge = _latestStatus.Charge;

			if (_latestStatus.IsOnBattery())
			{
				_trayIcon.Icon = charge switch
				{
					>= 95 => Resources.battery_100_ob,
					>= 75 => Resources.battery_75_ob,
					>= 50 => Resources.battery_50_ob,
					>= 25 => Resources.battery_25_ob,
					_ => Resources.battery_0_ob
				};
			}
			else
			{
				_trayIcon.Icon = charge switch
				{
					>= 95 => Resources.battery_100_ol,
					>= 75 => Resources.battery_75_ol,
					>= 50 => Resources.battery_50_ol,
					>= 25 => Resources.battery_25_ol,
					_ => Resources.battery_0_ol
				};
			}
		}

		void ExitApplication()
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

		async Task RequestDelay()
		{
			if (_client == null) { return; }

			var updatedShutdownInfo = await _client.RequestShutdownDelayAsync(new DelayRequest());

			if (!updatedShutdownInfo.ShutdownInitiated.AllowDelayRequest)
			{
				UpdateDelayable(false);
			}
		}

		async Task ShowShutdownNotification(ShutdownInitiated notification)
		{
			if (notification.AllowDelayRequest)
			{
				var result = MessageBox.Show("Currently on battery power; system shutdown has started. Would you like to delay it?", "Shutdown Initiated",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2,
					MessageBoxOptions.ServiceNotification);

				if (result == DialogResult.Yes && _client != null)
				{
					await RequestDelay();
				}
				else
				{
					UpdateDelayable(true);
				}
			}
			else
			{
				MessageBox.Show("Currently on battery power; system shutdown has started.", "Shutdown Initiated",
					MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1,
					MessageBoxOptions.ServiceNotification);
			}
		}

		void ShowDeviceInfo()
		{
			StringBuilder sb = new();

			if (_latestStatus != null)
			{
				sb.Append($"Battery: {_latestStatus.Charge}%\n");
				sb.Append($"Battery Time Remaining: {(int)(_latestStatus.BatterySecondsRemaining / 60)} minutes\n");
				sb.Append($"UPS Status: {StatusHelpers.TranslateStatus(_latestStatus.Flags, "\n")}\n");
			}
			else if (_latestError != null)
			{
				sb.Append($"Error: {_latestError.Description}\n");
			}

			if (_deviceInfo != null)
			{
				sb.Append("Device Information:\n");
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
