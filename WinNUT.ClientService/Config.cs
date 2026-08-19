namespace WinNUT.ClientService
{
	public record NUTClientConfig(string Host = "localhost", int Port = 3493);

	public record MonitorConfig(string UPSSerialNumber = "", int PollingIntervalSeconds = 5,
		int ChargeThreshold = 50, int RemainingThresholdSeconds = 300, int ShutdownDelaySeconds = 180,
		int ReconnectAttemptIntervalSeconds = 60)
	{
		public int ChargeThreshold { get; init; } = (ChargeThreshold >= 0 && ChargeThreshold <= 100)
			? ChargeThreshold : throw new ArgumentOutOfRangeException($"{nameof(ChargeThreshold)} must be between 0 and 100");

		public int PollingIntervalSeconds { get; init; } = (PollingIntervalSeconds >= 0)
			? PollingIntervalSeconds : throw new ArgumentOutOfRangeException($"{nameof(PollingIntervalSeconds)} cannot be less than 0");

		public int RemainingThresholdSeconds { get; init; } = (RemainingThresholdSeconds >= 0)
			? RemainingThresholdSeconds : throw new ArgumentOutOfRangeException($"{nameof(RemainingThresholdSeconds)} cannot be less than 0");

		public int ShutdownDelaySeconds { get; init; } = (ShutdownDelaySeconds >= 0)
			? ShutdownDelaySeconds : throw new ArgumentOutOfRangeException($"{nameof(ShutdownDelaySeconds)} cannot be less than 0");

		public int ReconnectAttemptIntervalSeconds { get; init; } = (ReconnectAttemptIntervalSeconds >= 0)
			? ReconnectAttemptIntervalSeconds : throw new ArgumentOutOfRangeException($"{nameof(ReconnectAttemptIntervalSeconds)} cannot be less than 0");
	}
}
