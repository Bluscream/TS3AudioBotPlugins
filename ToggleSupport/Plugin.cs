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
using TS3AudioBot.Config;
using TS3AudioBot.Helper.Environment;
using System.Collections.Generic;
using IniParser;
using IniParser.Model;
using System.IO;

using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using System.Text;

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
		// private bool closed = false;

		private List<string> actions_open;
		private List<string> actions_close;

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

	public void Initialize()
		{
			LoadConfig();
			actions_open = PluginConfig["Templates"]["Actions Open"].Split(',').ToList();
			actions_close = PluginConfig["Templates"]["Actions Close"].Split(',').ToList();
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
				PluginConfig[section]["Action not found"] = "[color=red]Action %section% not found, only %available% are available!";
				PluginConfig[section]["Server Message Closed"] = "%section% channel was [color=orange]closed[/color] by %invoker% .";
				PluginConfig[section]["Server Message Open"] = "%section% channel was [color=green]opened[/color] by %invoker%.";
				PluginConfig[section]["Response Closed"] = "[color=orange]Succsessfully closed %section%[/color].";
				PluginConfig[section]["Response Open"] = "[color=green]Succsessfully opened %section%[/color].";
				PluginConfig[section]["Insufficient Permissions"] = "[color=red]Nice try ;)";
				PluginConfig[section]["Actions Close"] = "zu,close";
				PluginConfig[section]["Actions Open"] = "auf,open";
				PluginConfig[section]["Actions Toggle"] = "toggle";
				section = "support";
				PluginConfig[section]["Channel ID"] = "0";
				PluginConfig[section]["Name Open"] = " Support | Waiting Room";
				PluginConfig[section]["Name Closed"] = " Support | Waiting Room [Closed]";
				PluginConfig[section]["Permissions Open"] = "support_open.csv";
				PluginConfig[section]["Permissions Closed"] = "support_closed.csv";
				PluginConfig[section]["MaxClients Open"] = "10";
				PluginConfig[section]["Open At"] = "14:00";
				PluginConfig[section]["Close At"] = "22:00";
				PluginConfig[section]["Allowed Groups Open"] = "2,6";
				PluginConfig[section]["Allowed Groups Close"] = "2,6";
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
				return PluginConfig["Templates"]["Section not found"].Replace("%section%", name).Replace("%available%", string.Join(", ", sections)); // Todo: Do select
			}
			sections.Clear();
			var allowed_groups_open = PluginConfig[name]["Allowed Groups Open"].Split(',').Select(ChannelIdT.Parse).ToList();
			var allowed_groups_close = PluginConfig[name]["Allowed Groups Close"].Split(',').Select(ChannelIdT.Parse).ToList();
			var allowed_groups_all = allowed_groups_open.Concat(allowed_groups_close);
			var sgids = invoker.ServerGroups.ToList();
			bool hasMatch = sgids.Intersect(allowed_groups_all).Any();
			if (!hasMatch) return PluginConfig[name]["Insufficient Permissions"];
			action = action.Trim().ToLower();
			var actionfound = 0;
			foreach (var open_action in actions_open)
			{
				if (action == open_action) { actionfound = 1; break; }
			}
			foreach (var close_action in actions_close)
			{
				if (action == close_action) { actionfound = 2; break; }
			}
			if (actionfound < 1)
			{
				var available = string.Join(", ", actions_open) + string.Join(", ", actions_close);
				return PluginConfig["Templates"]["Action not found"].Replace("%action%", action).Replace("%available%", available); // Todo: Do select
			}
			if (actionfound == 1) {
				if (!sgids.Intersect(allowed_groups_open).Any()) return PluginConfig[name]["Insufficient Permissions"];
			} else {
				if (!sgids.Intersect(allowed_groups_close).Any()) return PluginConfig[name]["Insufficient Permissions"];
			}
			var close = actionfound == 2;
			return editSupportChannel(name, close, invoker);
		}

		public string editSupportChannel(string section, bool close, InvokerData invoker)
		{
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
						new CommandParameter("cid", PluginConfig[section]["Channel ID"]),
						new CommandParameter("channel_name", close ? PluginConfig[section]["Name Closed"] : PluginConfig[section]["Name Open"]),
						new CommandParameter("channel_maxclients", close ? "0" : PluginConfig[section]["MaxClients Open"]),
						new CommandParameter("channel_flag_maxclients_unlimited", close ? false : true)
				});
			var editResult = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!editResult.Ok) {
				var err = $"Could not {(close?"close":"open")} channel {PluginConfig[section]["Channel ID"]} ! ({editResult.Error.Message})";
				Log.Warn($"{PluginInfo.Name}: {err}"); return err;
			}
			var smsg = close ? PluginConfig[section]["Server Message Closed"] : PluginConfig[section]["Server Message Open"];
			if (!string.IsNullOrWhiteSpace(smsg)) {
				TS3Client.SendServerMessage(smsg
					.Replace("%invoker%", ClientURL((ushort)invoker.ClientId, invoker.ClientUid, invoker.NickName))
					.Replace("%section%", section));
			}
			return (close ? PluginConfig[section]["Response Closed"] : PluginConfig[section]["Response Opened"]).Replace("%section%", section);
		}

		public void Dispose()
		{
			actions_open.Clear();actions_close.Clear();
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
