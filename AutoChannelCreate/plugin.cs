using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using IniParser;
using IniParser.Model;

namespace AutoChannelCreate
{
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
	public class AutoChannelCreate : IBotPlugin {
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		private List<ChannelList> channelList = new List<ChannelList>();
		public Ts3FullClient TS3FullClient { get; set; }
		public Bot Bot { get; set; }
		public ConfBot Conf { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		public static IniData PluginConfig;

		public void Initialize() {
			if (Regex.Match(Conf.Connect.Channel.Value, @"^\/\d+$").Success) {
				Log.Error("AutoChannelCreate does not work if the default channel is set to an ID!");
				return;
			}
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				var bot = "default";
				PluginConfig[bot]["Name"] = "{auto}";
				PluginConfig[bot]["Password"] = "{auto}";
				PluginConfig[bot]["Codec"] = "5";
				PluginConfig[bot]["Codec Quality"] = "10";
				PluginConfig[bot]["Maxclients"] = "-1";
				PluginConfig[bot]["Needed Talk Power"] = "0";
				PluginConfig[bot]["Topic Template"] = "Created: {now}";
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			TS3FullClient.OnChannelListFinished += Ts3Client_OnChannelListFinished;
			TS3FullClient.OnEachChannelList += Ts3Client_OnEachChannelList;
			Log.Info("Plugin {} v{} by {} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void Ts3Client_OnEachChannelList(object sender, ChannelList e) {
			channelList.Add(e);
		}

		private void Ts3Client_OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e) {
			ChannelIdT found = 0;
			foreach (ChannelList channel in channelList) {
				if (channel.Name == Conf.Connect.Channel.Value) {
					found = channel.ChannelId;
				}
			}
			var bot = Bot.Name;
			if (!PluginConfig.Sections.ContainsSection(bot)) {
				Log.Warn("No section found for \"{}\" \"{}\"! Skipping...", bot, PluginConfigFile); return;
			}
			var now = DateTime.Now.ToString();
			var neededTP = PluginConfig[bot]["Needed Talk Power"]; // .Replace("{max}", int.MaxValue.ToString())
			var channel_needed_talk_power = (neededTP == "{max}") ? int.MaxValue : int.Parse(neededTP);
			if (found == 0) {
				var channel_name = PluginConfig[bot]["Name"].Replace("{auto}", Conf.Connect.Channel.Value);
				Log.Info("Default channel \"{}\" for template \"{}\" does not exist yet, creating...", channel_name, bot);
				var commandCreate = new Ts3Command("channelcreate", new List<ICommandPart>() {
					new CommandParameter("channel_name", channel_name),
					new CommandParameter("channel_password", PluginConfig[bot]["Password"].Replace("{auto}", Conf.Connect.ChannelPassword.Get().HashedPassword)),
					new CommandParameter("channel_codec", PluginConfig[bot]["Codec"]), 
					new CommandParameter("channel_codec_quality", PluginConfig[bot]["Codec Quality"]),
					new CommandParameter("channel_flag_maxclients_unlimited", PluginConfig[bot]["Maxclients"] == "-1"),
					new CommandParameter("channel_maxclients", PluginConfig[bot]["Maxclients"]),
					new CommandParameter("channel_needed_talk_power", channel_needed_talk_power),
					new CommandParameter("channel_topic", PluginConfig[bot]["Topic Template"].Replace("{now}", now))
				});
				var createResult = TS3FullClient.SendNotifyCommand(commandCreate, NotificationType.ChannelCreated);
				if (!createResult.Ok) {
					Log.Debug($"{PluginInfo.Name}: Could not create default channel! ({createResult.Error.Message})"); return;
				}
				var createRes = createResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
				found = createRes.ChannelId;
			}
			if (found == 0 ) return;
			var descriptionFile = Path.Combine(ConfRoot.Plugins.Path.Value, "Descriptions", $"{bot}.txt");
			if (File.Exists(descriptionFile)) {
				Log.Debug("Updating channel...");
				var uid = ((ConnectionDataFull)TS3FullClient.ConnectionData).Identity.ClientUid;
				var descriptionText = File.ReadAllText(descriptionFile);
				var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
				new CommandParameter("cid", found),
					new CommandParameter("channel_description",
						descriptionText
						.Replace("{now}", now)
						.Replace("{botname}", Conf.Connect.Name)
						.Replace("{botuid}", uid)
						.Replace("{botclid}", TS3FullClient.ClientId.ToString())
						.Replace("{address}", Conf.Connect.Address)
						.Replace("{onconnect}", Conf.Events.OnConnect)
						.Replace("{onidle}", Conf.Events.OnIdle)
						.Replace("{ondisconnect}", Conf.Events.OnDisconnect)
					)
				});
				var editResult = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
				if (!editResult.Ok)
				{
					Log.Debug($"{PluginInfo.Name}: Could set channel description! ({editResult.Error.Message})"); return;
				}
				// var reditRes = editResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
			}
			var tp = TS3FullClient.ClientInfo(TS3FullClient.ClientId).Value.TalkPower;
			if (tp >= channel_needed_talk_power) return;
			var commandTP = new Ts3Command("clientedit", new List<ICommandPart>() {
				new CommandParameter("clid", TS3FullClient.ClientId),
				new CommandParameter("client_is_talker", true)
			});
			var tpResult = TS3FullClient.SendNotifyCommand(commandTP, NotificationType.ClientUpdated);
			if (!tpResult.Ok) {
				Log.Debug($"{PluginInfo.Name}: Could grant own Talk Power! ({tpResult.Error.Message})"); return;
			}
			//tpRes = tpResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
			
		}

		public void Dispose() {
			TS3FullClient.OnChannelListFinished -= Ts3Client_OnChannelListFinished;
			TS3FullClient.OnEachChannelList -= Ts3Client_OnEachChannelList;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
