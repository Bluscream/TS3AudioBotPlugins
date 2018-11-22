using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Reflection;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client.Audio;
using TS3Client;
using Nett;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using System.Diagnostics;

namespace BotAutoStart
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
	public class YoutubeLive : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		//public Ts3Client TS3Client { get; set; }
		//public ConfBot ConfBot { get; set; }
		public ConfRoot ConfRoot { get; set; }
		public BotManager BotManager { get; set; }
		//public PlayManager BotPlayer { get; set; }
		//public IPlayerConnection PlayerConnection { get; set; }
		//public IVoiceTarget targetManager { get; set; }
		//public ConfHistory confHistory { get; set; }

		private static readonly string PluginConfigFile = $"{PluginInfo.ShortName}.toml";
		private static readonly Configuration PluginConfig;
		private static TomlSettings TomlSettings;

		private static readonly WebClient wc = new WebClient();

		public void Initialize()
		{
			if (!File.Exists(PluginConfigFile))
			{
				TomlSettings = TomlSettings.Create();
				/*PluginConfig = new Configuration()
				{
					EnableDebug = true,
					Server = new Server() { Timeout = TimeSpan.FromMinutes(1) },
					Client = new Client() { ServerAddress = "http://127.0.0.1:8080" },
				};

				Toml.WriteFile(PluginConfig, PluginConfigFile);*/
			} else {
				//var config = Toml.ReadFile<Configuration>(PluginConfigFile);
				TomlTable table = Toml.ReadFile(PluginConfigFile);
				var timeout = table.Get<TomlTable>("Bots").Get<string>("Name");
			}
			
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachClientLeftView(object sender, ClientLeftView e)
		{
			return;
		}

		private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			return;
		}

		private bool IsBotConnected(string name)
		{
			var botInfoList = BotManager.GetBotInfolist();
			foreach (var bot in botInfoList)
			{
				Log.Warn(bot.Name);
				if (bot.Name == name && bot.Status != BotStatus.Offline)
					return true;
			}
			return false;
		}

		public void Dispose()
		{
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
