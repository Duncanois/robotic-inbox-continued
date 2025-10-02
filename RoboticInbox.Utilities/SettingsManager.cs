using System;
using System.IO;
using RoboticInbox.Data;

namespace RoboticInbox.Utilities;

internal class SettingsManager
{
	public const int H_DIST_MIN = 0;

	public const int H_DIST_MAX = 128;

	public const int V_DIST_MIN = -1;

	public const int V_DIST_MAX = 253;

	public const float SUCCESS_NOTICE_TIME_MIN = 0f;

	public const float SUCCESS_NOTICE_TIME_MAX = 10f;

	public const float BLOCKED_NOTICE_TIME_MIN = 0f;

	public const float BLOCKED_NOTICE_TIME_MAX = 10f;

	private static readonly ModLog<SettingsManager> _log = new ModLog<SettingsManager>();

	private static ModSettings Settings = null;

	public static string Filename { get; private set; } = Path.Combine(GameIO.GetSaveGameDir(), "robotic-inbox.json");

	public static int InboxHorizontalRange => Settings.InboxHorizontalRange;

	public static int InboxVerticalRange => Settings.InboxVerticalRange;

	public static float DistributionSuccessNoticeTime => Settings.DistributionSuccessNoticeTime;

	public static float DistributionBlockedNoticeTime => Settings.DistributionBlockedNoticeTime;

	public static bool BaseSiphoningProtection => Settings.BaseSiphoningProtection;

	internal static string AsString()
	{
		return $"\n=== Current Settings for Robotic Inbox\n- horizontal-range: {InboxHorizontalRange}\n  - [recommended: 5 | must be: >= {0} & <= {128} | impact: very high]\n- vertical-range: {InboxVerticalRange}\n  - [recommended: 5 | must be: >= {-1} & <= {253} | -1 = bedrock-to-sky | impact: high]\n- success-notice-time: {DistributionSuccessNoticeTime:0.0}\n  - [recommended: 2.0 | must be >= {0f} & <= {10f} | disable with 0.0]\n- blocked-notice-time: {DistributionBlockedNoticeTime:0.0}\n  - [recommended: 3.0 | must be >= {0f} & <= {10f} | disable with 0.0]\n- base-siphoning-protection: {BaseSiphoningProtection}\n  - [recommended: True]\n  - if placed within an LCB, the inbox will not distribute to containers outside of that same LCB\n  - this option helps to protect players from unintentionally dumping items in nearby raider chests placed just outside of their bases\n- debug mode: {ModApi.DebugMode}\n  - [recommended: False]\n  - enabling this adds a lot of overhead and should only be running for debugging purposes\n  - server starts with this in the False state\n=== Settings Stored In: {Filename}";
	}

	internal static void Load()
	{
		CreatePathIfMissing();
		try
		{
			Settings = Json<ModSettings>.Deserialize(File.ReadAllText(Filename));
			_log.Info("Successfully loaded settings for Robotic Inbox mod; filename: " + Filename + ".");
			_log.Info(AsString());
		}
		catch (FileNotFoundException)
		{
			_log.Info("No file detected for Robotic Inbox mod; creating a config with defaults in " + Filename);
			Settings = new ModSettings();
			try
			{
				Save();
			}
			catch (Exception)
			{
			}
		}
		catch (Exception ex3)
		{
			_log.Warn("Unhandled exception encountered when attempting to load settings for Robotic Inbox mod; filename: " + Filename, ex3);
			throw ex3;
		}
	}

	internal static void Save()
	{
		try
		{
			if (!Directory.Exists(GameIO.GetSaveGameDir()))
			{
				Directory.CreateDirectory(GameIO.GetSaveGameDir());
			}
			File.WriteAllText(Filename, Json<ModSettings>.Serialize(Settings));
		}
		catch (Exception ex)
		{
			_log.Error("Unable to save Robotic Inbox mod settings to " + Filename + ".", ex);
			throw ex;
		}
	}

	internal static void CreatePathIfMissing()
	{
		Directory.CreateDirectory(GameIO.GetSaveGameDir());
	}

	internal static int SetInboxHorizontalRange(int value)
	{
		Settings.InboxHorizontalRange = Clamp(value, 0, 128);
		Save();
		PropagateHorizontalRange();
		return Settings.InboxHorizontalRange;
	}

	internal static void PropagateHorizontalRange(EntityPlayer player)
	{
		((EntityAlive)player).SetCVar("roboticInboxRangeH", (float)Settings.InboxHorizontalRange);
	}

	private static void PropagateHorizontalRange()
	{
		for (int i = 0; i < ((WorldBase)GameManager.Instance.World).GetLocalPlayers().Count; i++)
		{
			PropagateHorizontalRange((EntityPlayer)(object)((WorldBase)GameManager.Instance.World).GetLocalPlayers()[i]);
		}
		for (int j = 0; j < GameManager.Instance.World.Players.list.Count; j++)
		{
			PropagateHorizontalRange(GameManager.Instance.World.Players.list[j]);
		}
	}

	internal static int SetInboxVerticalRange(int value)
	{
		Settings.InboxVerticalRange = Clamp(value, -1, 253);
		Save();
		PropagateVerticalRange();
		return Settings.InboxVerticalRange;
	}

	internal static void PropagateVerticalRange(EntityPlayer player)
	{
		((EntityAlive)player).SetCVar("roboticInboxRangeV", (float)Settings.InboxVerticalRange);
	}

	private static void PropagateVerticalRange()
	{
		for (int i = 0; i < ((WorldBase)GameManager.Instance.World).GetLocalPlayers().Count; i++)
		{
			PropagateVerticalRange((EntityPlayer)(object)((WorldBase)GameManager.Instance.World).GetLocalPlayers()[i]);
		}
		for (int j = 0; j < GameManager.Instance.World.Players.list.Count; j++)
		{
			PropagateVerticalRange(GameManager.Instance.World.Players.list[j]);
		}
	}

	internal static bool SetBaseSiphoningProtection(bool value)
	{
		Settings.BaseSiphoningProtection = value;
		Save();
		return Settings.BaseSiphoningProtection;
	}

	internal static float SetDistributionSuccessNoticeTime(float value)
	{
		Settings.DistributionSuccessNoticeTime = Clamp(value, 0f, 10f);
		Save();
		return Settings.DistributionSuccessNoticeTime;
	}

	internal static float SetDistributionBlockedNoticeTime(float value)
	{
		Settings.DistributionBlockedNoticeTime = Clamp(value, 0f, 10f);
		Save();
		return Settings.DistributionBlockedNoticeTime;
	}

	private static int Clamp(int value, int min, int max)
	{
		if (value >= min)
		{
			if (value <= max)
			{
				return value;
			}
			return max;
		}
		return min;
	}

	private static float Clamp(float value, float min, float max)
	{
		if (!(value < min))
		{
			if (!(value > max))
			{
				return value;
			}
			return max;
		}
		return min;
	}
}
