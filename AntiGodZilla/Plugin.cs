using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3AudioBot.CommandSystem.Text;

using IniParser;
using IniParser.Model;
using ClientIdT = System.UInt16;
using ClientUidT = System.String;
using System.Text;

namespace ModBlackList
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
	public class ModBlackList : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private static string PluginDir;

		private static string PluginConfigFile;
		private static FileIniDataParser ConfigParser;
		private static IniData PluginConfig;

		//private const string Description = "dolode";
		//private const string MetaData = "agodzilla"; // [AGodZilla] This program was developed by Dolo.

		//private const string KickReason = "Du kommscht hier net rein!";
		//private const string BanReason = "Botting!";
		// private static readonly TimeSpan BanTime = TimeSpan.FromMinutes(1);

		private static TimeSpan banTime;
		private static List<ClientUidT> whitelistUID;
		private static List<ulong> whitelistSGID;
		private static Dictionary<ClientIdT, ClientUidT> clientCache;

		public static string TruncateLongString(string str, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
				return str;
			return str.Substring(0, Math.Min(str.Length, maxLength));
		}

		public void Initialize()
		{
			PluginDir = Path.Combine(ConfRoot.Plugins.Path.Value, PluginInfo.ShortName); //TODO: REMOVE
			bool exists = System.IO.Directory.Exists(PluginDir);
			if (!exists) System.IO.Directory.CreateDirectory(PluginDir);
			PluginConfigFile = Path.Combine(PluginDir, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				PluginConfig["General"]["Ban Time"] = "1d";
				PluginConfig["Templates"]["Reason"] = "Disallowed modification!";
				PluginConfig["Templates"]["Poke Message"] = "%found% is not allowed on this server, uninstall it and try again!".Mod().Bold().Color(Color.Red).ToString();
				PluginConfig["Templates"]["Private Message"] = string.Empty;
				PluginConfig["Whitelist"]["UIDs"] = "e3dvocUFTE1UWIvtW8qzulnWErI=";
				PluginConfig["Whitelist"]["SGIDs"] = string.Empty;
				PluginConfig["Description"]["Test"] = "exact:exact:DESCRIPTION_TEST";
				PluginConfig["Description"]["AGodZilla"] = "contains:ignore:godzilla";
				PluginConfig["MetaData"]["Test"] = "exact:exact:METADATA_TEST";
				PluginConfig["MetaData"]["AGodZilla"] = "exact:exact:[AGodZilla] This program was developed by Dolo.";
				PluginConfig["Disconnect Message"]["1s:Test"] = "exact:exact:DISCONNECT_MESSAGE_TEST";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created!", PluginInfo.Name);
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			var banStr = PluginConfig["General"]["Ban Time"];
			whitelistUID = PluginConfig["Whitelist"]["UIDs"].Split(',').ToList();
			whitelistSGID = PluginConfig["Whitelist"]["SGIDs"].Split(',').Select(ulong.Parse).ToList();
			PluginConfig["Templates"]["Poke Message"] = PluginConfig["Templates"]["Poke Message"].Replace("\\n","\n");
			if (!string.IsNullOrEmpty(banStr)) banTime = parseTimespan(banStr);
			clientCache = new Dictionary<ClientIdT, ClientUidT>();
			/*var clientList = TS3FullClient.ClientList().Value;
			foreach (var client in clientList)
			{
				clientCache.Add(client.ClientId, client.Uid);
			}*/
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			// TS3FullClient.OnEachPluginCommand += OnEachPluginCommand;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachPluginCommand(object sender, PluginCommand e)
		{
			return;
			var strings = new List<string>() { e.Data.ToLower(), e.Name.ToLower() };
			foreach (var str in strings)
			{
				if (str.Contains("notify"))
				{
					// Kick doesn't work here
				}
			}
	{

	}
		}

		private void OnEachClientLeftView(object sender, ClientLeftView e)
		{
			try {
				// var client = TS3Client.GetCachedClientById(e.ClientId).Value;
				if (e.Reason == Reason.LeftServer) {
					var uid = clientCache[e.ClientId];
					if (!whitelistUID.Contains(uid)) {
						if (!string.IsNullOrEmpty(e.ReasonMessage)) {
							var match = CheckSection("Disconnect Message", e.ReasonMessage);
							if (match.Item1)
							{
								var splitted = match.Item2.Split(':');
								TS3FullClient.BanClient(uid, parseTimespan(splitted[0]), splitted[1]);
								Log.Info("Banned client {} for Disconnect Message \"{}\"", uid, splitted[1]);
							}
						}
					}
				}
				if (clientCache.ContainsKey(e.ClientId))
					clientCache.Remove(e.ClientId);
			} catch	(Exception ex) { Log.Error(ex.StackTrace); }
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			try
			{
				if (!clientCache.ContainsKey(client.ClientId))
					clientCache.Add(client.ClientId, client.Uid);
				CheckClient(client.ClientId);
			} catch (Exception ex) { Log.Error(ex.StackTrace); }
		}


		private void CheckClient(ClientIdT clientId)
		{
			if (clientId == TS3FullClient.ClientId) return;
			var client = TS3Client.GetClientInfoById(clientId).Value;
			if (client.ClientType == ClientType.Query) return;
			if (whitelistUID.Contains(client.Uid)) return;
			// bool hasMatch = parameters.Select(x => x.source).Intersect(myStrings).Any();
			if (whitelistSGID.Intersect(client.ServerGroups).Any()) return;
			// Check Version

			if (!string.IsNullOrEmpty(client.Description))
			{
				var match = CheckSection("Description", client.Description);
				if (match.Item1) { TakeAction(clientId, match.Item2); return; }
			}
			if (!string.IsNullOrEmpty(client.Metadata))
			{
				var match = CheckSection("MetaData", client.Metadata);
				if (match.Item1) { TakeAction(clientId, match.Item2); return; }
			}
		}

		private Tuple<bool, string> CheckSection(string section, string input)
		{
			foreach (var item in PluginConfig[section])
			{
				var parsed = ParseMatch(item.Value);
				var found = FindMatch(parsed.Item1, parsed.Item2, parsed.Item3, input);
				if (found) return new Tuple<bool, string>(found, item.KeyName);
			}
			return new Tuple<bool, string>(false, string.Empty);
		}
		#region Bullshit
		private bool FindMatch(MatchType matchType, MatchCase matchCase, string pattern, string input)
		{
			if (matchCase == MatchCase.Ignore)
			{
				pattern = pattern.ToLower();
				input = input.ToLower();
			}
			switch (matchType)
			{
				case MatchType.Exact:
					if (input == pattern) return true;
					break;
				case MatchType.Contains:
					if (input.Contains(pattern)) return true;
					break;
				case MatchType.StartsWith:
					if (input.StartsWith(pattern)) return true;
					break;
				case MatchType.EndsWith:
					if (input.EndsWith(pattern)) return true;
					break;
				case MatchType.Regex:
					throw new Exception("Regex is not yet supported, sorry!");
				default:
					break;
			}
			return false;
		}

		private Tuple<MatchType, MatchCase, string> ParseMatch(string unparsed) {
			var splitted = unparsed.Split(':');
			var matchType = MatchType.Unknown;
			switch (splitted[0].ToLower())
			{
				case "exact":
					matchType = MatchType.Exact; break;
				case "contains":
					matchType = MatchType.Contains; break;
				case "startswith":
					matchType = MatchType.StartsWith; break;
				case "endswith":
					matchType = MatchType.EndsWith; break;
				case "regex":
					matchType = MatchType.Regex; break;
				default:
					break;
			}
			var matchCase = MatchCase.Unknown;
			switch (splitted[1].ToLower())
			{
				case "exact":
					matchCase = MatchCase.Exact; break;
				case "ignore":
					matchCase = MatchCase.Ignore; break;
				default:
					break;
			}
			return new Tuple<MatchType, MatchCase, ClientUidT>(matchType, matchCase, splitted[2]);
		}

		private enum MatchType
		{
			Exact,
			Contains,
			StartsWith,
			EndsWith,
			Regex,
			Unknown
		}
		private enum MatchCase
		{
			Exact,
			Ignore,
			Unknown
		}

		public static TimeSpan parseTimespan(string input)
		{
			input = input.ToLower();
			var perms = new List<string>() { "p", "perm", "permanent" };
			if (perms.Contains(input)) return TimeSpan.MinValue;
			long timeMultiplication = 0;
			switch (input[input.Length - 1])
			{
				case 's':
					timeMultiplication = 1;
					break;
				case 'm':
					timeMultiplication = 60;
					break;
				case 'h':
					timeMultiplication = 3600;
					break;
				case 'd':
					timeMultiplication = 86400;
					break;
				case 'w':
					timeMultiplication = 604800;
					break;
				case 'M':
					timeMultiplication = 2592000;
					break;
				case 'y':
					timeMultiplication = 31104000;
					break;
			}
			long totalSeconds = long.Parse(input.Remove(input.Length - 1)) * timeMultiplication;

			return TimeSpan.FromSeconds(totalSeconds);
		}
		#endregion
		private void TakeAction(ClientIdT clientId, string found) {
			var msg = PluginConfig["Templates"]["Private Message"];
			if (!string.IsNullOrWhiteSpace(msg))
			{
				msg = msg.Replace("%found%", found);
				TS3Client.SendMessage(msg, clientId);
			}
			msg = PluginConfig["Templates"]["Poke Message"];
			if (!string.IsNullOrWhiteSpace(msg))
			{
				msg = msg.Replace("%found%", found);
				TS3FullClient.PokeClient(clientId, TruncateLongString(msg, 100));
			}
			var reason = PluginConfig["Templates"]["Reason"].Replace("%found%", found);
			if (banTime != null)
			{
				TS3FullClient.KickClientFromServer(clientId, reason);
				Log.Info("Kicked client {} for {} ({})", clientId, reason, found);
			} else
			{
				TS3FullClient.BanClient(clientId, banTime, reason);
				Log.Info("Kicked client {} for {} ({})", clientId, reason, found);
			}
			
		}
		private void CheckAllClients()
		{
			var clients = TS3FullClient.ClientList().Value;
			foreach (var client in clients)
			{
				CheckClient(client.ClientId);
			}
		}
		
		[Command("mbl checkall", "")]
		public string CommandCheckAllClients()
		{
			CheckAllClients();
			return "Checked all clients";
		}
		public void Dispose()
		{
			// TS3FullClient.OnEachPluginCommand -= OnEachPluginCommand;
			TS3FullClient.OnEachClientLeftView -= OnEachClientLeftView;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
