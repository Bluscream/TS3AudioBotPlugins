using System;
using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3Client.Messages;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Helper;
using TS3Client.Full;
using IniParser;
using IniParser.Model;
using ClientUidT = System.String;
using ClientDbIdT = System.UInt64;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ServerGroupIdT = System.UInt64;
using ChannelGroupIdT = System.UInt64;
using System.IO;

namespace ISPValidator {

	public class PluginInfo {
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Shortname = "ISPV";
		public const string Description = "This script will autokick everyone not using a whitelisted ISP.";
		public const string Url = "";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}

	public class ISPValidator : ITabPlugin {
		private MainBot bot;
		private Ts3FullClient lib;
		private static FileIniDataParser iniParser;
		private static IniData cfg;
		private static string cfgfile;
		private static string ispfile;
		public TickWorker Timer { get; private set; }
		public bool Enabled { get; private set; }
		public bool CCState;

		public PluginInfo pluginInfo = new PluginInfo();

		public void PluginLog(Log.Level logLevel, string Message) {
			Log.Write(logLevel, PluginInfo.Name + ": " + Message);
		}

		public void Initialize(MainBot mainBot) {
			bot = mainBot;
			//var pluginPath = mainBot.ConfigManager.GetDataStruct<PluginManagerData>("PluginManager", true).PluginPath;
			var pluginPath = "Plugins";
			cfgfile = Path.Combine(pluginPath, $"{PluginInfo.Name}.cfg");
			ispfile = Path.Combine(pluginPath, "ISPs.txt");
			lib = mainBot.QueryConnection.GetLowLibrary<Ts3FullClient>();
			if (File.Exists(cfgfile)) {
				cfg = iniParser.ReadFile(cfgfile);
			} else {
				cfg = new IniData();
				while (!Setup()) { }
			}
			lib.OnClientMoved += Lib_OnClientMoved;
			lib.OnConnected += Lib_OnConnected;
			Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		public void Dispose() {
			//Timer.Active = false;
			//TickPool.UnregisterTicker(Timer);
			lib.OnClientMoved -= Lib_OnClientMoved;
			lib.OnConnected -= Lib_OnConnected;
			PluginLog(Log.Level.Debug, $"Saved Settings to \"{cfgfile}\".");
			PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}

		#region Events

		private void Lib_OnConnected(object sender, EventArgs e) {
			if (!Enabled) { return; }
			PluginLog(Log.Level.Debug, "Our client is now connected, setting channel commander :)");
			bot.QueryConnection.SetChannelCommander(true);
		}

		private void Lib_OnClientMoved(object sender, IEnumerable<ClientMoved> e) {
			if (!Enabled) { return; }
			foreach (var client in e) {
				if (lib.ClientId != client.ClientId) continue;
				PluginLog(Log.Level.Debug, "Our client was moved to " + client.TargetChannelId.ToString() + " because of " + client.Reason + ", setting channel commander :)");
				bot.QueryConnection.SetChannelCommander(true);
				return;
			}
		}

		#endregion

		#region Functions

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
			return true;
			Console.Write($"{Environment.NewLine}{PluginInfo.Shortname}: Would you like to enable Debug mode? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				cfg[section]["debug"] = "true";
			else if (key.KeyChar == 'n')
				cfg[section]["debug"] = "false";
			else { Console.WriteLine(); return false; }
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
			Console.Write($"{PluginInfo.Shortname}: Kick clients whoms ISP couldn't be resolved? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y') {
				cfg[section]["kickunknown"] = "true";
				Console.WriteLine($"{Environment.NewLine}{PluginInfo.Shortname}: For the following 2 settings you can use {{ip}} {{isp}} {{nick}} {{clid}} {{uid}}");
				Console.Write($"{PluginInfo.Shortname}: Poke message if unresolvable (leave empty to disable) > "); line = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(line))
					cfg[section]["unknownpoke"] = String.Empty;
				else {
					cfg[section]["unknownpoke"] = line;
				}
				Console.Write($"{PluginInfo.Shortname}: Private message if unresolvable (leave empty to disable) > "); line = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(line))
					cfg[section]["unknownmsg"] = String.Empty;
				else {
					cfg[section]["unknownmsg"] = line;
				}
			} else if (key.KeyChar == 'n')
				cfg[section]["kickunknown"] = "false";
			else { Console.WriteLine(); return false; }
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
			Console.WriteLine($"{PluginInfo.Shortname}: Already resolved IP's will get cached for some time if you enable this.");
			Console.Write($"{PluginInfo.Shortname}: Clear cache interval in minutes (leave empty to disable cache) > "); line = Console.ReadLine();
			if (!String.IsNullOrWhiteSpace(line))
				cfg[section]["clearcache"] = line;
			Console.WriteLine($"{PluginInfo.Shortname}: Learn mode disables all actions and adds all new ISPs to the ISPs.txt until it got disabled.");
			Console.Write($"{PluginInfo.Shortname}: Would you like to enable learn mode now? (y/n) > "); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				cfg[section]["learn"] = "true";
			else if (key.KeyChar == 'n')
				cfg[section]["learn"] = "false";
			else { Console.WriteLine(); return false; }
			Console.WriteLine($"{Environment.NewLine}{PluginInfo.Name}: === API config ===");
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
					cfg[section]["main"] = "http://ipinfo.io/{ip}/org";
				else {
					cfg[section]["main"] = line;
				}
			} else if (key.KeyChar == 'n') {
			} else { Console.WriteLine(); return false; }
			return true;
		}
	}
}

#endregion
