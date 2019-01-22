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
		// private int CurrentVisibleUsers = 0;
		// private int CurrentVisibleQueries = 0;
		private int CurrentSlots = 32;
		// private bool CheckNext = false;
		private bool InitializedVisible = false;
		private bool InitializedUsers = false;
		private bool InitializedSlots = false;
		private List<ClientIdT> clientCache;
		private List<ClientIdT> queryCache;

		private readonly List<int> steps = new List<int>() { 5, 10, 32, 64, 128, 256, 512, 1024 };

		public void Initialize()
		{
			clientCache = new List<ClientIdT>(); queryCache = new List<ClientIdT>();
			InitializedVisible = false; InitializedUsers = false; ServerGetVariables();
			TS3FullClient.OnEachInitServer += OnEachInitServer;
			TS3FullClient.OnEachServerEdited += OnEachServerEdited;
			TS3FullClient.OnEachServerUpdated += OnEachServerUpdated;
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			TS3FullClient.OnEachChannelListFinished += OnEachChannelListFinished;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachChannelListFinished(object sender, ChannelListFinished e)
		{
			Log.Debug("OnEachChannelListFinished");
			ServerGetVariables();
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
				CheckVisible();
			}

		}

		private void buildCaches(bool clear = true)
		{
			if (clientCache == null) clientCache = new List<ClientIdT>();
			if (queryCache == null) queryCache = new List<ClientIdT>();
			if (clear) clientCache.Clear(); queryCache.Clear();
			var clients = TS3FullClient.ClientList(ClientListOptions.info).Value;
			foreach (var client in clients)
			{
				switch (client.ClientType)
				{
					case ClientType.Full:
						if (!clientCache.Contains(client.ClientId))
							clientCache.Add(client.ClientId);
						continue;
					case ClientType.Query:
						if (!queryCache.Contains(client.ClientId))
							queryCache.Add(client.ClientId);
						continue;
					default:
						break;
				}
			}
			Log.Debug("(Re)built caches > Clients: {} Queries: {}",clientCache.Count,queryCache.Count);
		}

		private void ServerGetVariables()
		{
			var Ok = TS3FullClient.Send<ResponseVoid>("servergetvariables", new List<ICommandPart>() { }).Ok;
		}
		private bool CheckSlots(int maxclients, int reserved = 0)
		{
			var realSlots = maxclients - reserved;
			if (!InitializedSlots) {
				Log.Debug("Initialized CurrentSlots: {} | realSlots: {} ({}-{}).", CurrentSlots, realSlots, maxclients, reserved);
				InitializedSlots = true;
			}
			if (realSlots != CurrentSlots)
			{
				Log.Info("Slots changed from {} to {} ({}-{}). Updating...", CurrentSlots, realSlots, maxclients, reserved);
				CurrentSlots = realSlots;
				return true;
			}
			return false;
		}
		private void CheckClients()
		{
			if (!InitializedSlots) return;
			if (!InitializedVisible) {
				if (InitializedUsers && (CurrentUsersQueries >= CurrentSlots)) {
					Log.Info("Editing Maxclients to {} because {} >= {} - 1", CurrentUsersQueries + 1, CurrentUsersQueries, CurrentSlots);
					EditMaxClients(CurrentUsersQueries + 1);
					ServerGetVariables();
				}
				return;
			}
			/*if (CurrentVisibleUsers == (CurrentSlots - 1)) { // TODO
				Log.Info("Editing Maxclients to {} because {} == {} - 1", CurrentVisibleUsers + 1, CurrentVisibleUsers, CurrentSlots);
				EditMaxClients(CurrentVisibleUsers + 1);
			} else if (CurrentSlots == (CurrentVisibleUsers + 1))
			{
				Log.Info("Editing Maxclients to {} because {} > {} + 1", CurrentVisibleUsers - 1, CurrentVisibleUsers, CurrentSlots);
				EditMaxClients(CurrentVisibleUsers - 1);
			}*/
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
			switch (client.ClientType)
			{
				case ClientType.Full:
					if (!clientCache.Contains(client.ClientId)){
						clientCache.Add(client.ClientId);
						Log.Debug("Client joined: {} ({}): Increasing clientCache to {} / {}", client.Name, client.ClientId, clientCache.Count, CurrentSlots);
					}
					break;
				case ClientType.Query:
					if (!queryCache.Contains(client.ClientId)){
						queryCache.Add(client.ClientId);
						Log.Debug("Query joined: {} ({}): Increasing queryCache to {} / {}", client.Name, client.ClientId, queryCache.Count, CurrentSlots);
					}
					break;
				default:
					break;
			}
			// CurrentVisibleUsers += 1;
			// Log.Debug("User joined: {} ({}): Increasing CurrentVisibleUsers to {} / {}", client.Name,client.ClientId,CurrentVisibleUsers,CurrentSlots);
			CheckClients();
			// Log.Debug("InitializedUsers: {} !InitializedVisible: {} clientCache.Count: {} >= {} === {}", InitializedUsers, !InitializedVisible, clientCache.Count, CurrentUsersQueries, (InitializedUsers && !InitializedVisible && (clientCache.Count >= CurrentUsersQueries)));
			if (InitializedUsers && !InitializedVisible) {
				CheckVisible();
			}
		}

		private void CheckVisible()
		{
			var all_users_visible = clientCache.Count >= CurrentUsersQueries;
			Log.Trace("{} >= {}: {}", clientCache.Count, CurrentUsersQueries, all_users_visible);
			if (all_users_visible)
			{
				Log.Debug("InitializedVisible because clientCache.Count: {} >= {}", clientCache.Count, CurrentUsersQueries);
				InitializedVisible = true;
			}
		}

		private void OnEachClientLeftView(object sender, ClientLeftView client)
		{
			if (clientCache.Contains(client.ClientId)) {
				clientCache.Remove(client.ClientId);
				Log.Debug("Client left: {}: Decreasing clientCache to {} / {}", client.ClientId, clientCache.Count, CurrentSlots);
			}
			if (queryCache.Contains(client.ClientId)) {
				queryCache.Remove(client.ClientId);
				Log.Debug("Query left: {}: Decreasing queryCache to {} / {}", client.ClientId, queryCache.Count, CurrentSlots);
			}
			// CurrentVisibleUsers -= 1;
			// Log.Debug("User left: {}: Decreasing CurrentVisibleUsers to {} / {}", client.ClientId, CurrentVisibleUsers, CurrentSlots);
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
			sb.AppendLine($"Clients: {{CurrentVisibleUsers}} ({CurrentUsersQueries}) / {CurrentSlots}");
			sb.AppendLine($"PluginEnabled: {PluginEnabled}");
			// sb.AppendLine($"CheckNext: {CheckNext}");
			sb.AppendLine($"InitializedVisible: {InitializedVisible}");
			sb.AppendLine($"clientCache ({clientCache.Count}): {string.Join(", ", clientCache)}");
			sb.AppendLine($"queryCache ({queryCache.Count}): {string.Join(", ", queryCache)}");
			sb.AppendLine($"steps: {string.Join(", ", steps)}");
			return sb.ToString();
		}

		public void Dispose()
		{
			clientCache.Clear(); queryCache.Clear(); CurrentSlots = 0; // CurrentVisibleUsers = 0;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachServerUpdated -= OnEachServerUpdated;
			TS3FullClient.OnEachServerEdited -= OnEachServerEdited;
			TS3FullClient.OnEachInitServer -= OnEachInitServer;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
