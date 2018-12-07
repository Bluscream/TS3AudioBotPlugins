#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TS3AudioBot;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot.Helper;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client.Commands;
using TS3AudioBot.CommandSystem;
using IniParser;
using IniParser.Model;
using TS3Client;

namespace AntiAFK
{
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name = "Anti AFK";
		public static readonly string Description = "";
		public static readonly string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static readonly string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;

		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}

	public class AntiAFK : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public TS3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot ConfBot { get; set; }
		public ConfRoot ConfRoot { get; set; }
		public BotManager BotManager { get; set; }
		//public PlayManager BotPlayer { get; set; }
		//public IPlayerConnection PlayerConnection { get; set; }
		//public IVoiceTarget targetManager { get; set; }
		//public ConfHistory confHistory { get; set; }
		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		private static IniData PluginConfig;

		private TickWorker Timer { get; set; }
		private static string suid;
		private static Regex Regex;

		private static bool lastSuccessful;
		private static DateTime lastUpdate;
		private static DateTime lastMessage;

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				var section = "serveruid";
				PluginConfig[section]["Message"] = "[Aa][Ff][Kk]";
				PluginConfig[section]["Bot UID"] = "";
				PluginConfig[section]["Min Seconds"] = "5";
				PluginConfig[section]["Max Seconds"] = "10";
				PluginConfig[section]["Command"] = "clientupdate";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			suid = TS3FullClient.WhoAmI().Unwrap().VirtualServerUid;
			if (!PluginConfig.Sections.ContainsSection(suid)) return;
			var cfg = PluginConfig[suid];var hasmsg = cfg.ContainsKey("Message");var hasmin = cfg.ContainsKey("Min Seconds");var hasmax = cfg.ContainsKey("Max Seconds");
			if (!hasmsg && !hasmin && !hasmax) return;
			if (hasmsg) {
				Regex = new Regex(cfg["Message"], RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ECMAScript);
				TS3FullClient.OnEachTextMessage += OnEachTextMessage;
			}
			if (hasmin || hasmax) {
				var seconds = 60; // Defaults to 1 minute
				if (hasmin && hasmax) {
					var min = int.Parse(cfg["Min Seconds"]); var max = int.Parse(cfg["Max Seconds"]);
					Random rnd = new Random();
					seconds = rnd.Next(min, max);
				} else if (hasmin) {
					seconds = int.Parse(cfg["Min Seconds"]);
				} else if (hasmax) {
					seconds = int.Parse(cfg["Max Seconds"]);
				}
				Timer = TickPool.RegisterTick(Tick, TimeSpan.FromSeconds(seconds), true);
			}
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachTextMessage(object sender, TextMessage e)
		{
			if (e.Target != TextMessageTargetMode.Private) return;
			if (e.InvokerId == TS3FullClient.ClientId) return;
			var hasuid = PluginConfig[suid].ContainsKey("Bot UID");
			if (hasuid && e.InvokerUid != PluginConfig[suid]["Bot UID"]) return;
			var contains = e.Message.Contains(PluginConfig[suid]["Message"]);
			if (!contains) {
				var match = Regex.Match(e.Message).Success;
				if (!match) return;
			}
			lastMessage = DateTime.Now;
			Tick();
		}
		private void Tick()
		{
			var cfg = PluginConfig[suid];
			var cmd = cfg["Command"];
			var command = new Ts3Command(cmd, new List<ICommandPart>() {
					new CommandParameter("clid", TS3FullClient.ClientId)
			});
			var Result = TS3FullClient.SendNotifyCommand(command, NotificationType.ClientUpdated);
			if (Result.Ok) {
				lastSuccessful = true;
				lastUpdate = DateTime.Now;
			} else lastSuccessful = false;
			// return; // Todo Remove maybe?
			var hasmin = cfg.ContainsKey("Min Seconds"); var hasmax = cfg.ContainsKey("Max Seconds");
			if (hasmin && hasmax) {
				var min = int.Parse(cfg["Min Seconds"]); var max = int.Parse(cfg["Max Seconds"]);
				Random rnd = new Random();
				Timer.Interval = TimeSpan.FromSeconds(rnd.Next(min, max));
			}
		}


		[Command("antiafk")]
		public string CommandInfo()
		{
			var sb = new StringBuilder(Environment.NewLine);
			sb.AppendLine($"Saved Servers: {PluginConfig.Sections.Count}");
			sb.AppendLine($"Last Update: {lastUpdate} ({lastSuccessful})");
			sb.AppendLine($"Last Message: {lastMessage}");
			return sb.ToString();
		}
		[Command("antiafk list")]
		public string CommandList()
		{
			var sb = new StringBuilder(Environment.NewLine);
			foreach (var item in PluginConfig[suid])
			{
				sb.AppendLine($"{item.KeyName} = {item.Value}");
			}
			return sb.ToString();
		}
		[Command("antiafk set")]
		public void CommandSet(string setting, string value)
		{
			PluginConfig[suid][setting] = value;
			ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
		}

		public void Dispose()
		{
			Timer.Active = false;
			TickPool.UnregisterTicker(Timer);
			TS3FullClient.OnEachTextMessage -= OnEachTextMessage;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
