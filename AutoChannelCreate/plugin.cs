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
	public class PluginInfo {
		public static readonly string ShortName = typeof(PluginInfo).Namespace;
		public static readonly string Name = string.IsNullOrEmpty(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name) ? ShortName : System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
		public static string Description = "";
		public static string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		public static readonly string ConfigFile = $"{ShortName}.ini";
		public PluginInfo()
		{
			var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
			Description = versionInfo.FileDescription;
			Author = versionInfo.CompanyName;
		}
	}
	public class AutoChannelCreate : IBotPlugin {
		//public PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		private List<ChannelList> channelList = new List<ChannelList>();

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }

		public static IniData PluginConfig;

		public void Initialize() {
			if (Regex.Match(Conf.Connect.Channel.Value, @"^\/\d+$").Success) {
				Log.Error("AutoChannelCreate does not work if the default channel is set to an ID!");
				return;
			}
			var success = new Config().LoadConfig();
			if (!success) {
				Log.Warn("Config for plugin {} created, please modify it and reload!");
				return;
			}
			TS3FullClient.OnChannelListFinished += Ts3Client_OnChannelListFinished;
			TS3FullClient.OnEachChannelList += Ts3Client_OnEachChannelList;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
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
			int neededTP = int.MaxValue; // int.MaxValue // -1
			if (found == 0) {
				Log.Warn("Default channel does not exist yet, creating...");
				var commandCreate = new Ts3Command("channelcreate", new List<ICommandPart>() {
					new CommandParameter("channel_name", Conf.Connect.Channel.Value),
					new CommandParameter("channel_password", Conf.Connect.ChannelPassword.Get().HashedPassword),
					new CommandParameter("channel_codec", (int) Codec.OpusMusic), // * Radio *
					new CommandParameter("channel_codec_quality", 10),
					new CommandParameter("channel_flag_maxclients_unlimited", false),
					new CommandParameter("channel_maxclients", 50), // 50 / 10
					new CommandParameter("channel_needed_talk_power", neededTP),
					new CommandParameter("channel_topic", $"Created: {now}")
				});
				var createResult = TS3FullClient.SendNotifyCommand(commandCreate, NotificationType.ChannelCreated);
				if (!createResult.Ok) {
					Log.Debug($"{PluginInfo.Name}: Could not create default channel! ({createResult.Error.Message})"); return;
				}
				var createRes = createResult.Value.Notifications.Cast<ChannelCreated>().FirstOrDefault();
				found = createRes.ChannelId;
			}
			if (found == 0 ) return;
			Log.Debug("Updating channel...");
			var uid = ((ConnectionDataFull)TS3FullClient.ConnectionData).Identity.ClientUid;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
			new CommandParameter("cid", found),
				new CommandParameter("channel_description",
				Properties.Resources.DescriptionRadio // Description // DescriptionRadio
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
			var tp = TS3FullClient.ClientInfo(TS3FullClient.ClientId).Value.TalkPower;
			if (tp >= neededTP) return;
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
