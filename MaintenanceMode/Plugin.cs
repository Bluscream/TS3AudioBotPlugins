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
		public ConfRoot ConfRoot { get; set; }
		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		public static IniData PluginConfig;
		public List<string> whitelist;
		public bool MaintenanceEnabled = false;

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				PluginConfig["general"]["kick reason"] = "Wartungsmodus aktiv, bitte versuche es später nochmal.";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created!", PluginInfo.Name);
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			var whitelistFile = Path.Combine(ConfRoot.Plugins.Path.Value, "whitelist.txt");
			whitelist = File.ReadAllLines(whitelistFile).ToList();
			TS3FullClient.OnClientEnterView += OnClientEnterView;
			//TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnClientEnterView(object sender, IEnumerable<ClientEnterView> e)
		{
			if (!MaintenanceEnabled) return;
			var toKick = new List<ClientIdT>();
			foreach (var client in e)
			{
				if (whitelist.Contains(client.Uid)) continue;
				toKick.Add(client.ClientId);
			}
			KickFromServer(toKick.ToArray(), PluginConfig["general"]["kick reason"]);
		}

		/*private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			if (!MaintenanceEnabled) return;
			if (whitelist.Contains(e.Uid)) return;
			KickFromServer
		}*/



		private bool KickFromServer(ClientIdT[] clientIds, string reason)
		{
			var command = new Ts3Command("clientkick", new List<ICommandPart>() {
					new CommandParameter("reasonid", (int)ReasonIdentifier.Server),
					new CommandMultiParameter("clid", clientIds),
					new CommandParameter("reasonmsg", reason)
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}
		// private bool KickFromServer(ClientIdT ClientId, string reason) => KickFromServer(new ClientIdT[] { ClientId }, reason);

		[Command("wartung", "")]
		public string CommandSetMaintenanceMode(string toggle="")
		{
			if (string.IsNullOrWhiteSpace(toggle)) {
				var onoff = MaintenanceEnabled ? "[color=orange]on" : "[color=green]off";
				return $"Maintenance is turned [b]{onoff}[/color]\n\n{string.Join("\n", whitelist)}";
			}
			var str = "";
			toggle = toggle.ToLower();
			if (toggle == "on") {
				MaintenanceEnabled = true;
				var clients = TS3FullClient.ClientList().Value;
				var toKick = new List<ClientIdT>();
				foreach (var client in clients)
				{
					if (whitelist.Contains(client.Uid)) continue;
					toKick.Add(client.ClientId);
				}
				KickFromServer(toKick.ToArray(), PluginConfig["general"]["kick reason"]);
				str = $"[color=orange][b]Enabled Maintenance Mode, {toKick.Count} clients were kicked!";
			} else if (toggle == "off") {
				MaintenanceEnabled = false;
				str = "[color=green][b]Disabled Maintenance Mode";
			} else { str = "[color=red][b]Use \"off\" or \"on\""; }
			return str;
		}

		public void Dispose()
		{
			// TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView; ;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
