using System.Net;
using System.Xml.Linq;

namespace PingoMeter
{

	internal static class Config
	{
		// Default values defined in source code
		private const int DEFAULT_INTERVAL = 1000;
		private const int DEFAULT_TIMEOUT = 1000;
		private const bool DEFAULT_OFFLINE_COUNTER = true;
		private const int DEFAULT_BG_COLOR_R = 70;
		private const int DEFAULT_BG_COLOR_G = 0;
		private const int DEFAULT_BG_COLOR_B = 0;
		private const int DEFAULT_GOOD_COLOR_R = 120;
		private const int DEFAULT_GOOD_COLOR_G = 180;
		private const int DEFAULT_GOOD_COLOR_B = 0;
		private const int DEFAULT_NORMAL_COLOR_R = 255;
		private const int DEFAULT_NORMAL_COLOR_G = 180;
		private const int DEFAULT_NORMAL_COLOR_B = 0;
		private const int DEFAULT_BAD_COLOR_R = 255;
		private const int DEFAULT_BAD_COLOR_G = 0;
		private const int DEFAULT_BAD_COLOR_B = 0;
		private const bool DEFAULT_RUN_ON_STARTUP = false;
		private const string DEFAULT_IP_ADDRESS = "1.1.1.1";
		private const bool DEFAULT_ALARM_CONNECTION_LOST = false;
		private const bool DEFAULT_ALARM_TIMEOUT = false;
		private const bool DEFAULT_ALARM_RESUMED = false;
		private const bool DEFAULT_USE_NUMBERS = false;

		private static readonly string ConfigFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"PingoMeter");
		private static readonly string ConfigPath = Path.Combine(ConfigFolder, "app.config");

		public static int Interval = DEFAULT_INTERVAL;
		public static int Timeout = DEFAULT_TIMEOUT;

		public static bool OfflineCounter = DEFAULT_OFFLINE_COUNTER;

		public static Pen? BgColor;
		public static Pen? GoodColor;
		public static Pen? NormalColor;
		public static Pen? BadColor;

		public static bool RunOnStartup;

		private static string ipName = string.Empty;
		private static IPAddress? ipAddress;

		public static IPAddress? TheIPAddress
		{
			get => ipAddress;
			set
			{
				ipAddress = value;
				ipName = value?.ToString() ?? string.Empty;
			}
		}

		public static string GetIPName => ipName;

		public static bool AlarmConnectionLost;
		public static bool AlarmTimeOut;
		public static bool AlarmResumed;

		// sound effects path to .wav (or "(none)")
		public const string NONE_SFX = "(none)";
		public static string SFXConnectionLost = NONE_SFX;
		public static string SFXTimeOut = NONE_SFX;
		public static string SFXResumed = NONE_SFX;

		/// <summary> Use numbers for the ping instead of a graph. </summary>
		public static bool UseNumbers;

		private static List<string> ipAddressHistory = new();
		public static IReadOnlyList<string> IPAddressHistory => ipAddressHistory.AsReadOnly();

		static Config() => Reset();

		public static void SetAll(int interval, int timeout, Color bgColor, Color goodColor, Color normalColor,
																				Color badColor, bool runOnStartup, IPAddress address,
																						bool alarmConnectionLost, bool alarmTimeOut, bool alarmResumed, bool useNumbers,
																						string _SFXConnectionLost, string _SFXTimeOut, string _SFXResumed, bool offlineCounter)
		{
			Interval = interval;
			Timeout = Math.Min(timeout, interval);
			BgColor = new Pen(bgColor);
			GoodColor = new Pen(goodColor);
			NormalColor = new Pen(normalColor);
			BadColor = new Pen(badColor);
			RunOnStartup = runOnStartup;
			TheIPAddress = address;
			AlarmConnectionLost = alarmConnectionLost;
			AlarmTimeOut = alarmTimeOut;
			AlarmResumed = alarmResumed;
			UseNumbers = useNumbers;
			SFXConnectionLost = _SFXConnectionLost;
			SFXTimeOut = _SFXTimeOut;
			SFXResumed = _SFXResumed;
			OfflineCounter = offlineCounter;
		}

		public static void Reset()
		{
			Interval = DEFAULT_INTERVAL;
			Timeout = DEFAULT_TIMEOUT;
			OfflineCounter = DEFAULT_OFFLINE_COUNTER;
			BgColor = new Pen(Color.FromArgb(DEFAULT_BG_COLOR_R, DEFAULT_BG_COLOR_G, DEFAULT_BG_COLOR_B));
			GoodColor = new Pen(Color.FromArgb(DEFAULT_GOOD_COLOR_R, DEFAULT_GOOD_COLOR_G, DEFAULT_GOOD_COLOR_B));
			NormalColor = new Pen(Color.FromArgb(DEFAULT_NORMAL_COLOR_R, DEFAULT_NORMAL_COLOR_G, DEFAULT_NORMAL_COLOR_B));
			BadColor = new Pen(Color.FromArgb(DEFAULT_BAD_COLOR_R, DEFAULT_BAD_COLOR_G, DEFAULT_BAD_COLOR_B));
			RunOnStartup = DEFAULT_RUN_ON_STARTUP;
			TheIPAddress = IPAddress.Parse(DEFAULT_IP_ADDRESS);
			AlarmConnectionLost = DEFAULT_ALARM_CONNECTION_LOST;
			AlarmTimeOut = DEFAULT_ALARM_TIMEOUT;
			AlarmResumed = DEFAULT_ALARM_RESUMED;
			UseNumbers = DEFAULT_USE_NUMBERS;
			SFXConnectionLost = NONE_SFX;
			SFXTimeOut = NONE_SFX;
			SFXResumed = NONE_SFX;
			ipAddressHistory.Clear();
			if (TheIPAddress != null)
				AddIPToHistory(TheIPAddress.ToString());
		}

		public static void Load()
		{
			Reset();
			Directory.CreateDirectory(ConfigFolder);

			// Try to load config from app.config file
			if (File.Exists(ConfigPath))
			{
				try
				{
					XDocument doc = XDocument.Load(ConfigPath);
					XElement? appSettingsElement = doc.Root?.Element("appSettings");
					if (appSettingsElement != null)
					{
						Interval = GetInt(appSettingsElement, nameof(Interval), Interval);
						Timeout = Math.Min(GetInt(appSettingsElement, nameof(Timeout), Timeout), Interval);
						OfflineCounter = GetBool(appSettingsElement, nameof(OfflineCounter), OfflineCounter);

						SetPenFromString(ref BgColor, GetString(appSettingsElement, nameof(BgColor)));
						SetPenFromString(ref GoodColor, GetString(appSettingsElement, nameof(GoodColor)));
						SetPenFromString(ref NormalColor, GetString(appSettingsElement, nameof(NormalColor)));
						SetPenFromString(ref BadColor, GetString(appSettingsElement, nameof(BadColor)));

						RunOnStartup = GetBool(appSettingsElement, nameof(RunOnStartup), RunOnStartup);

						string? ipText = GetString(appSettingsElement, nameof(TheIPAddress));
						if (!string.IsNullOrWhiteSpace(ipText) && IPAddress.TryParse(ipText, out IPAddress? ip) && ip != null)
						{
							TheIPAddress = ip;
						}

						AlarmConnectionLost = GetBool(appSettingsElement, nameof(AlarmConnectionLost), AlarmConnectionLost);
						AlarmTimeOut = GetBool(appSettingsElement, nameof(AlarmTimeOut), AlarmTimeOut);
						AlarmResumed = GetBool(appSettingsElement, nameof(AlarmResumed), AlarmResumed);
						UseNumbers = GetBool(appSettingsElement, nameof(UseNumbers), UseNumbers);

						SFXConnectionLost = NormalizeSfx(GetString(appSettingsElement, nameof(SFXConnectionLost)));
						SFXTimeOut = NormalizeSfx(GetString(appSettingsElement, nameof(SFXTimeOut)));
						SFXResumed = NormalizeSfx(GetString(appSettingsElement, nameof(SFXResumed)));

						string? historyString = GetString(appSettingsElement, nameof(IPAddressHistory));
						if (!string.IsNullOrWhiteSpace(historyString))
						{
							ipAddressHistory.Clear();
							string[] historyItems = historyString.Split('|');
							foreach (var item in historyItems)
							{
								if (!string.IsNullOrWhiteSpace(item))
									ipAddressHistory.Add(item);
							}
						}
						else if (TheIPAddress != null)
						{
							ipAddressHistory.Clear();
							AddIPToHistory(TheIPAddress.ToString());
						}
					}
				}
				catch (Exception)
				{
					// If config file has any reading errors, use defaults
				}
			}
		}

		private static int GetInt(XElement settings, string key, int fallback)
		{
			string? value = settings.Elements("add")
				.FirstOrDefault(e => (string?)e.Attribute("key") == key)
				?.Attribute("value")
				?.Value;

			if (value != null && !string.IsNullOrWhiteSpace(value))
			{
				if (int.TryParse(value, out int result))
					return result;
			}

			return fallback;
		}

		private static bool GetBool(XElement settings, string key, bool fallback)
		{
			string? value = settings.Elements("add")
				.FirstOrDefault(e => (string?)e.Attribute("key") == key)
				?.Attribute("value")
				?.Value;

			if (value != null && !string.IsNullOrWhiteSpace(value))
			{
				if (bool.TryParse(value, out bool result))
					return result;
			}

			return fallback;
		}

		private static string GetString(XElement settings, string key)
		{
			string? value = settings.Elements("add")
				.FirstOrDefault(e => (string?)e.Attribute("key") == key)
				?.Attribute("value")
				?.Value;

			if (value != null && !string.IsNullOrWhiteSpace(value))
			{
				return value;
			}

			return string.Empty;
		}

		private static void SetPenFromString(ref Pen? pen, string str)
		{
			if (str.IndexOf(':') != -1)
			{
				string[] rgb = str.Split(':');
				if (rgb.Length == 3)
				{
					if (int.TryParse(rgb[0], out int r) && r > -1 && r < 256 &&
							int.TryParse(rgb[1], out int g) && g > -1 && g < 256 &&
							int.TryParse(rgb[2], out int b) && b > -1 && b < 256)
						pen = new Pen(Color.FromArgb(r, g, b));
				}
			}
		}

		private static string ColorToString(Pen? pen)
		{
			if (pen == null)
				return string.Empty;

			return $"{pen.Color.R}:{pen.Color.G}:{pen.Color.B}";
		}

		private static string NormalizeSfx(string? value)
		{
			return string.IsNullOrWhiteSpace(value) ? NONE_SFX : value!;
		}

		public static void AddIPToHistory(string ipAddress)
		{
			if (string.IsNullOrWhiteSpace(ipAddress))
				return;

			ipAddressHistory.Remove(ipAddress);
			ipAddressHistory.Insert(0, ipAddress);

			const int maxHistory = 20;
			if (ipAddressHistory.Count > maxHistory)
				ipAddressHistory.RemoveRange(maxHistory, ipAddressHistory.Count - maxHistory);
		}

		public static void Save()
		{
			// Ensure all required fields are initialized
			if (BgColor == null || GoodColor == null || NormalColor == null || BadColor == null || TheIPAddress == null)
			{
				Reset();

				// After reset, these should never be null
				if (BgColor == null || GoodColor == null || NormalColor == null || BadColor == null || TheIPAddress == null)
				{
					throw new InvalidOperationException("Failed to initialize configuration values");
				}
			}

			Directory.CreateDirectory(ConfigFolder);

			XDocument doc;
			if (File.Exists(ConfigPath))
			{
				doc = XDocument.Load(ConfigPath);
			}
			else
			{
				doc = new XDocument(
					new XElement("configuration",
						new XElement("appSettings")));
			}

			XElement? appSettingsElement = doc.Root?.Element("appSettings");
			if (appSettingsElement == null)
			{
				appSettingsElement = new XElement("appSettings");
				doc.Root?.Add(appSettingsElement);
			}

			// Helper to add or update a setting
			void SetSetting(string key, string value)
			{
				XElement? existing = appSettingsElement.Elements("add")
					.FirstOrDefault(e => (string?)e.Attribute("key") == key);

				if (existing != null)
				{
					existing.SetAttributeValue("value", value);
				}
				else
				{
					appSettingsElement.Add(new XElement("add",
						new XAttribute("key", key),
						new XAttribute("value", value)));
				}
			}

			SetSetting(nameof(Interval), Interval.ToString());
			SetSetting(nameof(Timeout), Timeout.ToString());
			SetSetting(nameof(OfflineCounter), OfflineCounter.ToString());
			SetSetting(nameof(BgColor), ColorToString(BgColor));
			SetSetting(nameof(GoodColor), ColorToString(GoodColor));
			SetSetting(nameof(NormalColor), ColorToString(NormalColor));
			SetSetting(nameof(BadColor), ColorToString(BadColor));
			SetSetting(nameof(RunOnStartup), RunOnStartup.ToString());
			SetSetting(nameof(TheIPAddress), TheIPAddress?.ToString() ?? string.Empty);
			SetSetting(nameof(AlarmConnectionLost), AlarmConnectionLost.ToString());
			SetSetting(nameof(AlarmTimeOut), AlarmTimeOut.ToString());
			SetSetting(nameof(AlarmResumed), AlarmResumed.ToString());
			SetSetting(nameof(UseNumbers), UseNumbers.ToString());
			SetSetting(nameof(SFXConnectionLost), SFXConnectionLost);
			SetSetting(nameof(SFXTimeOut), SFXTimeOut);
			SetSetting(nameof(SFXResumed), SFXResumed);
			SetSetting(nameof(IPAddressHistory), string.Join("|", ipAddressHistory));

			doc.Save(ConfigPath);
		}
	}
}
