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

namespace AutoChannelCreate
{
	public class PluginInfo
	{
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "Allows you to set several locations where the current track is being announced.\n" +
										  "Edit the file NowPlaying.dll.config to your needs.\n" +
										  "Possible replacements: {now}, {botname}, {address}, {onconnect}, {onidle}, {ondisconnect}";
		public const string Url = "https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/AutoChannelCreate";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}
	public class AutoChannelCreate : IBotPlugin
	{
		//private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger(); /* TODO */
		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		private List<ChannelList> channelList = new List<ChannelList>();

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }		

		public void Initialize() {
			if (Regex.Match(Conf.Connect.Channel.Value, @"^\/\d+$").Success) {
				Console.WriteLine("AutoChannelCreate does not work if the default channel is set to an ID!");
				return;
			}
			TS3FullClient.OnChannelListFinished += Ts3Client_OnChannelListFinished;
			TS3FullClient.OnEachChannelList += Ts3Client_OnEachChannelList;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
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
			var now = DateTime.Now.ToString();
			int neededTP = -1; // 2147483647 // -1
			if (found == 0) {
				PluginLog(LogLevel.Warning, "Default channel does not exist yet, creating...");
				var commandCreate = new Ts3Command("channelcreate", new List<ICommandPart>() {
					new CommandParameter("channel_name", Conf.Connect.Channel.Value),
					new CommandParameter("channel_password", Conf.Connect.ChannelPassword.Get().HashedPassword),
					// new CommandParameter("channel_codec", 5), // * Radio *
					new CommandParameter("channel_codec_quality", 10),
					new CommandParameter("channel_flag_maxclients_unlimited", false),
					new CommandParameter("channel_maxclients", 10), // 50 / 10
					new CommandParameter("channel_needed_talk_power", neededTP),
					new CommandParameter("channel_topic", $"Created: {now}")
				});
				var createResult = TS3FullClient.SendNotifyCommand(commandCreate, NotificationType.ChannelCreated);
				if (!createResult.Ok) {
					PluginLog(LogLevel.Debug, $"{PluginInfo.Name}: Could not create default channel! ({createResult.Error.Message})"); return;
				}
				var createRes = createResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
				found = createRes.ChannelId;
			}
			if (found == 0 ) return;
			PluginLog(LogLevel.Debug, "Updating channel...");
			var uid = ((ConnectionDataFull)TS3FullClient.ConnectionData).Identity.ClientUid;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
			new CommandParameter("cid", found),
				new CommandParameter("channel_description",
				Properties.Resources.Description // Description // DescriptionRadio
					.Replace("{now}", now)
					.Replace("{botname}", Conf.Connect.Name)
					.Replace("{botuid}", uid)
					.Replace("{address}", Conf.Connect.Address)
					.Replace("{onconnect}", Conf.Events.OnConnect)
					.Replace("{onidle}", Conf.Events.OnIdle)
					.Replace("{ondisconnect}", Conf.Events.OnDisconnect)
				)
			});
			var editResult = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!editResult.Ok)
			{
				PluginLog(LogLevel.Debug, $"{PluginInfo.Name}: Could set channel description! ({editResult.Error.Message})"); return;
			}
			// var reditRes = editResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
			var tp = TS3FullClient.ClientInfo(TS3FullClient.ClientId).Value.TalkPower;
			if (tp >= neededTP) return;
			var commandTP = new Ts3Command("clientedit", new List<ICommandPart>() {
				new CommandParameter("clid", TS3FullClient.ClientId),
				new CommandParameter("client_is_talker", true)
			});
			var tpResult = TS3FullClient.SendNotifyCommand(commandTP, NotificationType.ClientUpdated);
			if (!tpResult.Ok) {
				PluginLog(LogLevel.Debug, $"{PluginInfo.Name}: Could grant own Talk Power! ({tpResult.Error.Message})"); return;
			}
			//tpRes = tpResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
			
		}

		public void Dispose() {
			TS3FullClient.OnChannelListFinished -= Ts3Client_OnChannelListFinished;
			TS3FullClient.OnEachChannelList -= Ts3Client_OnEachChannelList;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
