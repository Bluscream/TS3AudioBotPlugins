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
	public class AutoChannelCreate : IBotPlugin
	{

		private List<ChannelList> channelList = new List<ChannelList>();

		public Ts3FullClient Ts3FullClient { get; set; }

		public Ts3Client Ts3Client { get; set; }

		public ConfBot Conf { get; set; }

		public void Initialize() {
			if (Regex.Match(Conf.Connect.Channel.Value, @"^\/\d+$").Success) {
				Console.WriteLine("AutoChannelCreate does not work if the default channel is set to an ID!");
				return;
			}
			Ts3FullClient.OnChannelListFinished += Ts3Client_OnChannelListFinished;
			Ts3FullClient.OnEachChannelList += Ts3Client_OnEachChannelList;
		}

		private void Ts3Client_OnEachChannelList(object sender, ChannelList e) {
			channelList.Add(e);
		}

		private void Ts3Client_OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e) {
			ChannelIdT found = 0;
			foreach (ChannelList channel in channelList)
			{
				if (channel.Name == Conf.Connect.Channel.Value) {
					found = channel.ChannelId;
				}
			}
			if (found != 0) {
				Ts3Client.MoveTo(found, Conf.Connect.ChannelPassword.Password.Value);
			} else {
				Ts3Command command = new Ts3Command("channelcreate", new List<ICommandPart>() {
					new CommandParameter("channel_name", Conf.Connect.Channel.Value),
					new CommandParameter("channel_password", Conf.Connect.ChannelPassword.Get().HashedPassword),
					new CommandParameter("channel_codec_quality", 7),
					new CommandParameter("channel_flag_maxclients_unlimited", false),
					new CommandParameter("channel_maxclients", 10),
					new CommandParameter("channel_needed_talk_power", -1),
					new CommandParameter("channel_topic", $"Channel created at {DateTime.Now}")
				});
				var result = Ts3FullClient.SendNotifyCommand(command, NotificationType.ChannelCreated);
				if (!result.Ok) return;
				var res = result.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
				command = new Ts3Command("channeledit", new List<ICommandPart>() {
					new CommandParameter("cid", res.ChannelId),
					new CommandParameter("channel_description",
					Properties.Resources.Description
					.Replace("{botname}", Conf.Connect.Name)
					.Replace("{address}", Conf.Connect.Address)
					.Replace("{onconnect}", Conf.Events.OnConnect)
					.Replace("{onidle}", Conf.Events.OnIdle)
					.Replace("{ondisconnect}", Conf.Events.OnDisconnect)
					)
				});
				Ts3FullClient.SendNotifyCommand(command, NotificationType.ChannelEdited);
			}
		}

		public void Dispose() {
			Ts3FullClient.OnChannelListFinished -= Ts3Client_OnChannelListFinished;
			Ts3FullClient.OnEachChannelList -= Ts3Client_OnEachChannelList;
		}
	}
}
