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
		public Ts3Client TS3Client { get; set; }
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
				var bot = "bot name without bot_";
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
			try {
				ChannelIdT found = 0;
				var channel_name = Conf.Connect.Channel.Value.Split('/').Last();
				foreach (ChannelList channel in channelList) {
					if (channel.Name == channel_name) {
						found = channel.ChannelId;
					}
				}
				var bot = Bot.Name;
				if (!PluginConfig.Sections.ContainsSection(bot)) {
					Log.Warn("No section found for {} in {}! Skipping...", bot, PluginConfigFile); return;
				}
				var now = DateTime.Now.ToString();
				var neededTP = PluginConfig[bot]["Needed Talk Power"];
				int.TryParse(neededTP, out var nTP);
				var channel_needed_talk_power = (neededTP == "max") ? int.MaxValue: nTP;
				// Log.Debug(string.Join(", ", channelList.Select(person => person.Name)));
				if (found == 0) {
					Log.Info("Default channel {} for template {} does not exist yet, creating...", channel_name, bot);
					var commandCreate = new Ts3Command("channelcreate", new List<ICommandPart>() {
						new CommandParameter("channel_name", channel_name)
					});
					var channel_password = Conf.Connect.ChannelPassword.Get().HashedPassword;
					if (!string.IsNullOrEmpty(channel_password)) commandCreate.AppendParameter(new CommandParameter("channel_password", channel_password));
					var channel_codec = PluginConfig[bot]["Codec"];
					if (!string.IsNullOrEmpty(channel_codec)) commandCreate.AppendParameter(new CommandParameter("channel_codec", channel_codec));
					var channel_codec_quality = PluginConfig[bot]["Codec Quality"];
					if (!string.IsNullOrEmpty(channel_codec_quality)) commandCreate.AppendParameter(new CommandParameter("channel_codec_quality", channel_codec_quality));
					var channel_maxclients = PluginConfig[bot]["Maxclients"];
					if (!string.IsNullOrEmpty(channel_maxclients)) {
						commandCreate.AppendParameter(new CommandParameter("channel_maxclients", channel_maxclients));
						commandCreate.AppendParameter(new CommandParameter("channel_flag_maxclients_unlimited", channel_maxclients == "-1"));
					}
					if (!string.IsNullOrEmpty(neededTP)) commandCreate.AppendParameter(new CommandParameter("channel_needed_talk_power", channel_needed_talk_power));
					var channel_topic = PluginConfig[bot]["Topic Template"];
					if (!string.IsNullOrEmpty(channel_topic)) commandCreate.AppendParameter(new CommandParameter("channel_topic", channel_topic.Replace("{now}", now)));
					// Log.Debug(commandCreate);
					var createResult = TS3FullClient.SendNotifyCommand(commandCreate, NotificationType.ChannelCreated);
					if (!createResult.Ok) {
						Log.Debug($"{PluginInfo.Name}: Could not create default channel! ({createResult.Error.Message})"); return;
					}
					var createRes = createResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
					found = createRes.ChannelId;
				}
				if (found == 0 ) return;
				var commandEdit = ChannelCreateEdit(bot, true, found);
				if (commandEdit.Item1) {
					var editResult = TS3FullClient.SendNotifyCommand(commandEdit.Item2, NotificationType.ChannelEdited);
					if (!editResult.Ok)
					{
						Log.Debug($"{PluginInfo.Name}: Could not set channel description! ({editResult.Error.Message})"); return;
					}
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
			} catch (ArgumentNullException ex) { Log.Error($"{Bot.Name}: Unable to run {PluginInfo.Name}.Ts3Client_OnChannelListFinished ({ex.Message})"); }
		}

		public Tuple<bool,Ts3Command> ChannelCreateEdit(string bot, bool edit = false, ChannelIdT cid = 0)
		{
			var command = new Ts3Command(edit ? "channeledit" : "channelcreate", new List<ICommandPart>());
			if (edit) {
				new CommandParameter("cid", cid);
			} else
			{
				command.AppendParameter(new CommandParameter("channel_name", Conf.Connect.Channel.Value));
			}
			var send = false;

			var channel_password = Conf.Connect.ChannelPassword.Get().HashedPassword;
			if (!string.IsNullOrEmpty(channel_password)) {
				command.AppendParameter(new CommandParameter("channel_password", channel_password));
			}

			var channel_codec = PluginConfig[bot]["Codec"];
			if (!string.IsNullOrEmpty(channel_codec) && (!edit || channel_codec.StartsWith("edit:"))) {
				command.AppendParameter(new CommandParameter("channel_codec", channel_codec)); send = true;
			}
			var channel_codec_quality = PluginConfig[bot]["Codec Quality"];
			if (!string.IsNullOrEmpty(channel_codec_quality) && (!edit || channel_codec_quality.StartsWith("edit:"))) {
				command.AppendParameter(new CommandParameter("channel_codec_quality", channel_codec_quality)); send = true;
			}

			var channel_maxclients = PluginConfig[bot]["Maxclients"];
			if (!string.IsNullOrEmpty(channel_maxclients) && (!edit || channel_maxclients.StartsWith("edit:"))) {
				command.AppendParameter(new CommandParameter("channel_maxclients", channel_maxclients));
				command.AppendParameter(new CommandParameter("channel_flag_maxclients_unlimited", channel_maxclients == "-1"));
			}

			var neededTP = PluginConfig[bot]["Needed Talk Power"];
			int.TryParse(neededTP, out var nTP);
			var channel_needed_talk_power = (neededTP == "{max}") ? int.MaxValue : nTP;
			if (!string.IsNullOrEmpty(neededTP) && (!edit || neededTP.StartsWith("edit:"))) {
				command.AppendParameter(new CommandParameter("channel_needed_talk_power", channel_needed_talk_power));
			}

			var channel_topic = PluginConfig[bot]["Topic"];
			if (!string.IsNullOrEmpty(channel_topic) && (!edit || channel_topic.StartsWith("edit:"))) {
				command.AppendParameter(new CommandParameter("channel_topic", channel_topic.Replace("{now}", DateTime.Now.ToString())));
			}

			var channel_description = PluginConfig[bot]["DescriptionFile"];
			if (!string.IsNullOrEmpty(channel_description) && (!edit || channel_description.StartsWith("edit:")))
			{
				var descriptionFile = Path.Combine(ConfRoot.Plugins.Path.Value, "Descriptions", $"{channel_description.Replace("edit:", "")}.txt");
				var descriptionText = File.ReadAllText(descriptionFile);
				command.AppendParameter(new CommandParameter("channel_description", descriptionText
					.Replace("{now}", DateTime.Now.ToString())
					.Replace("{botname}", Conf.Connect.Name)
					.Replace("{botuid}", ((ConnectionDataFull)TS3FullClient.ConnectionData).Identity.ClientUid) // TS3FullClient.IdentityData.ClientUid
					.Replace("{botclid}", TS3FullClient.ClientId.ToString())
					.Replace("{address}", Conf.Connect.Address)
					.Replace("{onconnect}", Conf.Events.OnConnect)
					.Replace("{onidle}", Conf.Events.OnIdle)
					.Replace("{ondisconnect}", Conf.Events.OnDisconnect)
					.Replace("{template}", Bot.Name)
				));
			}

			return Tuple.Create(send,command);
		}

		public void Dispose() {
			TS3FullClient.OnChannelListFinished -= Ts3Client_OnChannelListFinished;
			TS3FullClient.OnEachChannelList -= Ts3Client_OnEachChannelList;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
