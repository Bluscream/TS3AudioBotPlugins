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
		public const string Description = "This script will autokick everyone not using a whitelisted ISP.";
		public const string Url = "";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}

	public class ISPValidator : ITabPlugin {
		private MainBot bot;
		private Ts3FullClient lib;
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

		public bool Setup() {
			string line;
			ConsoleKeyInfo key;
			IniData data = new IniData();
			Console.WriteLine($"{PluginInfo.Name}: === General setup ===");
			string section = "General";
			Console.WriteLine($"{PluginInfo.Name}: Blacklist mode means only the ISPs found in ISPs.txt will be blocked.");
			Console.WriteLine($"{PluginInfo.Name}: Whitelist mode means only the ISPs found in ISPs.txt will be allowed.");
			Console.Write($"{PluginInfo.Name}: Would you like to run the bot in (w)hitelist or (b)lacklist mode?"); key = Console.ReadKey();
			if (key.KeyChar == 'b')
				data[section]["whitelist"] = "false";
			else if (key.KeyChar == 'w')
				data[section]["whitelist"] = "true";
			else { return false; }
			Console.Write($"{PluginInfo.Name}: Would you like to enable Debug mode? (y/n)"); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				data[section]["debug"] = "true";
			else if (key.KeyChar == 'n')
				data[section]["debug"] = "false";
			else { return false; }
			Console.Write($"{PluginInfo.Name}: (k)ick or (b)an infringing clients?"); key = Console.ReadKey();
			if (key.KeyChar == 'k')
				data[section]["kickonly"] = "true";
			else if (key.KeyChar == 'b') {
				data[section]["kickonly"] = "false";
				Console.Write($"{PluginInfo.Name}: Bantime in seconds? (0 means permanent)"); line = Console.ReadLine();
				if (!int.TryParse(line, out var num)) {
					Console.WriteLine($"{PluginInfo.Name}: Bantime was not valid! (not a number?)");
					return false;
				}
				data[section]["bantime"] = line;
			} else {
				data[section]["bantime"] = "1";
				return false;
			}
			Console.Write($"{PluginInfo.Name}: Kick clients whoms ISP couldn't be resolved? (y/n)"); key = Console.ReadKey();
			if (key.KeyChar == 'y')
				data[section]["kickunknown"] = "true";
			else if (key.KeyChar == 'n')
				data[section]["kickunknown"] = "false";
			else { return false; }
			Console.Write($"{PluginInfo.Name}: Poke message before action is taken (leave empty to disable)"); line = Console.ReadLine();
			if (String.IsNullOrWhiteSpace(line))
				data[section]["poke"] = String.Empty;
			else {
				data[section]["poke"] = line;
			}
			Console.Write($"{PluginInfo.Name}: Private message before action is taken (leave empty to disable)"); line = Console.ReadLine();
			if (String.IsNullOrWhiteSpace(line))
				data[section]["msg"] = String.Empty;
			else {
				data[section]["msg"] = line;
			}
			Console.WriteLine($"{PluginInfo.Name}: === API config ===");
			section = "API";
			Console.WriteLine($"{PluginInfo.Name}: Default Main API is http://ip-api.com/line/{{ip}}?fields=isp");
			Console.Write($"{PluginInfo.Name}: Main API to use? (leave empty for default)"); line = Console.ReadLine();
			if (String.IsNullOrWhiteSpace(line))
				data[section]["main"] = "http://ip-api.com/line/{ip}?fields=isp";
			else {
				data[section]["main"] = line;
			}
			Console.Write($"{PluginInfo.Name}: Would you like to enable Fallback API? (y/n)"); key = Console.ReadKey();
			if (key.KeyChar == 'y') {
				Console.WriteLine($"{PluginInfo.Name}: Default Fallback API is http://ipinfo.io/{{ip}}/org");
				Console.Write($"{PluginInfo.Name}: Fallback API to use? (leave empty for default)"); line = Console.ReadLine();
				if (String.IsNullOrWhiteSpace(line))
					data[section]["main"] = "http://ipinfo.io/{ip}/org";
				else {
					data[section]["main"] = line;
				}
			} else if (key.KeyChar == 'n')
				data[section]["debug"] = "false";
			else { return false; }
			return true;
		}

		public void Initialize(MainBot mainBot) {
			bot = mainBot;
			var pluginPath = mainBot.ConfigManager.GetDataStruct<PluginManagerData>("PluginManager", true).PluginPath;
			cfgfile = Path.Combine(pluginPath, $"{PluginInfo.Name}.cfg");
			ispfile = Path.Combine(pluginPath, "ISPs.txt");
			lib = mainBot.QueryConnection.GetLowLibrary<Ts3FullClient>();
			if (File.Exists(cfgfile)) {
				var parser = new FileIniDataParser();
				cfg = parser.ReadFile(cfgfile);
			} else { while (!Setup()) { } }
			lib.OnClientMoved += Lib_OnClientMoved;
			lib.OnConnected += Lib_OnConnected;
			Enabled = true; PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

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

		public void Dispose() {
			//Timer.Active = false;
			//TickPool.UnregisterTicker(Timer);
			lib.OnClientMoved -= Lib_OnClientMoved;
			lib.OnConnected -= Lib_OnConnected;
			PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
