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
using TS3AudioBot.CommandSystem;
using System.Text;
using LiteDB;
using IniParser;
using IniParser.Model;
using TS3Client.Commands;

namespace ServerLog
{
#region PluginInfo
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
	#endregion
	public class LogEntry
	{
		public DateTime TimeStamp { get; set; }
		public TS3Client.LogLevel LogLevel { get; set; }
		public string Event { get; set; }
		public string Message { get; set; }
	}
	public class ServerLog : IBotPlugin
	{
#region Imports
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Bot Bot { get; set; }
		public Ts3FullClient TS3FullClient { get; set; }
		//public Ts3Client TS3Client { get; set; }
		public ConfBot ConfBot { get; set; }
		public ConfRoot ConfRoot { get; set; }
		public BotManager BotManager { get; set; }
		//public PlayManager BotPlayer { get; set; }
		//public IPlayerConnection PlayerConnection { get; set; }
		//public IVoiceTarget targetManager { get; set; }
		//public ConfHistory confHistory { get; set; }
		#endregion
		#region Variables
		private static string LogPath; private static string IndexPath;
		private static FileIniDataParser ConfigParser;
		private static IniData IndexFile;
		private static LiteDatabase dataBase;
		private List<Tuple<string,string>> monitoring = new List<Tuple<string, string>>();
		private static string lastServerName;
		#endregion
		private ServerLog()
		{
		}

		public void Initialize()
		{
			TS3FullClient.OnEachInitServer += OnEachInitServer;
			LogPath = Path.Combine(ConfRoot.Plugins.Path.Value, PluginInfo.ShortName);
			IndexPath = Path.Combine(LogPath, "index.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(IndexPath))
			{
				if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
				IndexFile = new IniData();
				IndexFile.Sections.Add(new SectionData("servers"));
				ConfigParser.WriteFile(IndexPath, IndexFile);
				Log.Warn("Config for plugin {} created!", PluginInfo.Name);
			}
			else { IndexFile = ConfigParser.ReadFile(IndexPath); }
			TS3FullClient.OnChannelListFinished += OnChannelListFinished;
			var success = InitServer();
			Log.Info("Plugin {0} v{1} by {2} loaded {3}.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author, success);
		}

		private enum LogStatus {
			NOT_CONNECTED,
			ALREADY_MONITORED,
			MONITORING
		}
		private LogStatus InitServer()
		{
			var whoami = TS3FullClient.WhoAmI();
			if (!whoami.Ok) return LogStatus.NOT_CONNECTED;
			var suid = whoami.Value.VirtualServerUid;
			foreach (var server in monitoring) {
				if (server.Item1 == suid) { return LogStatus.ALREADY_MONITORED; }
			}
			dataBase = new LiteDatabase(Path.Combine(LogPath, $"{suid}.db"));
			monitoring.Add(new Tuple<string, string>(suid, Bot.Name));
			IndexFile["servers"][suid] = lastServerName;
			lastServerName = null;
			ConfigParser.WriteFile(IndexPath, IndexFile);
			TS3FullClient.OnEachClientUpdated += OnEachClientUpdated;
			TS3FullClient.
			return LogStatus.MONITORING;
		}

		private void OnEachInitServer(object sender, InitServer e)
		{
			lastServerName = e.Name;
		}

		private void OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e)
		{
			InitServer();
		}

		#region Events
		private void OnEachClientUpdated(object sender, ClientUpdated e)
		{
			var col = dataBase.GetCollection<LogEntry>("Server");
			col.Insert(new LogEntry{ TimeStamp = DateTime.Now, LogLevel = TS3Client.LogLevel.Info, Event = "Client Updated", Message = $"Client ID: {e.ClientId}" });
		}
		#endregion
		#region Commands
		[Command("serverlog status", "")]
		public string CommandServerLogStatus()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"{PluginInfo.Name} Status:");
			sb.AppendLine($"Bot: {Bot.Name}");
			var suid = TS3FullClient.WhoAmI().Value.VirtualServerUid;
			sb.AppendLine($"DB: {Path.Combine(LogPath, $"{suid}.db")}");
			sb.AppendLine($"Monitoring {monitoring.Count}: {monitoring.ToString()}");
			sb.AppendLine($"Current: {suid}");
			return sb.ToString();
		}
		#endregion
		public void Dispose() {
			TS3FullClient.OnEachClientUpdated -= OnEachClientUpdated;
			TS3FullClient.OnChannelListFinished -= OnChannelListFinished;
			TS3FullClient.OnEachInitServer -= OnEachInitServer;
			dataBase.Dispose();
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
