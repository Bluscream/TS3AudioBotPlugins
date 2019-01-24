using System;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using System.Collections.Generic;
using ChannelIdT = System.UInt64;

namespace TeaSpeak
{
	public class TeaSpeak : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Ts3FullClient TS3FullClient { get; set; }

		private ulong neededTP = 2000000;
		private List<ChannelIdT> channels;
		private List<ChannelIdT> toEdit;

		public void Initialize()
		{
			channels = new List<ChannelIdT>() {
				205,	// ┗ Leitung | Warteraum
				229,	// ┗ Technik | Warteraum
				17,		// ┗ Admin | Warteraum
				219,	// ┏ Bewerbung | Warteraum
				227,	// ┗ Bewerbung | Beendet
				37,		// ┏ Support | Warteraum
				44,		// ┗ Support | Beendet
				50,		// [cspacer]◆ＶＥＲＩＦＩＺＩＥＲＵＮＧ◆
				54,		// [cspacer]◆ＥＩＮＧＡＮＧＳＨＡＬＬＥ◆
				61,		// ┏» Radio |  HappyFM
				191,	// ┣» Radio | Radio25
				224,	// ┣» Radio | RandyFM
				203,	// ┣» Radio | ZONERADIO
				180,	// ┣» Radio | NexusFM
				63,		// ┗ » Radio | I ♥ Radio
				225,	// ┗ » Radio | Spotify
				68,		// ┏ AFK | Kurz
				69,		// ┣ AFK | AutoMove
				70		// ┗ AFK | Lang
			};
			toEdit = new List<ChannelIdT>() { };
			TS3FullClient.OnEachChannelList += OnEachChannelList;
			TS3FullClient.OnEachChannelListFinished += OnEachChannelListFinished;
			TS3FullClient.OnEachChannelEdited += OnEachChannelEdited;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachChannelEdited(object sender, ChannelEdited channel)
		{
			if (channel.InvokerId == TS3FullClient.ClientId) return; // TODO: Check?
			if (!channels.Contains(channel.ChannelId)) return;
			if (channel.NeededTalkPower == (int)neededTP) return;
			setChannelTP(channel.ChannelId);
		}

		private void OnEachChannelListFinished(object sender, ChannelListFinished e)
		{
			if (toEdit.Count < 1) return; 
			foreach (var cid in toEdit)
			{
				setChannelTP(cid);
			}
			toEdit.Clear();
		}

		private void OnEachChannelList(object sender, ChannelList channel)
		{
			if (!channels.Contains(channel.ChannelId)) return;
			if (channel.NeededTalkPower == (int)neededTP) return;
			toEdit.Add(channel.ChannelId);
		}

		private void setChannelTP(ChannelIdT cid)
		{
			if (!channels.Contains(cid)) return;
			var commandEdit = new Ts3Command("channeledit", new List<ICommandPart>() {
						new CommandParameter("cid", cid),
						new CommandParameter("channel_needed_talk_power", neededTP)
				});
			var editResult = TS3FullClient.SendNotifyCommand(commandEdit, NotificationType.ChannelEdited);
			if (!editResult.Ok) { Log.Warn($"{PluginInfo.Name}: Could not edit channel {cid}! ({editResult.Error.Message})"); return; }
		}

		private ulong FixChannelsTP()
		{
			var Channels = TS3FullClient.Send<ChannelData>("channellist");
			if (!Channels.Ok) return 0;
			ulong edited = 0;
			foreach (var channel in Channels.Value) {
				if (!channels.Contains(channel.Id)) continue;
				setChannelTP(channel.Id);
			}
			return edited;
		}

		[Command("teaspeakfix tp")]
		public string CommandFixTP()
		{
			var edited = FixChannelsTP();
			return $"Edited {edited} channels.";
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
