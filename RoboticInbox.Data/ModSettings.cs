namespace RoboticInbox.Data;

internal class ModSettings
{
	public int InboxHorizontalRange { get; set; } = 5;

	public int InboxVerticalRange { get; set; } = 5;

	public float DistributionSuccessNoticeTime { get; set; } = 2f;

	public float DistributionBlockedNoticeTime { get; set; } = 3f;

	public bool BaseSiphoningProtection { get; set; } = true;
}
