using System;
using System.Collections.Generic;
using System.Linq;
using RoboticInbox.Utilities;

namespace RoboticInbox;

internal class ConsoleCmdRoboticInbox : ConsoleCmdAbstract
{
	private static readonly string[] Commands = new string[2] { "roboticinbox", "ri" };

	private readonly string help;

	public ConsoleCmdRoboticInbox()
	{
		ConsoleCmdRoboticInbox consoleCmdRoboticInbox = this;
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{ "settings", "list current settings alongside default/recommended values" },
			{ "horizontal-range <int>", "set how wide (x/z axes) the inbox should scan for storage containers" },
			{ "vertical-range <int>", "set how high/low (y axis) the inbox should scan for storage containers" },
			{ "success-notice-time <float>", "set how long to leave distribution success notice on boxes" },
			{ "blocked-notice-time <float>", "set how long to leave distribution blocked notice on boxes" },
			{ "base-siphoning-protection <bool>", "whether inboxes within claimed land should prevent scanning outside the bounds of their lcb" },
			{ "dm", "toggle debug logging mode" }
		};
		int i = 1;
		int j = 1;
		help = "Usage:\n  " + string.Join("\n  ", dictionary.Keys.Select((string command) => $"{i++}. {((ConsoleCmdAbstract)consoleCmdRoboticInbox).GetCommands()[0]} {command}").ToList()) + "\nDescription Overview\n" + string.Join("\n", dictionary.Values.Select((string description) => $"{j++}. {description}").ToList());
	}

	public override string[] getCommands()
	{
		return Commands;
	}

	public override string getDescription()
	{
		return "Configure or adjust settings for the RoboticInbox mod.";
	}

	public override string GetHelp()
	{
		return help;
	}

	public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
	{
		try
		{
			if (_params.Count > 0)
			{
				string text = _params[0].ToLower();
				if (text != null)
				{
					int length = text.Length;
					if (length <= 8)
					{
						if (length != 2)
						{
							if (length != 5)
							{
								if (length == 8 && text == "settings")
								{
									SingletonMonoBehaviour<SdtdConsole>.Instance.Output(SettingsManager.AsString());
									return;
								}
							}
							else if (text == "debug")
							{
								goto IL_03d6;
							}
						}
						else if (text == "dm")
						{
							goto IL_03d6;
						}
					}
					else
					{
						switch (length)
						{
						case 19:
							switch (text[0])
							{
							case 's':
							{
								if (!(text == "success-notice-time") || _params.Count == 1 || !float.TryParse(_params[1], out var result4))
								{
									break;
								}
								try
								{
									float distributionSuccessNoticeTime = SettingsManager.DistributionSuccessNoticeTime;
									float num4 = SettingsManager.SetDistributionSuccessNoticeTime(result4);
									SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"success-notice-time updated from {distributionSuccessNoticeTime:0.0} to {num4:0.0} and settings saved successfully to {SettingsManager.Filename}");
									return;
								}
								catch (Exception ex5)
								{
									SingletonMonoBehaviour<SdtdConsole>.Instance.Output("settings could not be saved to " + SettingsManager.Filename + " due to encountering an issue: " + ex5.Message);
									return;
								}
							}
							case 'b':
							{
								if (!(text == "blocked-notice-time") || _params.Count == 1 || !float.TryParse(_params[1], out var result3))
								{
									break;
								}
								try
								{
									float distributionBlockedNoticeTime = SettingsManager.DistributionBlockedNoticeTime;
									float num3 = SettingsManager.SetDistributionBlockedNoticeTime(result3);
									SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"blocked-notice-time updated from {distributionBlockedNoticeTime:0.0} to {num3:0.0} and settings saved successfully to {SettingsManager.Filename}");
									return;
								}
								catch (Exception ex4)
								{
									SingletonMonoBehaviour<SdtdConsole>.Instance.Output("settings could not be saved to " + SettingsManager.Filename + " due to encountering an issue: " + ex4.Message);
									return;
								}
							}
							}
							break;
						case 16:
						{
							if (!(text == "horizontal-range") || _params.Count == 1 || !int.TryParse(_params[1], out var result))
							{
								break;
							}
							try
							{
								int inboxHorizontalRange = SettingsManager.InboxHorizontalRange;
								int num = SettingsManager.SetInboxHorizontalRange(result);
								SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"horizontal-range updated from {inboxHorizontalRange} to {num} and settings saved successfully to {SettingsManager.Filename}");
								return;
							}
							catch (Exception ex2)
							{
								SingletonMonoBehaviour<SdtdConsole>.Instance.Output("settings could not be saved to " + SettingsManager.Filename + " due to encountering an issue: " + ex2.Message);
								return;
							}
						}
						case 14:
						{
							if (!(text == "vertical-range") || _params.Count == 1 || !int.TryParse(_params[1], out var result2))
							{
								break;
							}
							try
							{
								int inboxVerticalRange = SettingsManager.InboxVerticalRange;
								int num2 = SettingsManager.SetInboxVerticalRange(result2);
								SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"vertical-range updated from {inboxVerticalRange} to {num2} and settings saved successfully to {SettingsManager.Filename}");
								return;
							}
							catch (Exception ex3)
							{
								SingletonMonoBehaviour<SdtdConsole>.Instance.Output("settings could not be saved to " + SettingsManager.Filename + " due to encountering an issue: " + ex3.Message);
								return;
							}
						}
						case 25:
							if (!(text == "base-siphoning-protection"))
							{
								break;
							}
							try
							{
								bool baseSiphoningProtection = SettingsManager.BaseSiphoningProtection;
								bool flag = SettingsManager.SetBaseSiphoningProtection(!baseSiphoningProtection);
								SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"base-siphoning-protection updated from {baseSiphoningProtection} to {flag} and settings saved successfully to {SettingsManager.Filename}");
								return;
							}
							catch (Exception ex)
							{
								SingletonMonoBehaviour<SdtdConsole>.Instance.Output("settings could not be saved to " + SettingsManager.Filename + " due to encountering an issue: " + ex.Message);
								return;
							}
						}
					}
				}
			}
			SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Invald parameters provided; use 'help " + Commands[0] + "' to learn more.");
			return;
			IL_03d6:
			ModApi.DebugMode = !ModApi.DebugMode;
			SingletonMonoBehaviour<SdtdConsole>.Instance.Output("debug logging mode has been " + (ModApi.DebugMode ? "enabled" : "disabled") + " for Robotic Inbox.");
		}
		catch (Exception ex6)
		{
			SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Exception encountered: \"" + ex6.Message + "\"\n" + ex6.StackTrace);
		}
	}
}
