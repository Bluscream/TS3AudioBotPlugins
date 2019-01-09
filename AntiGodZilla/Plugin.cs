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
using System.Text;
using TS3AudioBot.Helper;
using TS3AudioBot.Web.Api;
using TS3AudioBot.Sessions;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ClientUidT = System.String;

namespace AntiGodZilla
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
	public class AntiGodZilla : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		// private const string Description = "";
		private const string MetaData = "GodZilla"; // [AGodZilla] This program was developed by Dolo.

		private const string KickReason = "Du kommscht hier net rein!";
		private const string BanReason = "Botting!";
		private static readonly TimeSpan BanTime = TimeSpan.FromMinutes(1);

		public static string TruncateLongString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return str.Substring(0, Math.Min(str.Length, maxLength));
		}

		public void Initialize()
		{
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			CheckClient(client);
		}

		private void CheckClient(ClientEnterView client)
		{
			if (client.ClientType == ClientType.Query) return;
			if (client.ClientId == TS3FullClient.ClientId) return;
			// var hasDesc = client.Description.Contains(Description);
			var hasMeta = client.Metadata.Contains(MetaData);
			if (/*hasDesc ||*/ hasMeta) {
				TakeAction(client.Uid);
			}
		}

		private bool TakeAction(ClientUidT clientUid) => KickFromServer(clientUid);

		private bool KickFromServer(ClientIdT clientId)
		{
			var command = new Ts3Command("clientkick", new List<ICommandPart>() {
					new CommandParameter("reasonid", (int)ReasonIdentifier.Server),
					new CommandParameter("clid", clientId),
					new CommandParameter("reasonmsg", TruncateLongString(KickReason, 80))
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}
		private bool KickFromServer(ClientUidT clientUid)
		{
			var command = new Ts3Command("clientkick", new List<ICommandPart>() {
					new CommandParameter("reasonid", (int)ReasonIdentifier.Server),
					new CommandParameter("uid", clientUid),
					new CommandParameter("reasonmsg", TruncateLongString(KickReason, 80))
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}
		private bool BanClient(ClientIdT clientId)
		{ // banclient uid=NndkCcFnoemS6mjQscpryybk6As= time=1 banreason=1 return_code=1:5z:0
			var command = new Ts3Command("banclient", new List<ICommandPart>() {
					new CommandParameter("time", BanTime),
					new CommandParameter("clid", clientId),
					new CommandParameter("banreason", TruncateLongString(BanReason, 80))
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}
		private bool BanClient(ClientUidT clientUid)
		{
			var command = new Ts3Command("banclient", new List<ICommandPart>() {
					new CommandParameter("time", BanTime),
					new CommandParameter("uid", clientUid),
					new CommandParameter("banreason", TruncateLongString(BanReason, 80))
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}
		/*
		private void CheckAllClients()
		{
			var clients = TS3FullClient.ClientList(ClientListOptions.uid).Value;
			foreach (var client in clients)
			{
				OnEachClientEnterView(null, (ClientEnterView)client);
			}
		}
		
		[Command("agz checkall", "")]
		public string CommandCheckAllClients()
		{
			CheckAllClients();
			return "Checked all clients";
		}
		*/
		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
