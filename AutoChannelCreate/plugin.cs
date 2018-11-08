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
			string math = "";
			string mathResult = "?";
			ChannelIdT found = 0;
			foreach (ChannelList channel in channelList) {
				if (channel.Name == Conf.Connect.Channel.Value) {
					found = channel.ChannelId;
				}
			}
			//Ts3Client.MoveTo(found, Conf.Connect.ChannelPassword.Password.Value);
			if (found == 0) {
				PluginLog(LogLevel.Warning, "Default channel does not exist yet, creating...");
				Random rnd = new Random();
				int rnd1 = rnd.Next(1, 999999);
				int rnd2 = rnd.Next(1, 999999);
				int rnd3 = rnd.Next(1, 555555);
				int rnd4 = rnd.Next(1, 10);
				int rnd5 = rnd.Next(1, 10);
				math = $"{rnd1} + {rnd2} - {rnd3} / {rnd4} * {rnd5}";
				mathResult = (rnd1 + rnd2 - rnd3 / rnd4 * rnd5).ToString();
				// int randomPasswordResultFloored = (int) Math.Floor(randomPasswordResult / 0.0);
				var commandCreate = new Ts3Command("channelcreate", new List<ICommandPart>() {
					new CommandParameter("channel_name", Conf.Connect.Channel.Value),
					new CommandParameter("channel_password", Conf.Connect.ChannelPassword.Get().HashedPassword), // randomPasswordResultFloored
					new CommandParameter("channel_codec_quality", 7),
					new CommandParameter("channel_flag_maxclients_unlimited", false),
					new CommandParameter("channel_maxclients", 10),
					new CommandParameter("channel_needed_talk_power", -1),
					new CommandParameter("channel_topic", $"Created: {DateTime.Now}")
				});
				var result = TS3FullClient.SendNotifyCommand(commandCreate, NotificationType.ChannelCreated);
				if (!result.Ok) {
					PluginLog(LogLevel.Debug, $"{PluginInfo.Name}: Could not create default channel! ({result.Error.Message})"); return;
				}
				var res = result.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
				found = res.ChannelId;
			/*} else {
				if (string.IsNullOrEmpty(randomPassword))
				{
					var result = TS3FullClient.SendNotifyCommand(new Ts3Command("channelgetdescription", new List<ICommandPart>() { new CommandParameter("cid", found) }), NotificationType.ChannelEdited);
					if (!result.Ok)
					{
						PluginLog(LogLevel.Debug, $"{PluginInfo.Name}: Could not get channel description! ({result.Error.Message})"); return;
					}
					var res = result.Value.Notifications.Cast<ChannelEdited>().FirstOrDefault();
					//res.Topic
				}*/
			}
			if (found == 0 ) return;
			PluginLog(LogLevel.Debug, "Updating channel...");
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
			new CommandParameter("cid", found),
				new CommandParameter("channel_description",
				Properties.Resources.Description
					.Replace("{now}", DateTime.Now.ToString())
					.Replace("{botname}", Conf.Connect.Name)
					.Replace("{address}", Conf.Connect.Address)
					.Replace("{onconnect}", Conf.Events.OnConnect)
					.Replace("{onidle}", Conf.Events.OnIdle)
					.Replace("{ondisconnect}", Conf.Events.OnDisconnect)
					.Replace("{math}", math)
					.Replace("{result}", mathResult)
				)
			});
			TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
		}

		public void Dispose() {
			TS3FullClient.OnChannelListFinished -= Ts3Client_OnChannelListFinished;
			TS3FullClient.OnEachChannelList -= Ts3Client_OnEachChannelList;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
