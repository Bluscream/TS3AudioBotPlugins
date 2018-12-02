using System;
using System.Collections.Generic;
using System.Linq;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client.Messages;
using TS3AudioBot.Helper;
using TS3Client.Full;
using TS3Client.Commands;
using IniParser;
using IniParser.Model;
using ClientUidT = System.String;
using ClientIdT = System.UInt16;
using ServerGroupIdT = System.UInt64;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using TS3AudioBot.CommandSystem;

namespace ISPValidator
{
	public class Utils
	{
	}
	public static class PluginInfo
	{
		public static readonly string ShortName = "ISPV";
		public static readonly string Name;
		public static readonly string Description = "This script will autokick everyone not using a whitelisted ISP.";
		public static readonly string Url;
		public static readonly string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;
		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}

	public class Client {
		public ClientIdT Id;
		public string NickName;
		public ClientUidT Uid;
		public IPAddress Ip;
		public string Isp;
		public string CountryCode;
	}

	public class ISPValidator : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		private Bot bot;
		private Ts3FullClient lib;
		public TickWorker ClearCache;
		private static IniData cfg;
		private static string cfgfile;
		private static string ispfile;
		public bool Enabled { get; private set; }
		public List<string> isps = new List<string>();
		public List<string> allowed = new List<string>();
		public List<string> blocked = new List<string>();
		public List<string> whitelistUID = new List<string>();
		public List<ServerGroupIdT> whitelistSGID = new List<ServerGroupIdT>();

		public void Initialize() {
			//var pluginPath = mainBot.ConfigManager.GetDataStruct<PluginManagerData>("PluginManager", true).PluginPath;
			var pluginPath = "Plugins";
			cfgfile = Path.Combine(pluginPath, $"{PluginInfo.Name}.cfg");
			try {
				if (File.Exists(cfgfile)) {
					var parser = new FileIniDataParser();
					cfg = parser.ReadFile(cfgfile);
					Log.Debug($"cfgfile = {cfgfile}");
					if (cfg["Ignore"]["uids"].Contains(',')) {
						whitelistUID = cfg["Ignore"]["uids"].Split(',').ToList();
					} else {
						whitelistUID.Add(cfg["Ignore"]["uids"]);
					}
					if (cfg["Ignore"]["sgids"].Contains(',')) {
						var _whitelistSGID = cfg["Ignore"]["sgids"].Split(',');
						foreach (var wsgid in _whitelistSGID) {
							whitelistSGID.Add(ServerGroupIdT.Parse(wsgid));
						}
					} else {
						whitelistSGID.Add(ServerGroupIdT.Parse(cfg["Ignore"]["sgids"]));
					}
				}
			} catch (Exception ex) {
				throw new Exception($"{PluginInfo.Name} Can't load \"{cfgfile}\"! Error:\n{ex}");
				// cfg = new IniData();
				//while (!Setup()) { }
			}
			ispfile = Path.Combine(pluginPath, "ISPs.txt");
			Log.Debug($"ispfile = {ispfile}");
			if (File.Exists(ispfile))
				isps = File.ReadAllLines(ispfile).ToList();
			lib.OnClientEnterView += Lib_OnClientEnterView;
			lib.OnConnected += Lib_OnConnected;
			ClearCache = TickPool.RegisterTick(Tick, TimeSpan.FromMinutes(UInt64.Parse(cfg["General"]["clearcache"])), false);
			Enabled = true;
			Log.Debug("Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		public void Tick() {
			allowed.Clear();
			blocked.Clear();
		}

		public void Dispose() {
			ClearCache.Active = false;
			//TickPool.UnregisterTicker(ClearCache);
			lib.OnConnected -= Lib_OnConnected;
			lib.OnClientEnterView -= Lib_OnClientEnterView;
			/*
			var parser = new FileIniDataParser();
			parser.WriteFile(cfgfile, cfg);
			*/
			Log.Debug($"Saved Settings to \"{cfgfile}\".");
			Log.Debug("Plugin " + PluginInfo.Name + " unloaded.");
		}

		#region Events

		private void Lib_OnConnected(object sender, EventArgs e) {
			if (!Enabled) return;
			if (cfg["General"]["clearcache"] != "0")
				ClearCache.Active = true;
			if (cfg["Events"]["bot_connnected"] != "true") return;
			Log.Debug("Our client is now connected, setting channel commander :)");
			if (cfg["Events"]["bot_connnected"] == "true") {
				foreach (var client in lib.ClientList().Value) {
					checkClient(client.ClientId);
				}
			}
		}

		private void Lib_OnClientEnterView(object sender, IEnumerable<ClientEnterView> e) {
			if (!Enabled) { return; }
			if (cfg["Events"]["client_joined_server"] != "true") return;
			foreach (var client in e) {
				if (client.ClientId != lib.ClientId)
					checkClient(client.ClientId);
			}
		}

		#endregion

		#region Functions

		public static bool IsLocal(string ip) {
			try {
				IPAddress[] hostIPs = Dns.GetHostAddresses(ip);
				//IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
				foreach (IPAddress hostIP in hostIPs) {
					if (IPAddress.IsLoopback(hostIP)) return true;
					/*foreach (IPAddress localIP in localIPs) {
						if (hostIP.Equals(localIP)) return "is in localips";
					}*/
				}
			} catch { }
			return false;
		}

		public string parseMSG(string msg, Client client) {
			var _msg = msg.ToLower();
			if (_msg.Contains("{ip}"))
				msg = msg.Replace("{ip}", client.Ip.ToString());
			if (_msg.Contains("{isp}"))
				msg = msg.Replace("{isp}", client.Isp);
			if (_msg.Contains("{nick}"))
				msg = msg.Replace("{nick}", client.NickName);
			if (_msg.Contains("{clid}"))
				msg = msg.Replace("{clid}", client.Id.ToString());
			if (_msg.Contains("{uid}"))
				msg = msg.Replace("{uid}", client.Uid);
			if (_msg.Contains("{country}"))
				msg = msg.Replace("{country}", client.CountryCode);
			return msg;
		}

		public void Poke(Client client, string msg) {
			var msgs = Regex.Split(msg, @"(.{1,100})(?:\s|$)|(.{100})").Where(x => x.Length > 0);
			foreach (var _msg in msgs) {
				lib.Send("clientpoke", new CommandParameter("clid", client.Id), new CommandParameter("msg", _msg));
			}
		}

		public void Message(Client client, string msg) {
			var msgs = Regex.Split(msg, @"(.{1,1024})(?:\s|$)|(.{1024})").Where(x => x.Length > 0);
			foreach (var _msg in msgs) {
				bot.QueryConnection.SendMessage(_msg, client.Id);
			}
		}

		public void takeAction(Client client, string section, bool alreadyBlocked = false) {
			if (!alreadyBlocked) {
				if (!String.IsNullOrWhiteSpace(cfg[section]["msg"])) {
					try {
						Message(client, parseMSG(cfg[section]["msg"], client));
					} catch (Exception ex) {
						Log.Warn($"Could not message {client.NickName}:\n{ex.Message}");
					}
				}
				if (!String.IsNullOrWhiteSpace(cfg[section]["poke"])) {
					try {
						Poke(client, parseMSG(cfg[section]["poke"], client));
					} catch (Exception ex) {
						Log.Warn($"Could not poke {client.NickName}:\n{ex.Message}");
					}
				}
			}
			if (cfg[section]["kickonly"] == "true") {
				//bot.QueryConnection.KickClientFromServer(client.Id, (parseMSG(cfg[section]["reason"]);
				try { // clientkick reasonid=5 reasonmsg=test clid=1
					var cmd = new Ts3Command("clientkick", new List<ICommandPart>() {
						new CommandParameter("clid", client.Id),
						new CommandParameter("reasonid", 5)
						});
					if (!alreadyBlocked && parseMSG(cfg[section]["reason"], client).Length < 100)
						cmd.AppendParameter(new CommandParameter("reasonmsg", parseMSG(cfg[section]["reason"], client)));
					lib.SendCommand<ResponseVoid>(cmd);
				} catch (Exception ex) {
					Log.Warn($"Could not kick {client.NickName}:\n{ex.Message}");
				}
			} else {
				try { // banclient clid=1 time=0 banreason=text
					var cmd = new Ts3Command("banclient", new List<ICommandPart>() {
						new CommandParameter("clid", client.Id),
						new CommandParameter("time", parseMSG(cfg[section]["bantime"], client))
						});
					if (!alreadyBlocked && parseMSG(cfg[section]["reason"], client).Length < 100)
						cmd.AppendParameter(new CommandParameter("banreason", parseMSG(cfg[section]["reason"], client)));
					lib.SendCommand<ResponseVoid>(cmd);
				} catch (Exception ex) {
					Log.Warn($"Could not ban {client.NickName}:\n{ex.Message}");
				}
			}
		}

		public bool checkISP(Client client) {
			if (cfg["General"]["whitelist"] == "true") {
				if (isps.Contains(client.Isp)) {
					allowed.Add(client.Ip.ToString());
					return true;
				} else {
					takeAction(client, "Actions");
					blocked.Add(client.Ip.ToString());
					return false;
				}
			} else {
				if (!isps.Contains(client.Isp)) {
					allowed.Add(client.Ip.ToString());
					return true;
				} else {
					takeAction(client, "Actions");
					blocked.Add(client.Ip.ToString());
					return false;
				}
			}
		}

		public string learn(string isp) {
			if (!isps.Contains(isp)) {
				isps.Add(isp);
				File.AppendAllLines(ispfile, new[] { isp });
				return $"learned {isp}";
			}
			return "already learned.";
		}

		public string checkClient(ClientIdT clid, string ip = "") {
			var _client = lib.ClientInfo(clid).Value;
			if (clid == lib.ClientId || _client.ClientType == TS3Client.ClientType.Query || whitelistUID.Contains(_client.Uid) || whitelistSGID.Intersect(_client.ServerGroups).Any()) {
				return "self or query or whitelisted";
			}
			var client = new Client();
			if (String.IsNullOrWhiteSpace(ip))
				ip = _client.Ip;
			ip = ip.Replace("[", "").Replace("]", "");
			var isIP = IPAddress.TryParse(_client.Ip, out client.Ip);
			if (!isIP) { Log.Error($"Unable to get ip of \"{_client.Name}\" #{clid} (dbid:{_client.DatabaseId}). Make sure the bot has the b_client_remoteaddress_view permission!"); return "unresolvable"; }
			if (allowed.Contains(ip) || IsLocal(ip))
				return "already allowed or local";
			client.Id = clid;
			client.NickName = _client.Name;
			client.Uid = _client.Uid;
			client.CountryCode = _client.CountryCode;
			if (blocked.Contains(ip)) {
				takeAction(client, "Actions", true);
				return "already blocked";
			}
			client.Isp = getISP(ip, cfg["API"]["main"]).Replace($"\n", "").Replace($"\r", "");
			if (cfg["General"]["learn"] == "true") {
				var m = "unknown";
				if (client.Isp != "unknown")
					m = learn(client.Isp);
				var fb = getISP(client.Ip.ToString(), cfg["API"]["fallback"]).Replace($"\n", "").Replace($"\r", ""); ;
				var f = "unknown";
				if (client.Isp != "unknown")
					f = learn(fb);
				var fb2 = getISP(client.Ip.ToString(), cfg["API"]["fallback2"]).Replace($"\n", "").Replace($"\r", ""); ;
				var f2 = "unknown";
				if (client.Isp != "unknown")
					f2 = learn(fb2);
				return $"main ({client.Isp}): {m} | fallback ({fb}): {f} | fallback2 ({fb2}): {f2}";
			} else {
				if (client.Isp == "unknown") {
					client.Isp = getISP(ip, cfg["API"]["fallback"]).Replace($"\n", "");
					if (client.Isp == "unknown") {
						client.Isp = getISP(ip, cfg["API"]["fallback2"]).Replace($"\n", "");
					}
				}
			}
			Log.Debug($"Got ISP \"{client.Isp}\" for client \"{client.NickName}\" #{client.Id} (dbid:{_client.DatabaseId})");
			if (client.Isp.ToLower() == "unknown" || client.Isp.ToLower() == "undefined") {
				if (cfg["Unresolvable"]["enabled"] == "true" && cfg["General"]["learn"] != "true")
					takeAction(client, "Unresolvable");
				return "unknown or undefined";
			}
			return checkISP(client).ToString();
		}

		public string getISP(string ip, string api) {
			using (WebClient client = new WebClient()) {
				try {
					string url = api.Replace("{ip}", ip);
					Log.Debug(url.ToString());
					string isp = client.DownloadString(url);
					Log.Debug($"Got ISP {isp}");
					//if (downloadString != "undefined" && !String.IsNullOrWhiteSpace(downloadString))
					if (isp.StartsWith("AS"))
						try {
							int i = isp.IndexOf(" ") + 1;
							isp = isp.Substring(i);
							//downloadString = downloadString.Split(' ')[1];
						} catch { }
					return isp;
				} catch (Exception ex) {
					Log.Warn($"Unable to resolve ISP: {ex}");
				}
				return "unknown";
			}
		}

		/*
		public bool Setup() {
			string line;
			ConsoleKeyInfo key;
			Console.WriteLine($"{PluginInfo.Name}: === General setup ===");
			string section = "General";
			Console.WriteLine($"{PluginInfo.Shortname}: Blacklist mode means only the ISPs found in ISPs.txt will be blocked.");
			Console.WriteLine($"{PluginInfo.Shortname}: Whitelist mode means only the ISPs found in ISPs.txt will be allowed.");
			Console.Write($"{PluginInfo.Shortname}: Would you like to run the bot in (w)hitelist or (b)lacklist mode? > "); key = Console.ReadKey();
			if (key.KeyChar == 'b')
				cfg[section]["whitelist"] = "false";
			else if (key.KeyChar == 'w')
				cfg[section]["whitelist"] = "true";
			else { Console.WriteLine(); return false; }
			Console.Write($"{Environment.NewLine}{PluginInfo.Shortname}: Would you like to enable Debug mode? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				cfg[section]["debug"] = "true";
			else if (key.KeyChar == 'n')
				cfg[section]["debug"] = "false";
			else { Console.WriteLine(); return false; }
			Console.WriteLine($"{PluginInfo.Shortname}: Already resolved IP's will get cached for some time if you enable this.");
			Console.Write($"{PluginInfo.Shortname}: Clear cache interval in minutes (leave empty to disable cache) > "); line = Console.ReadLine();
			if (String.IsNullOrWhiteSpace(line)) {
				cfg[section]["clearcache"] = "0";
			} else {
				cfg[section]["clearcache"] = line;
			}
			Console.WriteLine($"{PluginInfo.Shortname}: Learn mode disables all actions and adds all new ISPs to the ISPs.txt until it got disabled.");
			Console.Write($"{PluginInfo.Shortname}: Would you like to enable learn mode now? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				cfg[section]["learn"] = "true";
			else if (key.KeyChar == 'n')
				cfg[section]["learn"] = "false";
			else { Console.WriteLine(); return false; }
			section = "Actions";
			Console.Write($"{Environment.NewLine}{PluginInfo.Shortname}: (k)ick or (b)an infringing clients? > "); key = Console.ReadKey();
			if (key.KeyChar == 'k')
				cfg[section]["kickonly"] = "true";
			else if (key.KeyChar == 'b') {
				cfg[section]["kickonly"] = "false";
				Console.Write($"{Environment.NewLine}{PluginInfo.Shortname}: Bantime in seconds? (0 means permanent) > "); line = Console.ReadLine();
				if (!int.TryParse(line, out var num)) {
					Console.WriteLine($"{PluginInfo.Shortname}: Bantime was not valid! (not a number?) > ");
					Console.WriteLine(); return false;
				}
				cfg[section]["bantime"] = line;
			} else {
				Console.WriteLine(); return false;
			}
			Console.WriteLine($"{PluginInfo.Shortname}: For the following 2 settings you can use {{ip}} {{isp}} {{nick}} {{clid}} {{uid}}");
			Console.Write($"{Environment.NewLine}{PluginInfo.Shortname}: Poke message before action is taken (leave empty to disable) > "); line = Console.ReadLine();
			if (String.IsNullOrWhiteSpace(line))
				cfg[section]["poke"] = String.Empty;
			else {
				cfg[section]["poke"] = line;
			}
			Console.Write($"{PluginInfo.Shortname}: Private message before action is taken (leave empty to disable) > "); line = Console.ReadLine();
			if (String.IsNullOrWhiteSpace(line))
				cfg[section]["msg"] = String.Empty;
			else {
				cfg[section]["msg"] = line;
			}
			section = "Unresolvable";
			Console.Write($"{PluginInfo.Shortname}: Kick clients whoms ISP couldn't be resolved? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y') {
				cfg[section]["enabled"] = "true";
				Console.WriteLine($"{Environment.NewLine}{PluginInfo.Shortname}: For the following 2 settings you can use {{ip}} {{isp}} {{nick}} {{clid}} {{uid}}");
				Console.Write($"{PluginInfo.Shortname}: Poke message if unresolvable (leave empty to disable) > "); line = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(line))
					cfg[section]["poke"] = String.Empty;
				else {
					cfg[section]["poke"] = line;
				}
				Console.Write($"{PluginInfo.Shortname}: Private message if unresolvable (leave empty to disable) > "); line = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(line))
					cfg[section]["msg"] = String.Empty;
				else {
					cfg[section]["msg"] = line;
				}
			} else if (key.KeyChar == 'n')
				cfg[section]["enabled"] = "false";
			else { Console.WriteLine(); return false; }
			*//*Console.WriteLine($"{Environment.NewLine}{PluginInfo.Name}: === API config ===");
			section = "API";
			Console.WriteLine($"{PluginInfo.Shortname}: Default Main API is http://ip-api.com/line/{{ip}}?fields=isp");
			Console.Write($"{PluginInfo.Shortname}: Main API to use? (leave empty for default) > "); line = Console.ReadLine();
			if (String.IsNullOrWhiteSpace(line))
				cfg[section]["main"] = "http://ip-api.com/line/{ip}?fields=isp";
			else {
				cfg[section]["main"] = line;
			}
			Console.Write($"{PluginInfo.Shortname}: Would you like to enable Fallback API? (y/n)"); key = Console.ReadKey();
			if (key.KeyChar == 'y') {
				Console.WriteLine($"{Environment.NewLine}{PluginInfo.Shortname}: Default Fallback API is http://ipinfo.io/{{ip}}/org");
				Console.Write($"{PluginInfo.Shortname}: Fallback API to use? (leave empty for default) > "); line = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(line))
					cfg[section]["fallback"] = "http://ipinfo.io/{ip}/org";
				else {
					cfg[section]["fallback"] = line;
				}
			} else if (key.KeyChar == 'n') {
			} else { Console.WriteLine(); return false; }*//*
			Console.WriteLine($"{Environment.NewLine}{PluginInfo.Name}: === Event config ===");
			section = "Events";
			Console.WriteLine($"{PluginInfo.Shortname}: This event gets fired when the bot connects.");
			Console.Write($"{PluginInfo.Shortname}: Should the bot check all clients for their ISPs when he connects? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				cfg[section]["onConnectStatusChange"] = "true";
			else if (key.KeyChar == 'n')
				cfg[section]["onConnectStatusChange"] = "false";
			else { Console.WriteLine(); return false; }
			Console.WriteLine($"{Environment.NewLine}{PluginInfo.Shortname}: This event gets fired when some client's IP changes.");
			Console.Write($"{PluginInfo.Shortname}: Should the bot re-check a client when his IP changed? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				cfg[section]["onUpdateClient"] = "true";
			else if (key.KeyChar == 'n')
				cfg[section]["onUpdateClient"] = "false";
			else { Console.WriteLine(); return false; }
			Console.WriteLine($"{Environment.NewLine}{PluginInfo.Shortname}: This event gets fired when a client connects.");
			Console.Write($"{PluginInfo.Shortname}: Should the bot check a client when they connect? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				cfg[section]["onClientMove"] = "true";
			else if (key.KeyChar == 'n')
				cfg[section]["onClientMove"] = "false";
			else { Console.WriteLine(); return false; }
			return true;
		}
		*/

#endregion

#region Commands

		[Command("ispv toggle", "Toggles ISPValidator")]
		public string CommandToggle() {
			Enabled = !Enabled;
			return PluginInfo.Name + " is now " + Enabled;
		}

		[Command("ispv save", "Saves the current configuration")]
		public string CommandSave() {
			var parser = new FileIniDataParser();
			parser.WriteFile(cfgfile, cfg);
			return $"{PluginInfo.Name}: Saved settings to {cfgfile}";
		}

		[Command("ispv check", "Checks a clientID")]
		public string CommandCheck(ExecutionInformation info, ClientIdT clid) {
			return $"Checked! Client #{clid} is {checkClient(clid)}";
		}

		[Command("ispv clear", "Clears the cache of ISPValidator")]
		public string CommandClear() {
			allowed.Clear();
			blocked.Clear();
			var lines = File.ReadAllLines(ispfile).Where(arg => !string.IsNullOrWhiteSpace(arg));
			File.WriteAllLines(ispfile, lines);
			return "Cleared Cache!";
		}
		#endregion
	}
} // TODO KICK CLIENT NO FLAG EXCEPT LOC
