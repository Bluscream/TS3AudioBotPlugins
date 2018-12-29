using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using TS3AudioBot.Helper;
using TS3AudioBot.Sessions;

namespace CountryWhitelist
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
	public class CountryWhitelist : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private static string PluginConfigFile;
		private static FileIniDataParser ConfigParser;
		private static IniData PluginConfig;
		private List<string> whitelist = new List<string>();
		private bool WhitelistEnabled = false;
		private ulong DefaultServerGroupId = 0;

		public static string TruncateLongString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return str.Substring(0, Math.Min(str.Length, maxLength));
		}

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				PluginConfig["General"]["Whitelist"] = "DE,CH,AT,LU";
				PluginConfig["Session"]["Active"] = "0";
				PluginConfig["Session"]["Invoker"] = string.Empty;
				PluginConfig["Session"]["Start"] = string.Empty;
				PluginConfig["Templates"]["Kick Reason"] = "Country not whitelisted";
				PluginConfig["Templates"]["Poke Message"] = "[color=red]Country Whitelist is active since {start}!\\n\\nOnly {whitelist} are allowed to connect now.";
				PluginConfig["Templates"]["Private Message"] = string.Empty;
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created!", PluginInfo.Name);
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			whitelist = PluginConfig["General"]["Whitelist"].Split(',').ToList();
			WhitelistEnabled = PluginConfig["Session"]["Active"].Trim() == "1";
			TS3FullClient.OnEachInitServer += OnEachInitServer;
			TS3FullClient.OnEachServerEdited += OnEachServerEdited;
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachServerEdited(object sender, ServerEdited server)
		{
			DefaultServerGroupId = server.DefaultServerGroup;
		}

		private void OnEachInitServer(object sender, InitServer server)
		{
			DefaultServerGroupId = server.DefaultServerGroup;
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			if (!WhitelistEnabled) return;
			if (whitelist.Contains(client.Uid)) return;
			if (client.ClientType == ClientType.Query) return;
			if (client.ClientId == TS3FullClient.ClientId) return;
			if (DefaultServerGroupId > 0) {
				// var default_list = new List<ulong>() { DefaultServerGroupId };
				// var isDefault = client.ServerGroups.All(default_list.Contains) && client.ServerGroups.Count == default_list.Count;
				if (client.ServerGroups.Contains(DefaultServerGroupId)) return;
			}
			if (whitelist.Contains(client.CountryCode)) return;
			KickFromServer(client.ClientId);
		}

		private bool KickFromServer(ClientIdT ClientId)
		{
			var msg = PluginConfig["Templates"]["Private Message"];
			if (!string.IsNullOrWhiteSpace(msg))
			{
				msg = msg.Replace("{start}", PluginConfig["Session"]["Start"]).Replace("{invoker}", PluginConfig["Session"]["Invoker"]).Replace("{whitelist}", string.Join(", ", whitelist));
				TS3Client.SendMessage(msg, ClientId);
			}
			msg = PluginConfig["Templates"]["Poke Message"];
			if (!string.IsNullOrWhiteSpace(msg))
			{
				msg = msg.Replace("{start}", PluginConfig["Session"]["Start"]).Replace("{invoker}", PluginConfig["Session"]["Invoker"]).Replace("{whitelist}", string.Join(", ", whitelist));
				var cmd = new Ts3Command("clientpoke", new List<ICommandPart>() {
					new CommandParameter("clid", ClientId),
					new CommandParameter("msg", TruncateLongString(msg, 100))
				});
				var result = TS3FullClient.SendNotifyCommand(cmd, NotificationType.ClientPokeRequest).Value;
			}
			var command = new Ts3Command("clientkick", new List<ICommandPart>() {
					new CommandParameter("reasonid", (int)ReasonIdentifier.Server),
					new CommandParameter("clid", ClientId),
					new CommandParameter("reasonmsg", TruncateLongString(PluginConfig["Session"]["Kick Reason"], 80))
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientLeftView);
			return Result.Ok;
		}

		[Command("countrywhitelist", "")]
		public string CommandCountryWhitelistMode()
		{
			var onoff = WhitelistEnabled ? "[color=orange]on" : "[color=green]off";
			return $"Country whitelist is turned [b]{onoff}[/color]\n\n{string.Join("\n", whitelist)}";
		}
		[Command("countrywhitelist on", "")]
		public string CommandEnableCountryWhitelistMode(InvokerData invoker, UserSession session = null)
		{
			string ResponseQuit(string message)
			{
				if (TextUtil.GetAnswer(message) == Answer.Yes)
				{
					PluginConfig["Session"]["Invoker"] = invoker.NickName;
					PluginConfig["Session"]["Start"] = DateTime.Now.ToString();
					PluginConfig["Session"]["Active"] = "1";
					WhitelistEnabled = true;
					ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
					Log.Info("Country Whitelist was enabled by \"{}\" ({})", invoker.NickName, invoker.ClientUid);
					return "[color=orange][b]Enabled Country Whitelist!";
				}
				return null;
			}
			session.SetResponse(ResponseQuit);
			return "You sure about that? (!yes | !no)";
		}
		[Command("countrywhitelist off", "")]
		public string CommandDisableCountryWhitelistMode(InvokerData invoker)
		{
			WhitelistEnabled = false;
			PluginConfig["Session"]["Active"] = "0";
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			Log.Info("Country Whitelist was disabled by \"{}\" ({})", invoker.NickName, invoker.ClientUid);
			return "[color=green][b]Disabled Country Whitelist";
		}
		[Command("wartung whitelist", "")]
		public string CommandToggleWhitelist(string uid)
		{
			uid = uid.Trim();
			if (whitelist.Contains(uid))
			{
				whitelist.Remove(uid);
				PluginConfig["General"]["Whitelist"] = string.Join(",", whitelist);
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				return $"Removed [b]{uid}[/b] from Country whitelist";
			}
			else
			{
				whitelist.Add(uid);
				PluginConfig["General"]["Whitelist"] = string.Join(",", whitelist);
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				return $"Added [b]{uid}[/b] to Country whitelist";
			}

		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachServerEdited -= OnEachServerEdited;
			TS3FullClient.OnEachInitServer -= OnEachInitServer;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
