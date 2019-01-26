using System;
using System.Linq;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3Client.Helper;
using TS3AudioBot.Web.Api;
using TS3AudioBot.Helper;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using TS3AudioBot.Config;
using TS3AudioBot.Helper.Environment;
using System.Collections.Generic;
using IniParser;
using IniParser.Model;
using System.IO;

namespace ToggleSupport
{
	public class ToggleSupport : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }
		public IPlayerConnection PlayerConnection { get; set; }
		public IVoiceTarget targetManager { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		private static IniData PluginConfig;

		/* private ChannelIdT cid = 37;
		private string name_open = "┏ Support | Warteraum";
		private string name_closed = "┏ Support | Warteraum [Geschlossen]"; */
		private bool closed = false;

		public void Initialize()
		{
			LoadConfig();
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void LoadConfig()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			if (ConfigParser == null) ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				var section = "Templates";
				PluginConfig[section]["Section not found"] = "[color=red]Channel %section% not found, only %available% are available!";
				PluginConfig[section]["Action Close"] = "zu";
				section = "support";
				PluginConfig[section]["Channel ID"] = "0";
				PluginConfig[section]["Name Open"] = " Support | Waiting Room";
				PluginConfig[section]["Name Closed"] = " Support | Waiting Room [Closed]";
				PluginConfig[section]["Permissions Open"] = "support_open.csv";
				PluginConfig[section]["Permissions Closed"] = "support_closed.csv";
				PluginConfig[section]["Open At"] = "14:00";
				PluginConfig[section]["Close At"] = "22:00";
				PluginConfig[section]["Allowed Groups"] = "2,6";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
		}


		[Command("channel")]
		public string CommandToggleSupport(InvokerData invoker, string name, string action)
		{
			name = name.Trim().ToLower();
			var sectionfound = false;
			var sections = new List<string>(){ };
			foreach (var section in PluginConfig.Sections)
			{
				if (section.SectionName == "Templates") continue;
				sections.Add(section.SectionName);
				if (section.SectionName == name) { sectionfound = true; break; }
			}
			if (!sectionfound) {
				return PluginConfig["Templates"]["Section not found"].Replace("%section%", name).Replace("%available%", string.Join(", ", sections); // Todo: Do select
			}
			sections.Clear();
			action = action.Trim().ToLower();
			closed = !closed;
			return editSupportChannel(cid, closed, invoker);
		}
		[Command("support auf")]
		public string CommandOpenSupport(InvokerData invoker)
		{
			closed = false;
			return editSupportChannel(cid, closed, invoker);
		}
		[Command("support zu")]
		public string CommandCloseSupport(InvokerData invoker)
		{
			closed = true;
			return editSupportChannel(cid, closed, invoker);
		}

		public string editSupportChannel(ChannelIdT cid, bool close, InvokerData invoker)
		{
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
						new CommandParameter("cid", cid),
						new CommandParameter("channel_name", close ? name_closed : name_open),
						new CommandParameter("channel_maxclients", close ? "-1" : "-1"),
						new CommandParameter("channel_flag_maxclients_unlimited", close ? false : true)
				});
			var editResult = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!editResult.Ok) {
				var err = $"Could not {(close?"close":"open")} channel {cid} ! ({editResult.Error.Message})";
				Log.Warn($"{PluginInfo.Name}: {err}"); return err;
			}
			TS3Client.SendServerMessage($"Support channel wurde von \"{invoker.NickName}\" {(close ? "[color=orange]geschlossen" : "[color=green]geöffnet")}!");
			return $"{(close ? "[color=orange]Closed" : "[color=green]Opened")} Support!";
		}

		public void Dispose()
		{
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name;
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
}
