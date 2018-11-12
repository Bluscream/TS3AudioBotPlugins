using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;

namespace SimpleTTS
{
	public class PluginInfo {
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "";
		public const string Url = "https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/SimpleTTS";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}
	public class SimpleTTS : IBotPlugin {
		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }
		// { "UK English Female", "UK English Male", "US English Female", "Spanish Female", "French Female", "Deutsch Female", "Italian Female", "Greek Female", "Hungarian Female", "Turkish Female", "Russian Female", "Dutch Female", "Swedish Female", "Norwegian Female", "Japanese Female", "Korean Female", "Chinese Female", "Hindi Female", "Serbian Male", "Croatian Male", "Bosnian Male", "Romanian Male", "Catalan Male", "Australian Female", "Finnish Female", "Afrikaans Male", "Albanian Male", "Arabic Male", "Armenian Male", "Czech Female", "Danish Female", "Esperanto Male", "Hatian Creole Female", "Icelandic Male", "Indonesian Female", "Latin Female", "Latvian Male", "Macedonian Male", "Moldavian Male", "Montenegrin Male", "Polish Female", "Brazilian Portuguese Female", "Portuguese Female", "Serbo-Croatian Male", "Slovak Female", "Spanish Latin American Female", "Swahili Male", "Tamil Male", "Thai Female", "Vietnamese Male", "Welsh Male" };
		public string[] TTSLocales = { "af-ZA", "ar-SA", "bs", "ca-ES", "cs-CZ", "cy", "da-DK", "de-DE", "el-GR", "en-AU", "en-GB", "en-US", "eo", "es-ES", "es-MX", "fi-FI", "fr-FR", "hi-IN", "hr-HR", "hu-HU", "hy-AM", "id-ID", "is-IS", "it-IT", "ja-JP", "ko-KR", "la", "lv-LV", "md", "me", "mk-MK", "nb-NO", "nl-NL", "pl-PL", "pt-BR", "ro-RO", "ru-RU", "sk-SK", "sq-AL", "sr-RS", "sv-SE", "sw-KE", "th-TH", "tr-TR", "vi-VN", "zh-CN", "zh-HK", "zh-TW" };
		public string[] TTSGenders = { "male", "female" };
		public string TTSUrl = "http://code.responsivevoice.org/getvoice.php?t={text}&tl={locale}&gender={gender}&pitch={pitch}&rate={rate}&vol={volume}"; // &sv=&vn=
		public string TTSLocale = "en-US";
		public string TTSGender = "male";
		public string TTSPitch = "0.5";
		public string TTSRate = "0.5";
		public string TTSVolume = "1";

		public void Initialize() {
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		[Command("say", "Syntax: !say <text>")]
		public string CommandSay(params string[] _text) {
			var text = Uri.EscapeUriString(string.Join(" ", _text));
			var url_ = TTSUrl.Replace("{text}", text).Replace("{locale}", TTSLocale).Replace("{pitch}", TTSPitch).Replace("{rate}", TTSRate).Replace("{volume}", TTSVolume);
			// PluginLog(LogLevel.Debug, url_);
			return "[url]" + url_ + "[/url]";
			//BotPlayer.ResourceFactoryManager.Load(new TS3AudioBot.ResourceFactories.AudioResource());
		}

		[Command("tts locale", "Syntax: !tts <locale>")]
		public string CommandSetLocale(string locale)
		{
			if (!TTSLocales.Contains(locale)) return $"Failed to set locale! Make sure it's a valid locale from {string.Join(",", TTSLocales)}";
			TTSLocale = locale;
			return $"Set SimpleTTS locale to [b]{TTSLocale}[/b]";
		}

		[Command("tts gender", "Syntax: !tts gender <male|female>")]
		public string CommandSetGender(string gender)
		{
			if (!TTSGenders.Contains(gender)) return $"Failed to set locale! Make sure it's a valid value from {string.Join(",", TTSGenders)}";
			TTSGender = gender;
			return $"Set SimpleTTS gender to [b]{TTSGender}[/b]";
		}

		[Command("tts pitch", "Syntax: !tts <pitch (0.0-1.0)>")]
		public string CommandSetPitch(string pitch)
		{
			var success = double.TryParse(pitch, out double s);
			if (!success) return "Failed to set pitch! Make sure it's a valid value between 0.0 and 1.0";
			TTSPitch = pitch;
			return $"Set SimpleTTS pitch to [b]{TTSPitch}[/b]";
		}

		[Command("tts rate", "Syntax: !tts <rate (0.0-1.0)>")]
		public string CommandSetRate(string rate)
		{
			var success = double.TryParse(rate, out double s);
			if (!success) return "Failed to set rate! Make sure it's a valid value between 0.0 and 1.0";
			TTSRate = rate;
			return $"Set SimpleTTS rate to [b]{TTSRate}[/b]";
		}

		[Command("tts volume", "Syntax: !tts <volume (0.0-1.0)>")]
		public string CommandSetVolume(string volume)
		{
			var success = double.TryParse(volume, out double s);
			if (!success) return "Failed to set volume! Make sure it's a valid value between 0.0 and 1.0";
			TTSVolume = volume;
			return $"Set SimpleTTS volume to [b]{TTSVolume}[/b]";
		}

		public void Dispose() {
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
