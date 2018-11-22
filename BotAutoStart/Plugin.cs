#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Full;
using TS3Client.Messages;
using System.Diagnostics;

namespace BotAutoStart
{
	public class PluginInfo
	{
		public static readonly string ShortName = typeof(PluginInfo).Namespace;
		public static readonly string Name = string.IsNullOrEmpty(Assembly.GetExecutingAssembly().GetName().Name) ? ShortName : Assembly.GetExecutingAssembly().GetName().Name;
		public static string Description = "";
		public static string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		public PluginInfo()
		{
			var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
			Description = versionInfo.FileDescription;
			Author = versionInfo.CompanyName;
		}
	}
	public class BotAutoStart : IBotPlugin
	{
		//private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		//public Ts3Client TS3Client { get; set; }
		public ConfBot ConfBot { get; set; }
		public ConfRoot ConfRoot { get; set; }
		public BotManager BotManager { get; set; }
		//public PlayManager BotPlayer { get; set; }
		//public IPlayerConnection PlayerConnection { get; set; }
		//public IVoiceTarget targetManager { get; set; }
		//public ConfHistory confHistory { get; set; }

		private static string PluginConfigFile;
		/*private static FileIniDataParser ConfigParser;
		private static IniData PluginConfig;*/
		private static Dictionary<string, string> PluginConfig = new Dictionary<string, string>();

		private static Dictionary<int, string> UidCache;

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			if (!File.Exists(PluginConfigFile))
			{
				File.CreateText(PluginConfigFile).Dispose();
				throw new Exception($"Config file {PluginConfigFile} not found, creating... Please add new entries in the format [b]uid:template name without bot_[/b]");
				/*var botSection = new SectionData("Bots");
				PluginConfig.Sections.Add(botSection);
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig, System.Text.Encoding.UTF8);*/
			}
			foreach (var _line in File.ReadAllLines(PluginConfigFile))
			{
				try {
					if (_line.StartsWith("[") || _line.StartsWith("#")) continue;
					var line = _line.Split(':');
					PluginConfig.Add(line[0], line[1]);
				} catch {
					Log.Warn($"Malformed Line in {PluginConfigFile}: \"{_line}\"");
				}
			}
			/*ConfigParser = new FileIniDataParser();
			PluginConfig = ConfigParser.ReadFile(PluginConfigFile);*/

			UidCache = new Dictionary<int, string>();

			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachClientLeftView(object sender, ClientLeftView e)
		{
			if (!UidCache.ContainsKey(e.ClientId)) return;
			var has = HasAutoStart(UidCache[e.ClientId]);
			UidCache.Remove(e.ClientId);
			if (string.IsNullOrEmpty(has)) return;
			if (!IsBotConnected(has)) return;
			var bot = BotByName(has);
			BotManager.StopBot(bot);
		}

		private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			var has = HasAutoStart(e.Uid);
			if (string.IsNullOrEmpty(has)) return;
			if (!IsBotConnected(has)) return;
			BotManager.RunBotTemplate(has);
			UidCache.Add(e.ClientId,e.Uid);
		}

		private string HasAutoStart(string Uid) {
			foreach (var bot in PluginConfig) {
				if (bot.Key == Uid) return bot.Value;
			}
			return null;
		}

		private Bot BotByName(string name) {
			var botInfoList = BotManager.GetBotInfolist();
			foreach (BotInfo bot in botInfoList) {
				if (bot.Name == name) {
					using (var botLock = BotManager.GetBotLock(bot.Id.Value)) {
						return botLock.Bot;
					}
				}
			}
			return null;
		}

		private bool IsBotConnected(string name) {
			var botInfoList = BotManager.GetBotInfolist();
			var botConfigList = ConfRoot.GetAllBots();
			var infoList = new Dictionary<string, BotInfo>();
			foreach (var botInfo in botInfoList.Where(x => !string.IsNullOrEmpty(x.Name)))
				infoList[botInfo.Name] = botInfo;
			foreach (var botConfig in botConfigList)
			{
				if (infoList.ContainsKey(botConfig.Name))
					continue;
				infoList[botConfig.Name] = new BotInfo
				{
					Id = null,
					Name = botConfig.Name,
					Server = botConfig.Connect.Address,
					Status = BotStatus.Offline,
				};
			}
			foreach (var _bot in infoList)
			{
				var bot = _bot.Value;
				if (bot.Name == name && bot.Status != BotStatus.Offline)
					return true;
			}
			return false;
		}

		public void Dispose() {
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
