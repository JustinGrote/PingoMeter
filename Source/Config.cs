using System.Net;
using System.Runtime.Versioning;
using System.Text.Json;

namespace PingoMeter
{
	[SupportedOSPlatform("windows")]
	internal static class Config
	{
		private const string ConfigFileName = "appsettings.json";
		private const string ConfigSectionName = "PingoMeter";
		private static readonly string ConfigFolder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"PingoMeter");
		private static readonly string ConfigPath = Path.Combine(ConfigFolder, ConfigFileName);

		public static int Interval = 1000;
		public static int Timeout = Interval;

		public static bool OfflineCounter = true;

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
			Interval = 3000;
			Timeout = 1000;
			OfflineCounter = true;
			BgColor = new Pen(Color.FromArgb(70, 0, 0));
			GoodColor = new Pen(Color.FromArgb(120, 180, 0));
			NormalColor = new Pen(Color.FromArgb(255, 180, 0));
			BadColor = new Pen(Color.FromArgb(255, 0, 0));
			RunOnStartup = false;
			TheIPAddress = IPAddress.Parse("8.8.8.8"); // google ip
			AlarmConnectionLost = false;
			AlarmTimeOut = false;
			AlarmResumed = false;
			UseNumbers = false;
			SFXConnectionLost = NONE_SFX;
			SFXTimeOut = NONE_SFX;
			SFXResumed = NONE_SFX;
			RunOnStartup = false;
			ipAddressHistory.Clear();
			if (TheIPAddress != null)
				AddIPToHistory(TheIPAddress.ToString());
		}

		public static void Load()
		{
			Reset();
			Directory.CreateDirectory(ConfigFolder);

			// Try to load config from JSON file
			if (File.Exists(ConfigPath))
			{
				try
				{
					string jsonText = File.ReadAllText(ConfigPath);
					using JsonDocument doc = JsonDocument.Parse(jsonText);

					if (doc.RootElement.TryGetProperty(ConfigSectionName, out JsonElement config))
					{
						Interval = GetInt(config, nameof(Interval), Interval);
						Timeout = Math.Min(GetInt(config, nameof(Timeout), Timeout), Interval);
						OfflineCounter = GetBool(config, nameof(OfflineCounter), OfflineCounter);

						SetPenFromString(ref BgColor, GetString(config, nameof(BgColor)));
						SetPenFromString(ref GoodColor, GetString(config, nameof(GoodColor)));
						SetPenFromString(ref NormalColor, GetString(config, nameof(NormalColor)));
						SetPenFromString(ref BadColor, GetString(config, nameof(BadColor)));

						RunOnStartup = GetBool(config, nameof(RunOnStartup), RunOnStartup);

						string? ipText = GetString(config, nameof(TheIPAddress));
						if (!string.IsNullOrWhiteSpace(ipText) && IPAddress.TryParse(ipText, out IPAddress? ip) && ip != null)
						{
							TheIPAddress = ip;
						}

						AlarmConnectionLost = GetBool(config, nameof(AlarmConnectionLost), AlarmConnectionLost);
						AlarmTimeOut = GetBool(config, nameof(AlarmTimeOut), AlarmTimeOut);
						AlarmResumed = GetBool(config, nameof(AlarmResumed), AlarmResumed);
						UseNumbers = GetBool(config, nameof(UseNumbers), UseNumbers);

						SFXConnectionLost = NormalizeSfx(GetString(config, nameof(SFXConnectionLost)));
						SFXTimeOut = NormalizeSfx(GetString(config, nameof(SFXTimeOut)));
						SFXResumed = NormalizeSfx(GetString(config, nameof(SFXResumed)));

						if (config.TryGetProperty(nameof(IPAddressHistory), out JsonElement historyElement) && historyElement.ValueKind == JsonValueKind.Array)
						{
							ipAddressHistory.Clear();
							foreach (var item in historyElement.EnumerateArray())
							{
								var historyItem = item.GetString();
								if (!string.IsNullOrWhiteSpace(historyItem))
									ipAddressHistory.Add(historyItem);
							}
						}
						else if (TheIPAddress != null)
						{
							ipAddressHistory.Clear();
							AddIPToHistory(TheIPAddress.ToString());
						}
					}
				}
				catch (JsonException)
				{
					// If config file has invalid JSON, use defaults
				}
				catch (IOException)
				{
					// If config file can't be read, use defaults
				}
			}
		}

		private static int GetInt(JsonElement config, string key, int fallback)
		{
			if (config.TryGetProperty(key, out JsonElement element))
			{
				if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int result))
					return result;

				// Try parsing as string
				string? str = element.GetString();
				if (str != null && int.TryParse(str, out int parsed))
					return parsed;
			}

			return fallback;
		}

		private static bool GetBool(JsonElement config, string key, bool fallback)
		{
			if (config.TryGetProperty(key, out JsonElement element))
			{
				if (element.ValueKind == JsonValueKind.True)
					return true;
				if (element.ValueKind == JsonValueKind.False)
					return false;

				// Try parsing as string
				string? str = element.GetString();
				if (str != null && bool.TryParse(str, out bool parsed))
					return parsed;
			}

			return fallback;
		}

		private static string GetString(JsonElement config, string key)
		{
			if (config.TryGetProperty(key, out JsonElement element))
			{
				return element.GetString() ?? string.Empty;
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
			return string.IsNullOrWhiteSpace(value) ? NONE_SFX : value;
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

			var root = new Dictionary<string, object?>
			{
				[ConfigSectionName] = new Dictionary<string, object?>
				{
					[nameof(Interval)] = Interval,
					[nameof(Timeout)] = Timeout,
					[nameof(OfflineCounter)] = OfflineCounter,
					[nameof(BgColor)] = ColorToString(BgColor),
					[nameof(GoodColor)] = ColorToString(GoodColor),
					[nameof(NormalColor)] = ColorToString(NormalColor),
					[nameof(BadColor)] = ColorToString(BadColor),
					[nameof(RunOnStartup)] = RunOnStartup,
					[nameof(TheIPAddress)] = TheIPAddress?.ToString() ?? string.Empty,
					[nameof(AlarmConnectionLost)] = AlarmConnectionLost,
					[nameof(AlarmTimeOut)] = AlarmTimeOut,
					[nameof(AlarmResumed)] = AlarmResumed,
					[nameof(UseNumbers)] = UseNumbers,
					[nameof(SFXConnectionLost)] = SFXConnectionLost,
					[nameof(SFXTimeOut)] = SFXTimeOut,
					[nameof(SFXResumed)] = SFXResumed,
					[nameof(IPAddressHistory)] = ipAddressHistory
				}
			};

			var json = JsonSerializer.Serialize(root, new JsonSerializerOptions
			{
				WriteIndented = true
			});

			File.WriteAllText(ConfigPath, json);
		}
	}
}
