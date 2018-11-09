using System;
using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;

namespace ChannelEdit
{
	public class PluginInfo
	{
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "";
		public const string Url = "https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/ChannelEdit";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}
	public class ChannelEdit : IBotPlugin
	{
		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }

		public void Initialize()
		{
			TS3FullClient.OnEachClientUpdated += OnEachClientUpdated; // Remove this for security reasons!
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		private void OnEachClientUpdated(object sender, ClientUpdated e)
		{
			try {
				var me = TS3FullClient.WhoAmI().Value;
				if (e.ClientId == e.ClientId) return;
				var client = TS3Client.GetCachedClientById(e.ClientId).Value;
				ChannelIdT cid = client.ChannelId;
				if (cid != me.ChannelId) return;
				var clientInfo = TS3Client.GetClientInfoById(e.ClientId).Value;
				var tpmsg = clientInfo.TalkPowerRequestMessage;
				if (tpmsg != "ts3ab") return;
				var commandEdit = new Ts3Command("clientedit", new List<ICommandPart>() { new CommandParameter("clid", e.ClientId), new CommandParameter("client_is_talker", true) });
				TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ClientUpdated);
			} catch { }
		}

		[Command("ce name", PluginInfo.Description)]
		public string CommandEditChannelName(string name)
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() { new CommandParameter("cid", ownChannelId),
				new CommandParameter("channel_name", name)
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			else { return $"Channel Name set to: [b]{name}[/b]"; }
		}

		[Command("ce pw", "Syntax: !ce <new password (empty=no pw)>")]
		public string CommandEditChannelPassword(string password = "")
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() { new CommandParameter("cid", ownChannelId),
				new CommandParameter("channel_password", Ts3Crypt.HashPassword(password))
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			if (string.IsNullOrEmpty(password)) { return "Channel Password removed!";
			} else { return $"Channel Password set to: [b]{password}[/b]"; }
		}

		[Command("ce tp", "Syntax: !ce <needed talk power>")]
		public string CommandEditChannelTalkPower(int tp)
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() { new CommandParameter("cid", ownChannelId),
				new CommandParameter("channel_needed_talk_power", tp)
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			else { return $"Channel Needed Talk Power set to: [b]{tp}[/b]"; }
		}

		[Command("ce tpg", "Syntax: !ce tpgrant <client id>")]
		public string CommandToggleTalkPower(ClientIdT clid)
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var wasTalker = TS3FullClient.ClientInfo(clid).Value.TalkPowerGranted;
			var commandEdit = new Ts3Command("clientedit", new List<ICommandPart>() { new CommandParameter("clid", clid),
				new CommandParameter("client_is_talker", !wasTalker)
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ClientUpdated);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			if (!wasTalker) return $"Granted Talk Power to [b]{clid}[/b]";
			return $"Revoked Talk Power from [b]{clid}[/b]";
		}

		[Command("ce cg", "Syntax: !ce cg <channel group id> <client database id>")]
		public string CommandAssignChannelGroup(ulong dbid, ulong cgid)
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var commandEdit = new Ts3Command("setclientchannelgroup", new List<ICommandPart>() { new CommandParameter("cid", ownChannelId),
				new CommandParameter("cldbid", dbid),
				new CommandParameter("cgid", cgid),
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ClientChannelGroupChanged);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			return $"Assigned channel group {cgid} to [b]{dbid}[/b]";
		}

		[Command("ce kick", "Syntax: !ce kick <client id>")]
		public string CommandKickClientFromChannel(ClientIdT clid, string reason = null)
		{
			var result = TS3FullClient.KickClient(new ClientIdT[]{ clid }, ReasonIdentifier.Channel, reason);
			if (!result.Ok) return $"{PluginInfo.Name}: [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			return $"Kicked client [b]{clid}[/b] from his channel for {reason}.";
		}

		[Command("ce codec", "Syntax: !ce codec <codec (0-5)>")]
		public string CommandEditChannelCodec(Codec codec)
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() { new CommandParameter("cid", ownChannelId),
				new CommandParameter("channel_codec", (int) codec)
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			else { return $"Channel Codec set to: [b]{codec.ToString()}[/b]"; }
		}

		[Command("ce codecquality", "Syntax: !ce codecquality <quality (1-10)>")]
		public string CommandEditChannelCodecQuality(int quality)
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() { new CommandParameter("cid", ownChannelId),
				new CommandParameter("channel_codec_quality", quality)
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			else { return $"Channel Codec Quality set to: [b]{quality}[/b]"; }
		}

		[Command("ce codeclatency", "Syntax: !ce codeclatency <delay (1-10)>")]
		public string CommandEditChannelCodecLatency(int latency)
		{
			ChannelIdT ownChannelId = TS3FullClient.WhoAmI().Value.ChannelId;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() { new CommandParameter("cid", ownChannelId),
				new CommandParameter("channel_codec_latency_factor", latency)
			});
			var result = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!result.Ok) return $"{PluginInfo.Name}: {commandEdit.ToString()} = [color=red]{result.Error.Message} ({result.Error.ExtraMessage})";
			else { return $"Channel Codec Latency Factor set to: [b]{latency}[/b]"; }
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientUpdated -= OnEachClientUpdated;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
