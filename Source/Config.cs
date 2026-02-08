using System.Net;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

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

		public static int Delay = 3000;
		public static int TraceTimeoutMs = 1000;

		public static int MaxPing;
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

		static Config() => Reset();

		public static void SetAll(int delay, int traceTimeoutMs, int maxPing, Color bgColor, Color goodColor, Color normalColor,
															Color badColor, bool runOnStartup, IPAddress address,
																	bool alarmConnectionLost, bool alarmTimeOut, bool alarmResumed, bool useNumbers,
																	string _SFXConnectionLost, string _SFXTimeOut, string _SFXResumed, bool offlineCounter)
		{
			Delay = delay;
			TraceTimeoutMs = traceTimeoutMs;
			MaxPing = maxPing;
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
			Delay = 3000;
			TraceTimeoutMs = 1000;
			MaxPing = 250;
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
		}

		public static void Load()
		{
			Reset();
			Directory.CreateDirectory(ConfigFolder);

			var config = new ConfigurationBuilder()
					.SetBasePath(ConfigFolder)
					.AddJsonFile(ConfigFileName, optional: true, reloadOnChange: false)
					.Build();

			Delay = GetInt(config, Key(nameof(Delay)), Delay);
			TraceTimeoutMs = GetInt(config, Key(nameof(TraceTimeoutMs)), TraceTimeoutMs);
			MaxPing = GetInt(config, Key(nameof(MaxPing)), MaxPing);
			OfflineCounter = GetBool(config, Key(nameof(OfflineCounter)), OfflineCounter);

			SetPenFromString(ref BgColor, config[Key(nameof(BgColor))] ?? string.Empty);
			SetPenFromString(ref GoodColor, config[Key(nameof(GoodColor))] ?? string.Empty);
			SetPenFromString(ref NormalColor, config[Key(nameof(NormalColor))] ?? string.Empty);
			SetPenFromString(ref BadColor, config[Key(nameof(BadColor))] ?? string.Empty);

			RunOnStartup = GetBool(config, Key(nameof(RunOnStartup)), RunOnStartup);

			string? ipText = config[Key(nameof(TheIPAddress))];
			if (!string.IsNullOrWhiteSpace(ipText) && IPAddress.TryParse(ipText, out IPAddress? ip) && ip != null)
			{
				TheIPAddress = ip;
			}

			AlarmConnectionLost = GetBool(config, Key(nameof(AlarmConnectionLost)), AlarmConnectionLost);
			AlarmTimeOut = GetBool(config, Key(nameof(AlarmTimeOut)), AlarmTimeOut);
			AlarmResumed = GetBool(config, Key(nameof(AlarmResumed)), AlarmResumed);
			UseNumbers = GetBool(config, Key(nameof(UseNumbers)), UseNumbers);

			SFXConnectionLost = NormalizeSfx(config[Key(nameof(SFXConnectionLost))]);
			SFXTimeOut = NormalizeSfx(config[Key(nameof(SFXTimeOut))]);
			SFXResumed = NormalizeSfx(config[Key(nameof(SFXResumed))]);
		}

		private static int GetInt(IConfiguration config, string key, int fallback)
		{
			if (int.TryParse(config[key], out int result))
				return result;

			return fallback;
		}

		private static bool GetBool(IConfiguration config, string key, bool fallback)
		{
			if (bool.TryParse(config[key], out bool result))
				return result;

			return fallback;
		}

		private static string Key(string name)
		{
			return $"{ConfigSectionName}:{name}";
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
					[nameof(Delay)] = Delay,
					[nameof(TraceTimeoutMs)] = TraceTimeoutMs,
					[nameof(MaxPing)] = MaxPing,
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
					[nameof(SFXResumed)] = SFXResumed
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
