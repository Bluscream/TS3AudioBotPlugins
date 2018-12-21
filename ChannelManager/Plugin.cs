using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TS3AudioBot;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3Client;
using TS3Client.Full;
using TS3Client.Commands;
using TS3Client.Messages;
using LiteDB;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;

namespace ChannelManager
{
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name;
		public static readonly string Description;
		public static readonly string Url;
		public static readonly string Author = "Bluscream";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;
		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}
	public class DBEntry
	{
		public DateTime TimeStamp { get; set; }
		public string InvokerUid { get; set; }
		public string ServerUid { get; set; }
		public string ClientUid { get; set; }
	}

public class ChannelManager : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private enum ChannelGroup {
			ADMIN = 0,
			MOD,
			DEFAULT,
			BANNED,
			OTHER
		}

		private static readonly string[] admin_names = { "admin", "owner" };
		private static readonly string[] mod_names = { "mod", "operator" };
		private static readonly string[] banned_names = { "ban", "not welcome" };

		private static LiteDatabase dataBase;

		private ChannelIdT ownChannel = 0; private ChannelGroup ownGroup = ChannelGroup.DEFAULT;
		private ulong ChannelBanGroup = 0; private ulong ChannelModGroup = 0; private ulong ChannelAdminGroup = 0; private ulong ChannelDefaultGroup = 0;

		public void Initialize()
		{
			var dbPath = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.db");
			dataBase = new LiteDatabase(dbPath);
			TS3FullClient.OnEachInitServer += OnEachInitServer;
			TS3FullClient.OnEachServerUpdated += OnEachServerUpdated;
			TS3FullClient.OnChannelListFinished += OnChannelListFinished;
			TS3FullClient.OnEachClientChannelGroupChanged += OnEachClientChannelGroupChanged;
			TS3FullClient.OnEachChannelGroupList += OnEachChannelGroupList;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void Load()
		{
			var _void = TS3FullClient.SendNotifyCommand(new Ts3Command("servergetvariables", new List<ICommandPart>() { }), NotificationType.ServerUpdated).Value; // ChannelAdminGroup

			var result = TS3FullClient.SendNotifyCommand(new Ts3Command("serverinfo", new List<ICommandPart>() { }), NotificationType.ServerInfo).Value; // ChannelDefaultGroup
			// result.
		}

		private void OnEachServerUpdated(object sender, ServerUpdated e)
		{
			ChannelAdminGroup = e.DefaultChannelAdminGroup;
		}

		private void OnEachInitServer(object sender, InitServer e)
		{
			ChannelDefaultGroup = e.DefaultChannelGroup;
		}

		private void OnEachChannelGroupList(object sender, ChannelGroupList e)
		{
			if (e.GroupType != GroupType.Regular) return;
			var name = e.Name.Trim().ToLower();
			foreach (var _name in mod_names)
			{
				if (name.Contains(_name)) ChannelModGroup = e.ChannelGroup;
			}
			foreach (var _name in banned_names)
			{
				if (name.Contains(_name)) ChannelBanGroup = e.ChannelGroup;
			}
			if (ChannelAdminGroup != 0) return;
			foreach (var _name in admin_names)
			{
				 if (name.Contains(_name)) ChannelAdminGroup = e.ChannelGroup;
			}
		}

		private void OnEachClientChannelGroupChanged(object sender, ClientChannelGroupChanged client)
		{
			if (client.ClientId == TS3FullClient.ClientId)
			{
				Log.Debug("My Channel group was changed to {}", client.ChannelGroup);
				if (client.ChannelGroup == ChannelAdminGroup) {
					ownGroup = ChannelGroup.ADMIN; ownChannel = client.ChannelId;
				} else if (client.ChannelGroup == ChannelModGroup) {
					ownGroup = ChannelGroup.MOD; ownChannel = client.ChannelId;
				} else if (client.ChannelGroup == ChannelDefaultGroup) {
					ownGroup = ChannelGroup.DEFAULT; ownChannel = client.ChannelId;
				} else if (client.ChannelGroup == ChannelBanGroup) {
					ownGroup = ChannelGroup.BANNED; ownChannel = client.ChannelId;
				} else {
					ownGroup = ChannelGroup.OTHER; ownChannel = 0;
				}
				return;
			}
		}

		private void OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e)
		{
			Log.Debug("OnChannelListFinished");
		}

		[Command("cm tp", "")]
		public string CommandSetAutoTalkPower(string uid)
		{
			var col = dataBase.GetCollection<DBEntry>("TP");
			// col.Find();
			// col.Insert(new DBEntry { TimeStamp = DateTime.Now, LogLevel = TS3Client.LogLevel.Info, Event = "Client Updated", Message = $"Client ID: {e.ClientId}" });
			return PluginInfo.Name + " is now ";
		}

		[Command("cm mod", "")]
		public string CommandSetAutoChannelMod(string uid)
		{
			return PluginInfo.Name + " is now ";
		}

		[Command("cm ban", "")]
		public string CommandSetAutoChannelBan(string uid)
		{
			return PluginInfo.Name + " is now ";
		}

		[Command("cm load", "")]
		public string CommandLoad()
		{
			Load();
			return CommandInfo();
		}

		[Command("cm info", "")]
		public string CommandInfo()
		{
			var sb = new StringBuilder(PluginInfo.Name);
			sb.AppendLine();
			sb.AppendLine($"admin_names: {string.Join(", ", admin_names)}");
			sb.AppendLine($"mod_names: {string.Join(", ", mod_names)}");
			sb.AppendLine($"banned_names: {string.Join(", ", banned_names)}");
			sb.AppendLine();
			sb.AppendLine($"ownChannel: {ownChannel}");
			sb.AppendLine($"ownGroup: {ownGroup}");
			sb.AppendLine();
			sb.AppendLine($"ChannelAdminGroup: {ChannelAdminGroup}");
			sb.AppendLine($"ChannelModGroup: {ChannelModGroup}");
			sb.AppendLine($"ChannelDefaultGroup: {ChannelDefaultGroup}");
			sb.AppendLine($"ChannelBanGroup: {ChannelBanGroup}");
			return sb.ToString();
		}

		public void Dispose()
		{
			TS3FullClient.OnEachInitServer -= OnEachInitServer;
			TS3FullClient.OnEachServerUpdated -= OnEachServerUpdated;
			TS3FullClient.OnEachChannelGroupList -= OnEachChannelGroupList;
			TS3FullClient.OnEachClientChannelGroupChanged -= OnEachClientChannelGroupChanged;
			TS3FullClient.OnChannelListFinished -= OnChannelListFinished;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
