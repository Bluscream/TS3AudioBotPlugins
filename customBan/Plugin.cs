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
using TS3Client.Full;
using TS3Client.Audio;
using TS3Client;
using TS3AudioBot.History;
using TS3AudioBot.Helper;
using Newtonsoft.Json;
using IniParser;
using IniParser.Model;
using TS3Client.Commands;

using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ClientUidT = System.String;
using TS3Client.Messages;
using System.Text;
using TS3AudioBot.Sessions;

namespace customBan
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

	public class templates
	{
		public string Prefix { get; set; }
		public string Suffix { get; set; }
		public Dictionary<string, string> Templates { get; set; }
	}

	public class customBan : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		private static IniData PluginConfig;

		// ^((?P<years>\d+?)y)?((?P<months>\d+?)M)?((?P<days>\d+?)d)?((?P<hours>\d+?)h)?((?P<minutes>\d+?)m)?((?P<seconds>\d+?)s)?$
		private static readonly Regex regex_time = new Regex(@"^((\d+?)y)?((\d+?)M)?((\d+?)d)?((\d+?)h)?((\d+?)m)?((\d+?)s)?$");

		// private string templateURL = "https://raw.githubusercontent.com/MexoGames/Teamspeak/master/templates/bans.json";
		// private string whitelistURL = "https://raw.githubusercontent.com/MexoGames/Teamspeak/master/templates/ip-whitelist.txt";


		private templates banTemplates;
		private string[] ipWhitelist;

		public static string TruncateLongString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return str.Substring(0, Math.Min(str.Length, maxLength));
		}

		public void Initialize()
		{
			LoadConfig();
			LoadTemplates();
			LoadWhitelist();
			//PlayManager.BeforeResourceStopped += BeforeResourceStopped;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void LoadTemplates()
		{
			using (WebClient wc = new WebClient())
			{
				var json = wc.DownloadString(PluginConfig["general"]["template"]);
				banTemplates = JsonConvert.DeserializeObject<templates>(json);
			}
		}
		private void LoadWhitelist()
		{
			using (WebClient wc = new WebClient())
			{
				var whitelist_str = wc.DownloadString(PluginConfig["general"]["whitelist"]);
				ipWhitelist = whitelist_str.Split(new string[] { Environment.NewLine, "\n", "\"r" }, StringSplitOptions.None);
			}
		}

		private void LoadConfig()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				var section = "general";
				PluginConfig[section]["template"] = string.Empty;
				PluginConfig[section]["whitelist"] = string.Empty;
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
		}
		private bool BanClient(ClientIdT clientId, ulong duration, string reason)
		{ // banclient uid=NndkCcFnoemS6mjQscpryybk6As= time=1 banreason=1 return_code=1:5z:0
			var command = new Ts3Command("banclient", new List<ICommandPart>() {
					new CommandParameter("time", duration),
					new CommandParameter("clid", clientId),
					new CommandParameter("banreason", TruncateLongString(reason, 80))
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}

		/*private ulong ConvertDuration(string input)
		{
			var match = regex_time.Match(input);
			match.
			return;
		}*/

		public static TimeSpan ParseTimeSpan(String value)
		{
			Dictionary<String, long> seconds = new Dictionary<String, long>() {
			{"y", 31536000 },
			{"M", 2629746},
			{"w", 604800},
			{"d", 86400},
			{"h", 3600},
			{"m", 60},
			{"s", 1},
			{"permanent", 0},
			{"perm", 0},
			{"p", 0},
		  };
			String[] items = value.Split();
			long result = 0;
			Log.Warn("value " + value);
			Log.Warn("items.Length " + items.Length);
			for (int i = 0; i < items.Length - 1; i += 1) {
				result += long.Parse(items[i]) * seconds[items[i + 1]];
			}
			Log.Warn("result " + result);
			return TimeSpan.FromSeconds(result);
		}

		[Command("ban", "Syntax: !ban <reason> <name>")]
		public string CommandBan(InvokerData invoker, UserSession session = null, string reason = "", params string[] _name)
		{
			var name = string.Join(" ", _name);
			var found = 0;
			var lowerReason = reason.ToLower();
			TimeSpan duration;
			foreach (string key in banTemplates.Templates.Keys)
			{
				if (key.ToLower().Contains(lowerReason)) {
					reason = key;
					var mduration_str = banTemplates.Templates[key];
					Log.Info("mduration_str " + mduration_str);
					Log.Info(ParseTimeSpan(mduration_str).TotalSeconds);
					Log.Info((ulong)ParseTimeSpan(mduration_str).TotalSeconds);
					duration = ParseTimeSpan(mduration_str);
					found+=1;break;
				}
			}
			if (found < 1) return "[color=red]No matching reason found, try again!";
			else if (found > 1) return "[color=red]Too many matching reasons found, try again!";
			var client = TS3Client.GetClientByName(name).UnwrapThrow();
			string ResponseQuit(string message)
			{
				if (TextUtil.GetAnswer(message) == Answer.Yes)
				{
					var sb = new StringBuilder(reason);
					if (!string.IsNullOrEmpty(banTemplates.Prefix))
					{
						sb.Append(banTemplates.Prefix.Replace("%duration%", duration.TotalSeconds.ToString()).Replace("%ownnick%", invoker.NickName));
					}
					if (!string.IsNullOrEmpty(banTemplates.Suffix)) {
						sb.Append(banTemplates.Suffix.Replace("%duration%", duration.TotalSeconds.ToString()).Replace("%ownnick%", invoker.NickName));
					}
					
					var Reason = sb.ToString();
					var success = BanClient(client.ClientId, (ulong)duration.TotalSeconds, Reason);
					if (success)
					{
						return $"Banned client \"{client.Name}\" for \"{reason}\"";
					} else
					{
						return $"[color=red]Failed to ban [b]\"{client.Name}\" ( {client.ClientId} )";
					}
				}
				return null;
			}
			session.SetResponse(ResponseQuit);
			var duration_str = (duration.TotalSeconds < 1) ? "Permanent" : duration.ToString();
			return $"[color=orange]Sure that you want to ban \"{client.Name}\" for \"{reason}\" (\"{duration_str}\")? (!yes | !no)";
		}

		public void Dispose()
		{
			//PlayManager.AfterResourceStopped -= AfterResourceStopped;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
