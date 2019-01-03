// #define ALT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;
using TS3Client.Audio;
using TS3Client;
using IniParser;
using IniParser.Model;
using TS3AudioBot.History;
using TS3AudioBot.Helper;

namespace SimpleTTS
{
	public class PluginInfo
	{
		public static readonly string ShortName = typeof(PluginInfo).Namespace;
		public static readonly string Name = string.IsNullOrEmpty(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name) ? ShortName : System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
		public static string Description = "";
		public static string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		public PluginInfo()
		{
			var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
			Description = versionInfo.FileDescription;
			Author = versionInfo.CompanyName;
		}
	}
	public class SimpleTTS : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3Client TS3Client { get; set; }
		public PlayManager PlayManager { get; set; }
		public HistoryManager HistoryManager { get; set; }
		public IPlayerConnection PlayerConnection { get; set; }
		public IVoiceTarget targetManager { get; set; }
		public ConfRoot ConfRoot { get; set; }
		// public Bot Bot { get; set; }
		// { "UK English Female", "UK English Male", "US English Female", "Spanish Female", "French Female", "Deutsch Female", "Italian Female", "Greek Female", "Hungarian Female", "Turkish Female", "Russian Female", "Dutch Female", "Swedish Female", "Norwegian Female", "Japanese Female", "Korean Female", "Chinese Female", "Hindi Female", "Serbian Male", "Croatian Male", "Bosnian Male", "Romanian Male", "Catalan Male", "Australian Female", "Finnish Female", "Afrikaans Male", "Albanian Male", "Arabic Male", "Armenian Male", "Czech Female", "Danish Female", "Esperanto Male", "Hatian Creole Female", "Icelandic Male", "Indonesian Female", "Latin Female", "Latvian Male", "Macedonian Male", "Moldavian Male", "Montenegrin Male", "Polish Female", "Brazilian Portuguese Female", "Portuguese Female", "Serbo-Croatian Male", "Slovak Female", "Spanish Latin American Female", "Swahili Male", "Tamil Male", "Thai Female", "Vietnamese Male", "Welsh Male" };
		public string[] TTSLocales = { "af-ZA", "ar-SA", "bs", "ca-ES", "cs-CZ", "cy", "da-DK", "de-DE", "el-GR", "en-AU", "en-GB", "en-US", "eo", "es-ES", "es-MX", "fi-FI", "fr-FR", "hi-IN", "hr-HR", "hu-HU", "hy-AM", "id-ID", "is-IS", "it-IT", "ja-JP", "ko-KR", "la", "lv-LV", "md", "me", "mk-MK", "nb-NO", "nl-NL", "pl-PL", "pt-BR", "ro-RO", "ru-RU", "sk-SK", "sq-AL", "sr-RS", "sv-SE", "sw-KE", "th-TH", "tr-TR", "vi-VN", "zh-CN", "zh-HK", "zh-TW" };
		public string[] TTSGenders = { "male", "female" };
		public bool isTalking = false; public bool isBroadcast = false; public float BOTVolume; public float oldVolume = 0;
		GroupWhisperType oldGroupWhisperType; GroupWhisperTarget oldGroupWhisperTarget; TargetSendMode oldSendMode;
		ICollection<ushort> oldWhisperClients; ICollection<ulong> oldWhisperChannels; ulong oldTargetId; bool waiting_for_end = false;

		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		public static IniData PluginConfig;
		public const string section = "General";
		public const string bsection = "Broadcast";

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				PluginConfig[section]["Url"] = "http://code.responsivevoice.org/getvoice.php?t={text}&tl={locale}&gender={gender}&pitch={pitch}&rate={rate}&vol=1"; // &sv={sv}&vn={vn}
				PluginConfig[section]["Locale"] = "en-US";
				PluginConfig[section]["Gender"] = "female";
				PluginConfig[section]["Pitch"] = "0.5";
				PluginConfig[section]["Rate"] = "0.5";
				PluginConfig[section]["Mode"] = "1";
				PluginConfig[section]["Resume"] = "0";
				PluginConfig[section]["Volume"] = "50";
				PluginConfig[bsection]["Gender"] = "male";
				PluginConfig[bsection]["Rate"] = "0.3";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			BOTVolume = PluginConfig[section].ContainsKey("Volume") ? float.Parse(PluginConfig[section]["Volume"]) : (float)100;
			PlayManager.BeforeResourceStopped += BeforeResourceStopped;
			PlayManager.AfterResourceStopped += AfterResourceStopped;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void BeforeResourceStopped(object sender, EventArgs e)
		{
			try {
				if (!isTalking) return;
				PlayerConnection.Volume = (float) oldVolume;
				Log.Debug($"Reset Volume to {oldVolume}");
				isTalking = false;
				if (!isBroadcast) return;
				isBroadcast = false;
				targetManager.SendMode = oldSendMode;
				switch (oldSendMode)
				{
					case TargetSendMode.None:
					case TargetSendMode.Voice:
						targetManager.SendMode = oldSendMode;
						break;
					case TargetSendMode.Whisper:
						foreach (var client in oldWhisperClients)
							targetManager.WhisperClientSubscribe(client);
						foreach (var channel in oldWhisperChannels)
							targetManager.WhisperChannelSubscribe(false, channel);
						break;
					case TargetSendMode.WhisperGroup:
						targetManager.SetGroupWhisper(oldGroupWhisperType, oldGroupWhisperTarget, oldTargetId);
						break;
					default: break;
				}

			/*foreach (var client in oldWhisperClients)
				{
					targetManager.SetGroupWhisper(GroupWh, GroupWhisperTarget., 0);
				}
				targetManager.SetGroupWhisper(oldGroupWhisperType, oldGroupWhisperTarget, 0);*/
			}  catch (Exception ex) { Log.Error(ex.Message); }
		}

		private void AfterResourceStopped(object sender, EventArgs e)
		{
			if (!waiting_for_end) return;
			waiting_for_end = false;
			var resume = PluginConfig[section]["Resume"];
			if (string.IsNullOrWhiteSpace("resume")) return;
			if (resume == "0") return;
			if (PluginConfig[section]["Resume"] == "0") return;
			switch (resume)
			{
				case "1":
					PlayManager.PlaylistManager.Previous();
					break;
				case "2":
					var ale = HistoryManager.GetEntryById(2).UnwrapThrow();
					PlayManager.Play(InvokerData.Anonymous, ale.AudioResource).UnwrapThrow();
					break;
				default:
					break;
			}
		}

		[Command("broadcast", "Syntax: !broadcast <text>")]
		public void CommandBroadCast(IVoiceTarget targetManager, IPlayerConnection playerConnection, PlayManager playManager, InvokerData invoker, params string[] text)
		{
			try {
				oldGroupWhisperType = targetManager.GroupWhisperType;
				oldGroupWhisperTarget = targetManager.GroupWhisperTarget;
				oldSendMode = targetManager.SendMode;
				targetManager.SetGroupWhisper(GroupWhisperType.AllClients, GroupWhisperTarget.AllChannels, 0);
				targetManager.SendMode = TargetSendMode.WhisperGroup;
				oldTargetId = targetManager.GroupWhisperTargetId;
				oldWhisperChannels = targetManager.WhisperChannel.ToArray();
				oldWhisperClients = targetManager.WhisperClients.ToArray();
				isBroadcast = true;
				PlayerConnection.Volume = BOTVolume;
				Log.Debug($"Set Volume to {PlayerConnection.Volume}");
				CommandSay(playerConnection, invoker, text);
					// playerConnection.Volume = 100;
					// targetManager.WhisperClientSubscribe(invoker.ClientId.Value);
			} catch (Exception ex) { Log.Error(ex.Message); }
		}
		[Command("say", "Syntax: !say <text>")]
		public void CommandSay(IPlayerConnection playerConnection, InvokerData invoker, params string[] _text) {
			try {
				var text = Uri.EscapeUriString(string.Join(" ", _text));
				var url = PluginConfig[section]["Url"]
					.Replace("{text}", text)
					.Replace("{locale}", PluginConfig[section]["Locale"])
					.Replace("{gender}", isBroadcast ? PluginConfig[bsection]["Gender"] : PluginConfig[section]["Gender"])
					.Replace("{pitch}", PluginConfig[section]["Pitch"])
					.Replace("{rate}", isBroadcast ? PluginConfig[bsection]["Rate"] : PluginConfig[section]["Rate"])
					.Replace("{volume}", PluginConfig[section]["Volume"]);
				oldVolume = playerConnection.Volume;
				Log.Debug("Saved old volume: {}", oldVolume);
				isTalking = true;waiting_for_end = true;
				Log.Debug("Saying {}", url);
				var mode = PluginConfig[section]["Mode"];
				switch (mode)
				{
					case "0":
						PlayManager.Play(invoker, url);
						break;
					case "1":
						playerConnection.AudioStart(url);
						break;
					case "2":
						PlayManager.Play(InvokerData.Anonymous, url); 
						break;
					default:
						throw new Exception($"Invalid Mode: {mode}");
				}
			} catch(Exception ex) { Log.Error(ex.Message); }
		}

		[Command("tts locale", "Syntax: !tts <locale>")]
		public string CommandSetLocale(string locale)
		{
			if (!TTSLocales.Contains(locale)) return $"Failed to set locale! Make sure it's a valid locale from {string.Join(",", TTSLocales)}";
			PluginConfig[section]["Locale"] = locale;
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			return $"Set SimpleTTS locale to [b]{locale}[/b]";
		}

		[Command("tts gender", "Syntax: !tts gender <male|female>")]
		public string CommandSetGender(string gender)
		{
			if (!TTSGenders.Contains(gender)) return $"Failed to set locale! Make sure it's a valid value from {string.Join(",", TTSGenders)}";
			PluginConfig[section]["Gender"] = gender;
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			return $"Set SimpleTTS gender to [b]{gender}[/b]";
		}

		[Command("tts pitch", "Syntax: !tts pitch <0.0-1.0>")]
		public string CommandSetPitch(string pitch)
		{
			var success = double.TryParse(pitch, out double s);
			if (!success) return "Failed to set pitch! Make sure it's a valid value between 0.0 and 1.0";
			PluginConfig[section]["Pitch"] = pitch;
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			return $"Set SimpleTTS pitch to [b]{pitch}[/b]";
		}

		[Command("tts rate", "Syntax: !tts rate <0.0-1.0>")]
		public string CommandSetRate(string rate)
		{
			var success = double.TryParse(rate, out double s);
			if (!success) return "Failed to set rate! Make sure it's a valid value between 0.0 and 1.0";
			PluginConfig[section]["Rate"] = rate;
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			return $"Set SimpleTTS rate to [b]{rate}[/b]";
		}

		[Command("tts volume", "Syntax: !tts volume <0-100>")]
		public string CommandSetVolume(string volume)
		{
			var success = float.TryParse(volume, out float s);
			if (!success) return "Failed to set volume! Make sure it's a valid value between 0 and 100";
			BOTVolume = s;
			PluginConfig[section]["Volume"] = s.ToString();
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			return $"Set SimpleTTS volume to [b]{volume}[/b]";
		}

		[Command("tts mode", "Syntax: !tts mode <0-1>")]
		public string CommandSetMode(string mode = null)
		{
			if (mode is null) return $"Mode: {PluginConfig[section]["Mode"]}";
			PluginConfig[section]["Mode"] = mode;
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			return $"Set SimpleTTS mode to [b]{mode}[/b]";
		}

		public void Dispose()
		{
			PlayManager.AfterResourceStopped -= AfterResourceStopped;
			PlayManager.BeforeResourceStopped -= BeforeResourceStopped;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
