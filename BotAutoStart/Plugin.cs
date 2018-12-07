#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Full;
using TS3Client.Messages;
using IniParser;
using IniParser.Model;

namespace BotAutoStartQuery
{
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name;
		public static readonly string Description = "";
		public static readonly string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static readonly string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;

		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}

	public class BotAutoStartQuery : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public TS3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot ConfBot { get; set; }
		public ConfRoot ConfRoot { get; set; }
		public BotManager BotManager { get; set; }
		//public PlayManager BotPlayer { get; set; }
		//public IPlayerConnection PlayerConnection { get; set; }
		//public IVoiceTarget targetManager { get; set; }
		//public ConfHistory confHistory { get; set; }
		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		private static IniData PluginConfig;

		private static Dictionary<int, string> UidCache;

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				var section = "botname";
				PluginConfig[section]["UIDs"] = "";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }

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
			var uids = PluginConfig[has]["UIDs"].Split(',');
			foreach (var uid in uids)
			{
				if (IsOnline(uid)) return;
			}
			var bot = BotByName(has);
			BotManager.StopBot(bot);
		}

		private bool IsOnline(string uid)
		{
			return TS3FullClient.GetClientIds(uid).Value.Length > 0;
		}

		private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			var has = HasAutoStart(e.Uid);
			if (string.IsNullOrEmpty(has)) return;
			Log.Debug($"{e.Name} has a default bot: {has}");
			if (IsBotConnected(has)) return;
			Log.Debug($"{has} is not connected");
			BotManager.RunBotTemplate(has);
			UidCache.Add(e.ClientId, e.Uid);
		}

		private string HasAutoStart(string Uid)
		{
			foreach (var bot in PluginConfig.Sections)
			{
				var uids = bot.Keys["UIDs"].Split(',');
				foreach (var uid in uids)
				{
					if (uid.Trim() == Uid) return bot.SectionName;
				}
			}
			return null;
		}

		private Bot BotByName(string name)
		{
			var botInfoList = BotManager.GetBotInfolist();
			foreach (BotInfo bot in botInfoList)
			{
				if (bot.Name == name)
				{
					using (var botLock = BotManager.GetBotLock(bot.Id.Value))
					{
						return botLock.Bot;
					}
				}
			}
			return null;
		}

		private bool IsBotConnected(string name)
		{
			name = name.Trim();
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
				var matches = bot.Name == name;
				var not_offline = bot.Status != BotStatus.Offline;
				// Log.Debug($"{bot.Name} == {name} && {bot.Status} != {BotStatus.Offline}: {matches} && {not_offline} ({matches && not_offline})");
				if (matches && not_offline)
					return true;
			}
			// Log.Debug("Returning False");
			return false;
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView -= OnEachClientLeftView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
