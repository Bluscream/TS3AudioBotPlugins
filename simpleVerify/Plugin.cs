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
using IniParser;
using IniParser.Model;
using TS3Client.Commands;

using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ClientUidT = System.String;
using TS3Client.Messages;
using System.Text;
using TS3AudioBot.Sessions;

namespace SimpleVerify
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

	public class SimpleVerify : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		private static IniData PluginConfig;

		private static string OnHoldFile;
		private static IniData OnHold;

		private bool VerificationEnabled = true;
		private ulong DefaultServerGroupId = 0;

		public static string TruncateLongString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return str.Substring(0, Math.Min(str.Length, maxLength));
		}
		public static string ClientURL(ushort clientID, string uid = "unknown", string nickname = "Unknown User")
		{
			var sb = new StringBuilder("[URL=client://");
			sb.Append(clientID);
			sb.Append("/");
			sb.Append(uid);
			//sb.Append("~");
			sb.Append("]\"");
			sb.Append(nickname);
			sb.Append("\"[/URL]");
			return sb.ToString();
		}

		/*public static string FormatMessage(string message, int maxCharacters = -1)
		{
			return message.Replace("{clientname}", PluginConfig["Session"]["Start"]).Replace("{invoker}", PluginConfig["Session"]["Invoker"]
		}*/

		public void Initialize()
		{
			LoadOnHold(); LoadConfig();
			DefaultServerGroupId = ulong.Parse(PluginConfig["Groups"]["Unverified"]);
			TS3FullClient.OnEachInitServer += OnEachInitServer;
			TS3FullClient.OnEachServerEdited += OnEachServerEdited;
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientServerGroupRemoved += OnEachClientServerGroupRemoved;
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
			StartVerification(client.ClientType, client.ClientId, client.ServerGroups, client.Uid, client.Name);
		}

		private void StartVerification(ClientType clientType, ClientIdT clientId, ulong[] serverGroups, string uid, string name /*, ChannelIdT unverifiedChannel = null*/)
		{
			if (!VerificationEnabled) return;
			if (clientType == ClientType.Query) return;
			if (clientId == TS3FullClient.ClientId) return;
			if (!serverGroups.Contains(DefaultServerGroupId)) return;
			var cid = ulong.Parse(PluginConfig["Channels"]["Unverified"]);
			if (cid > 0) TS3FullClient.ClientMove(clientId, cid);
			//var session = new UserSession();
			var msg = PluginConfig["Templates"]["Poke Message"];
			if (!string.IsNullOrWhiteSpace(msg))
			{
				msg = msg.Replace("%clientname%", name).Replace("\\n", "\n");
				PokeClient(clientId, msg);
			}
			msg = PluginConfig["Templates"]["Chat Message"];
			if (!string.IsNullOrWhiteSpace(msg))
			{
				msg = msg.Replace("%client%", ClientURL(clientId, uid, name)).Replace("\\n", "\n");
				TS3Client.SendMessage(msg, clientId);
			}
		}

		private void OnEachClientServerGroupRemoved(object sender, ClientServerGroupRemoved e)
		{
			Log.Debug($"OnEachClientServerGroupRemoved: ClientId: {e.ClientId} ClientUid: {e.ClientUid} Name: {e.Name} ServerGroupId: {e.ServerGroupId} NotifyType: {e.NotifyType}");
			if (!VerificationEnabled) return;
			if (e.ClientId == TS3FullClient.ClientId) return;
			var client = TS3Client.GetClientInfoById(e.ClientId).Value;
			if (client.ClientType == ClientType.Query) return;
			var verified_group = ulong.Parse(PluginConfig["Groups"]["Verified"]);
			if (e.ServerGroupId != verified_group) return;
			if (!client.ServerGroups.SequenceEqual(new ulong[] { verified_group })) {
				var uid_str = e.ClientUid.Replace("=", string.Empty);
				OnHold["Clients"][uid_str] = string.Join(",", client.ServerGroups);
				foreach (var sgid in client.ServerGroups)
				{
					if (sgid == DefaultServerGroupId) continue;
					TS3FullClient.ServerGroupDelClient(sgid, client.DatabaseId);
				}
				ConfigParser.WriteFile(OnHoldFile, OnHold);
			}
			var cid = ulong.Parse(PluginConfig["Channels"]["Unverified"]);
			if (cid < 1) TS3Client.KickClientFromChannel(e.ClientId);
			StartVerification(client.ClientType, e.ClientId, client.ServerGroups, e.ClientUid, client.Name);
		}

		private bool PokeClient(ClientIdT clientId, string message)
		{
			return TS3FullClient.Send<ResponseVoid>("clientpoke", new List<ICommandPart>() {
				new CommandParameter("clid", clientId),
				new CommandParameter("msg", TruncateLongString(message, 100))
			}).Ok;
		}
		private bool KickFromServer(ClientIdT clientId)
		{

			return TS3FullClient.Send<ResponseVoid>("clientkick", new List<ICommandPart>() {
					new CommandParameter("reasonid", (int)ReasonIdentifier.Server),
					new CommandParameter("clid", clientId),
					new CommandParameter("reasonmsg", TruncateLongString(PluginConfig["Templates"]["Kick Reason"], 80))
			}).Ok;
		}

		[Command("accept", "")]
		public string CommandAcceptToS(InvokerData invoker)
		{
			if (!VerificationEnabled) return PluginConfig["Templates"]["Verification Disabled"];
			var uid_str = invoker.ClientUid.Replace("=", string.Empty);
			if (OnHold["Clients"].ContainsKey(uid_str))
			{
				var groups = Array.ConvertAll(OnHold["Clients"][uid_str].Split(','), ulong.Parse).ToList();
				foreach (var group in groups)
				{
					TS3FullClient.ServerGroupAddClient(group, (ulong)invoker.DatabaseId);
				}
				OnHold["Clients"].RemoveKey(uid_str);
				ConfigParser.WriteFile(OnHoldFile, OnHold);
			}
			TS3FullClient.ServerGroupAddClient(ulong.Parse(PluginConfig["Groups"]["Verified"]), (ulong)invoker.DatabaseId);
			var cid = ulong.Parse(PluginConfig["Channels"]["Verified"]);
			if (cid > 0) TS3FullClient.ClientMove((ushort)invoker.ClientId, cid);
			return PluginConfig["Templates"]["Verified Response"];
		}


		[Command("deny", "")]
		public void CommandDenyToS(InvokerData invoker)
		{
			if (!VerificationEnabled) return;
			KickFromServer((ClientIdT)invoker.ClientId);
		}


		[Command("verification toggle", "")]
		public string CommandToggleVerification()
		{
			VerificationEnabled = !VerificationEnabled;
			var enabled = VerificationEnabled ? "[color=green]Enabled" : "[color=orange]Disabled";
			return $"{enabled} Verification!\n\nDefaultServerGroupId: {DefaultServerGroupId}";
		}

		private void LoadConfig()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			if (ConfigParser == null) ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				var section = "Templates";
				PluginConfig[section]["Poke Message"] = @"Hello %clientname%\n\nPlease check your privat chat on how to get verified on this server!";
				PluginConfig[section]["Chat Message"] = @"Welcome %client%,\n\nBefore you can use this TeamSpeak Server you have to agree to our Terms of service\n\nYou can find them at [url]https://termsfeed.com/assets/pdf/privacy-policy-template.pdf[/url]\n\nAfter reading them you have to choose [b]!accept[/b] or [b]!deny[/b]";
				PluginConfig[section]["Verified Response"] = @"[color=green]Now you can use this TeamSpeak 3 server. Have fun :)";
				PluginConfig[section]["Kick Reason"] = "ToS not accepted!";
				PluginConfig[section]["Verification Disabled"] = "Verification is currently disabled!";
				section = "Groups";
				PluginConfig[section]["Unverified"] = "0";
				PluginConfig[section]["Verified"] = "0";
				section = "Channels";
				PluginConfig[section]["Unverified"] = "0";
				PluginConfig[section]["Verified"] = "0";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
		}

		private void LoadOnHold()
		{
			OnHoldFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}_waiting.ini");
			if (ConfigParser == null) ConfigParser = new FileIniDataParser();
			if (!File.Exists(OnHoldFile))
			{
				OnHold = new IniData();
				OnHold.Sections.Add(new SectionData("Clients"));
				ConfigParser.WriteFile(OnHoldFile, OnHold);
			}
			else { OnHold = ConfigParser.ReadFile(OnHoldFile); }
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientServerGroupRemoved -= OnEachClientServerGroupRemoved;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachServerEdited -= OnEachServerEdited;
			TS3FullClient.OnEachInitServer -= OnEachInitServer;
			if (ConfigParser == null) ConfigParser = new FileIniDataParser();
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
