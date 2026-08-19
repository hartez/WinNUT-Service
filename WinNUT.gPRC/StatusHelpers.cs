using System.Text;

namespace WinNUT.gPRC
{
	public static class StatusHelpers
	{
		static readonly Dictionary<string, string> _statusTranslations = new()
		{
			{ "OL", "On Line" },
			{ "OB", "On battery" },
			{ "LB", "Low battery"},
			{ "HB", "High battery"},
			{ "RB", "The battery needs to be replaced"},
			{ "CHRG", "The battery is charging"},
			{ "DISCHRG", "The battery is discharging"},
			{ "BYPASS", "UPS bypass circuit is active -- no battery protection is available"},
			{ "CAL", "UPS is currently performing runtime calibration (on battery)"},
			{ "OFF", "UPS is offline"},
			{ "OVER", "UPS is overloaded"},
			{ "TRIM", "UPS is trimming incoming voltage"},
			{ "BOOST", "UPS is boosting incoming voltage"},
			{ "FSD", "Forced Shutdown"}
		};

		public static string[] GetStatusFlags(string statusFlags)
		{
			return statusFlags.Split(" ");
		}

		public static string TranslateStatus(string upsStatus, string delimiter = "; ")
		{
			var flags = upsStatus.Split(" ");

			StringBuilder sb = new();

			foreach (var status in flags)
			{
				if (sb.Length > 0)
				{
					sb.Append(delimiter);
				}

				if (_statusTranslations.TryGetValue(status, out string? value))
				{
					sb.Append($"{value}");
				}
				else
				{
					sb.Append($"{status};");
				}
			}

			return sb.ToString();
		}

		public static string ToDisplay(this DeviceStatus status)
		{
			if (GetStatusFlags(status.Flags).Contains("OB"))
			{
				var remaining = (int)(status.BatterySecondsRemaining / 60);
				return $"Battery: {status.Charge}%\nRemaining: {remaining} min\nStatus: {TranslateStatus(status.Flags)}";
			}

			return $"Battery: {status.Charge}%\nStatus: {TranslateStatus(status.Flags)}";
		}

		public static bool IsOnBattery(this DeviceStatus status)
		{
			return GetStatusFlags(status.Flags).Contains("OB");
		}

		public static bool IsOnline(this DeviceStatus status)
		{
			return GetStatusFlags(status.Flags).Contains("OL");
		}
	}
}
