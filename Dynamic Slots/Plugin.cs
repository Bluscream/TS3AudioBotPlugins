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
using TS3Client.Commands;

using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ClientUidT = System.String;
using TS3Client.Messages;
using System.Text;
using TS3AudioBot.Sessions;

namespace Dynamic_Slots
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

	public class Dynamic_Slots : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private bool PluginEnabled = true;
		private int CurrentUsers = 0;
		private int CurrentUsersQueries = 0;
		private int CurrentVisibleUsers = 0;
		private int CurrentSlots;
		// private bool CheckNext = false;
		private bool InitializedVisible = false;
		private bool InitializedUsers = false;
		private List<ClientEnterView> clientCache;

		private readonly List<int> steps = new List<int>() { 32, 64, 128, 256, 512, 1024 };

		public void Initialize()
		{
			clientCache = new List<ClientEnterView>();
			InitializedVisible = false; InitializedUsers = false; ServerGetVariables();
			TS3FullClient.OnEachInitServer += OnEachInitServer;
			TS3FullClient.OnEachServerEdited += OnEachServerEdited;
			TS3FullClient.OnEachServerUpdated += OnEachServerUpdated;
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}
		private void OnEachInitServer(object sender, InitServer server) => CheckSlots(server.MaxClients);
		private void OnEachServerUpdated(object sender, ServerUpdated server){
			CheckSlots(server.MaxClients, server.ReservedSlots);
			CurrentUsersQueries = server.ClientsOnline; // - server.QueriesOnline;
			CurrentUsers = server.ClientsOnline - server.QueriesOnline;
			if (!InitializedUsers)
			{
				InitializedUsers = true;
				Log.Debug("Initialized user counts: CurrentUsersQueries: {} CurrentUsers: {}",CurrentUsersQueries,CurrentUsers);
			}

		}
		private bool CheckSlots(int maxclients, int reserved = 0)
		{
			var realSlots = maxclients - reserved;
			if (realSlots != CurrentSlots)
			{
				Log.Info("Slots changed from {} to {} ({}-{}). Updating...", CurrentSlots, realSlots, maxclients, reserved);
				CurrentSlots = realSlots;
				return true;
			}
			return false;
		}
		private void ServerGetVariables() {
			var Ok = TS3FullClient.Send<ResponseVoid>("servergetvariables", new List<ICommandPart>() { }).Ok;
		}
		private void CheckClients()
		{
			if (!InitializedVisible) {
				if (InitializedUsers && (CurrentUsersQueries >= CurrentSlots)) {
					Log.Info("Editing Maxclients to {} because {} >= {} - 1", CurrentUsersQueries + 1, CurrentUsersQueries, CurrentSlots);
					EditMaxClients(CurrentUsersQueries + 1);
					ServerGetVariables();
				}
				return;
			}
			if (CurrentVisibleUsers == (CurrentSlots - 1)) {
				Log.Info("Editing Maxclients to {} because {} == {} - 1", CurrentVisibleUsers + 1, CurrentVisibleUsers, CurrentSlots);
				EditMaxClients(CurrentVisibleUsers + 1);
			} else if (CurrentSlots == (CurrentVisibleUsers + 1))
			{
				Log.Info("Editing Maxclients to {} because {} > {} + 1", CurrentVisibleUsers - 1, CurrentVisibleUsers, CurrentSlots);
				EditMaxClients(CurrentVisibleUsers - 1);
			}
		}

		private void OnEachServerEdited(object sender, ServerEdited server)
		{
			// if (CheckNext) return;
			if (server.InvokerId == TS3FullClient.ClientId) return;
			// CheckNext = true;
			ServerGetVariables();
			// var result = TS3FullClient.SendNotifyCommand(new Ts3Command("serverinfo", new List<ICommandPart>() { }), NotificationType.ServerInfo).Value;
			// var result = TS3FullClient.Send<ResponseVoid>("serverinfo", new List<ICommandPart>() { });
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			// if (client.ClientType == ClientType.Query) return; // TODO: FIX
			// var clientInfo = TS3Client.GetClientInfoById(client.ClientId)
			clientCache.Add(client);
			CurrentVisibleUsers += 1;
			Log.Debug("User joined: {} ({}): Increasing CurrentVisibleUsers to {} / {}", client.Name,client.ClientId,CurrentVisibleUsers,CurrentSlots);
			CheckClients();
			// Log.Debug("InitializedUsers: {} !InitializedVisible: {} clientCache.Count: {} >= {} === {}", InitializedUsers, !InitializedVisible, clientCache.Count, CurrentUsersQueries, (InitializedUsers && !InitializedVisible && (clientCache.Count >= CurrentUsersQueries)));
			if (InitializedUsers && !InitializedVisible && (clientCache.Count >= CurrentUsersQueries)) {
				Log.Debug("InitializedVisible because clientCache.Count: {} >= {}", clientCache.Count, CurrentUsersQueries);
				InitializedVisible = true;
			}
		}

		private void OnEachClientLeftView(object sender, ClientLeftView client)
		{
			ClientEnterView found = null;
			foreach (var cachedClient in clientCache) {
				if (cachedClient.ClientId == client.ClientId) {
					found = cachedClient; break;
				}
			}
			if (found == null) return;
			// if (found.ClientType == ClientType.Query) return; // TODO: FIX
			CurrentVisibleUsers -= 1;
			Log.Debug("User left: {}: Decreasing CurrentVisibleUsers to {} / {}", client.ClientId, CurrentVisibleUsers, CurrentSlots);
			clientCache.Remove(found);
			CheckClients();
		}

		private bool EditMaxClients(int maxClients)
		{ // serveredit [sid=6] virtualserver_maxclients=63
			var command = new Ts3Command("serveredit", new List<ICommandPart>() {
					new CommandParameter("virtualserver_maxclients", maxClients)
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ServerEdited);
			return Result.Ok;
		}

		[Command("dynamicslots", "")]
		public string CommandListWaiting()
		{
			var sb = new StringBuilder(PluginInfo.Name);
			sb.AppendLine();
			sb.AppendLine($"Clients: {CurrentVisibleUsers} ({CurrentUsersQueries}) / {CurrentSlots}");
			sb.AppendLine($"PluginEnabled: {PluginEnabled}");
			// sb.AppendLine($"CheckNext: {CheckNext}");
			sb.AppendLine($"InitializedVisible: {InitializedVisible}");
			sb.AppendLine($"clientCache: {clientCache.Count}");
			sb.AppendLine($"steps: {string.Join(", ", steps)}");
			return sb.ToString();
		}

		public void Dispose()
		{
			clientCache.Clear();CurrentSlots = 0;CurrentVisibleUsers = 0;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachServerUpdated -= OnEachServerUpdated;
			TS3FullClient.OnEachServerEdited -= OnEachServerEdited;
			TS3FullClient.OnEachInitServer -= OnEachInitServer;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
