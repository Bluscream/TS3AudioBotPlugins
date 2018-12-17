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
using IniParser;
using IniParser.Model;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using System.Text;

namespace MaintenanceMode
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
	public class MaintenanceMode : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		public static IniData PluginConfig;
		private List<string> whitelist = new List<string>();
		private bool MaintenanceEnabled = false;

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				PluginConfig["general"]["active"] = "0";
				PluginConfig["general"]["invoker"] = string.Empty;
				PluginConfig["general"]["start"] = string.Empty;
				PluginConfig["general"]["kick reason"] = "Maintenance Mode, please try again later.";
				PluginConfig["general"]["poke message"] = string.Empty;
				PluginConfig["general"]["private message"] = string.Empty;
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created!", PluginInfo.Name);
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			var whitelistFile = Path.Combine(ConfRoot.Plugins.Path.Value, "whitelist.txt");
			whitelist = (File.ReadAllLines(whitelistFile)).Select(l => l.Trim()).ToList();
			MaintenanceEnabled = PluginConfig["general"]["active"].Trim() == "1";
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnChannelListFinished += OnChannelListFinished;
			if (MaintenanceEnabled)
				CheckAllClients();
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e)
		{
			if (PluginConfig["general"]["active"].Trim() == "1")
				CheckAllClients();
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			if (!MaintenanceEnabled) return;
			if (whitelist.Contains(client.Uid)) return;
			if (client.ClientType == ClientType.Query) return;
			if (client.ClientId == TS3FullClient.ClientId) return;
			KickFromServer(client.ClientId);
		}

		private bool KickFromServer(ClientIdT ClientId)
		{
			var msg = PluginConfig["general"]["private message"];
			if (!string.IsNullOrWhiteSpace(msg)) {
				TS3Client.SendMessage(msg, ClientId);
			}
			msg = PluginConfig["general"]["poke message"];
			if (!string.IsNullOrWhiteSpace(msg))
			{
				var cmd = new Ts3Command("clientpoke", new List<ICommandPart>() {
					new CommandParameter("clid", ClientId),
					new CommandParameter("msg", msg.Replace("{start}", PluginConfig["general"]["start"]).Replace("{invoker}", PluginConfig["general"]["invoker"]))
				});
				var result = TS3FullClient.SendNotifyCommand(cmd, NotificationType.ClientPokeRequest).Value;
			}
			var command = new Ts3Command("clientkick", new List<ICommandPart>() {
					new CommandParameter("reasonid", (int)ReasonIdentifier.Server),
					new CommandParameter("clid", ClientId),
					new CommandParameter("reasonmsg", PluginConfig["general"]["kick reason"])
			});
			Log.Warn(command);
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}

		private int CheckAllClients()
		{
			var clients = TS3FullClient.ClientList(ClientListOptions.uid).Value;
			var kicked = 0;
			foreach (var client in clients)
			{
				if (client.ClientId == TS3FullClient.ClientId) continue;
				if (client.ClientType != ClientType.Full) continue;
				if (whitelist.Contains(client.Uid)) continue;
				Log.Info("\"{}\" not in \"{}\"", client.Uid, string.Join("\", \"", whitelist));
				KickFromServer(client.ClientId);
				kicked += 1;
			}
			return kicked;
		}

		[Command("wartung", "")]
		public string CommandMaintenanceMode()
		{
			var onoff = MaintenanceEnabled ? "[color=orange]on" : "[color=green]off";
			return $"Maintenance is turned [b]{onoff}[/color]\n\n{string.Join("\n", whitelist)}";
		}
		[Command("wartung an", "")]
		public string CommandEnableMaintenanceMode(InvokerData invoker)
		{
			PluginConfig["general"]["invoker"] = invoker.NickName;
			PluginConfig["general"]["start"] = DateTime.Now.ToString();
			PluginConfig["general"]["active"] = "1";
			MaintenanceEnabled = true;
			var kicked = CheckAllClients();
			return $"[color=orange][b]Enabled Maintenance Mode, {kicked} clients were kicked!";
		}
		[Command("wartung aus", "")]
		public string CommandDisableMaintenanceMode()
		{
			MaintenanceEnabled = false;
			PluginConfig["general"]["active"] = "0";
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			return "[color=green][b]Disabled Maintenance Mode";
		}
		[Command("wartung whitelist", "")]
		public string CommandToggleWhitelist(string uid)
		{
			uid = uid.Trim();
			if (whitelist.Contains(uid))
			{
				whitelist.Remove(uid);
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				return $"Removed [b]{uid}[/b] from Maintenance whitelist";
			} else
			{
				whitelist.Add(uid);
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				return $"Added [b]{uid}[/b] to Maintenance whitelist";
			}
			
		}

		public void Dispose()
		{
			TS3FullClient.OnChannelListFinished -= OnChannelListFinished;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
