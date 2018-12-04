#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.SQLite;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Full;
using TS3Client.Messages;
using TS3AudioBot.CommandSystem;
using System.Text;
using LiteDB;

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
		private static string LogDBFile;
		private static SQLiteConnection dataBase;
		private List<Tuple<string,string>> monitoring = new List<Tuple<string, string>>();
#endregion
		// private ServerLog() {}

		private int SQLExec(string sql)
		{
			var cmd = new SQLiteCommand(sql, dataBase);
			var result = SQLExec(cmd);
			return result;
		}
		private int SQLExec(SQLiteCommand cmd)
		{
			Log.Debug("{}: Executing SQL \"{}\"", Bot.Name, cmd.CommandText);
			var result = cmd.ExecuteNonQuery();
			Log.Debug("{}: Executed SQL. Affected Rows: {}", Bot.Name, result);
			return result;
		}

		private int LogDB(DateTime _timestamp, TS3Client.LogLevel _logLevel, string Event, string message)
		{
			string queryString = $"INSERT INTO Log( timestamp, level, event, message) VALUES (@timestamp, @level, @event, @message);";
			SQLiteCommand cmd = new SQLiteCommand(dataBase);
			cmd.CommandType = System.Data.CommandType.Text;
			cmd.CommandText = queryString;
			cmd.Parameters.AddWithValue("@timestamp", _timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
			cmd.Parameters.AddWithValue("@level", _logLevel.ToString());
			cmd.Parameters.AddWithValue("@event", Event);
			cmd.Parameters.AddWithValue("@message", message);
			return SQLExec(cmd);
		}

		public void Initialize()
		{
			// var firstStart = false;
			LogDBFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.db");
			if (!File.Exists(LogDBFile))
			{
				SQLiteConnection.CreateFile(LogDBFile);
				// firstStart = true;
			}
			SQLiteConnection dataBase = new SQLiteConnection($"Data Source={LogDBFile};Version=3;");
			dataBase.Open();
			// if (firstStart) {
			TS3FullClient.OnChannelListFinished += OnChannelListFinished;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e)
		{
			var suid = TS3FullClient.WhoAmI().Value.VirtualServerUid;
			SQLExec($"CREATE TABLE IF NOT EXISTS {suid} (\"id\" INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE, \"timestamp\" TIMESTAMP NOT NULL, \"level\" TEXT NOT NULL, \"event\" TEXT NOT NULL, \"message\"  TEXT NOT NULL);");
			monitoring.Add(new Tuple<string, string>(suid, Bot.Name));
			TS3FullClient.OnEachClientUpdated += OnEachClientUpdated;
		}

		#region Events
		private void OnEachClientUpdated(object sender, ClientUpdated e)
		{
			LogDB(DateTime.Now, TS3Client.LogLevel.Info, "Client Updated", e.ClientId.ToString());
		}
		#endregion
		#region Commands
		[Command("serverlog status", "")]
		public string CommandServerLogStatus()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"{PluginInfo.Name} Status:");
			sb.AppendLine($"Bot: {Bot.Name}");
			sb.AppendLine($"DB: {LogDBFile}");
			sb.AppendLine($"State: {dataBase.State.ToString()}");
			sb.AppendLine($"Monitoring {monitoring.Count}: {monitoring.ToString()}");
			var suid = TS3FullClient.WhoAmI().Value.VirtualServerUid;
			sb.AppendLine($"Current: {suid}");
			return sb.ToString();
		}
		#endregion
		public void Dispose() {
			TS3FullClient.OnEachClientUpdated -= OnEachClientUpdated;
			TS3FullClient.OnChannelListFinished -= OnChannelListFinished;
			switch (dataBase.State)
			{
				case System.Data.ConnectionState.Connecting:
				case System.Data.ConnectionState.Executing:
				case System.Data.ConnectionState.Fetching:
				case System.Data.ConnectionState.Open:
					dataBase.Close();
					break;
				default:
					break;
			}
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
