using System;
using System.Linq;
using NowPlaying.Properties;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.Commands;
using TS3Client;
using TS3Client.Commands;
using TS3Client.Full;

namespace NowPlaying {

    public class PluginInfo
    {
        public static readonly string Name = typeof(PluginInfo).Namespace;
        public const string Description = "Allows you to set several locations where the current track is being announced.\n" +
										  "Edit the file NowPlaying.dll.config to your needs.\n"+
										  "Possible replacements: {title}, {invoker}, {invokeruid}, {volume}, {resourceid}, {uniqueid}, {playuri}";
        public const string Url = "";
        public const string Author = "Bluscream <admin@timo.de.vc>";
        public const int Version = 1;
    }

    public class NowPlaying : ITabPlugin {

        public PluginInfo pluginInfo = new PluginInfo();
        private MainBot bot;
        private Ts3FullClient lib;

        public void PluginLog(Log.Level logLevel, string Message) { Log.Write(logLevel, PluginInfo.Name + ": " + Message); }

        public void Initialize(MainBot mainBot) {
			//bot = mainBot.Bots.GetBot(0);
			bot = mainBot;
            lib = mainBot.QueryConnection.GetLowLibrary<Ts3FullClient>();
            bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
        }

        public string ParseNowPlayingString(string input, PlayInfoEventArgs e)
        {
            return input
                .Replace("{title}", e.ResourceData.ResourceTitle)
                .Replace("{invoker}", "[URL=client://" + e.Invoker.ClientId + "/" + e.Invoker.ClientUid + "]" + e.Invoker.NickName + "[/URL]")
                .Replace("{invokeruid}", e.Invoker.ClientUid)
                .Replace("{volume}", e.MetaData.Volume.ToString())
                .Replace("{resourceid}", e.ResourceData.ResourceId)
                .Replace("{uniqueid}", e.ResourceData.UniqueId)
                .Replace("{playuri}", e.PlayResource.PlayUri)
                .Replace("{length}", 2.0m.ToString(bot.PlayerConnection.Length.ToString()));
                // TODO: Length, etc
        }

        private void PlayManager_AfterResourceStarted(object sender, PlayInfoEventArgs e) {
            if (!Settings.Default.Enabled) { return; }
            PluginLog(Log.Level.Debug, "Track changed. Applying now playing values");
            if (!string.IsNullOrWhiteSpace(Settings.Default.Description))
            {
	            try {
		            bot.QueryConnection.ChangeDescription(ParseNowPlayingString(Settings.Default.Description, e));
	            } catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to change Description: " + ex.Message); }
            }
            if (!string.IsNullOrWhiteSpace(Settings.Default.ServerChat))
            {
	            try {
                bot.QueryConnection.SendServerMessage(ParseNowPlayingString(Settings.Default.ServerChat, e));
	            } catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to send Server Message: " + ex.Message); }
			}
            if (!string.IsNullOrWhiteSpace(Settings.Default.ChannelChat)) {
				try {
					bot.QueryConnection.SendChannelMessage(ParseNowPlayingString(Settings.Default.ChannelChat, e));
	        } catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to send Channel Message: " + ex.Message); }
		}
            if (!string.IsNullOrWhiteSpace(Settings.Default.PrivateChat)) {
				try {
				    var clientbuffer = lib.ClientList(ClientListOptions.uid).ToList();
				    foreach (var client in clientbuffer) {
				        foreach (var uid in Settings.Default.PrivateChatUIDs) {
				            if (client.Uid == uid) {
				                bot.QueryConnection.SendMessage(ParseNowPlayingString(Settings.Default.PrivateChat, e), client.ClientId);
				            }
				        }
                    }
	            } catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to send private Message: " + ex.Message); }
            }
            if (!string.IsNullOrWhiteSpace(Settings.Default.NickName)) {
				try {
					bot.QueryConnection.ChangeName(ParseNowPlayingString(Settings.Default.NickName,e));
	            } catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to change nickname: " + ex.Message); }
		}
            if (!string.IsNullOrWhiteSpace(Settings.Default.PluginCommand)) {
				try {
					lib.Send("plugincmd", new CommandParameter("name", "TS3AudioBot"),
                        new CommandParameter("targetmode", 0),
                        new CommandParameter("data", ParseNowPlayingString(Settings.Default.PluginCommand, e)));
				} catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to send Plugin Command: " + ex.Message); }
            }
            if (!string.IsNullOrWhiteSpace(Settings.Default.MetaData)) {
				try {
					/* TODO: Append Meta Data
					var clid = lib.ClientId;
					var ownClient = lib.Send<ClientData>("clientinfo", new CommandParameter("clid", clid)).FirstOrDefault();
					ownClient.*/
					lib.Send("clientupdate", new CommandParameter("client_meta_data", ParseNowPlayingString(Settings.Default.MetaData, e)));
				} catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to set Meta Data: " + ex.Message); }
			}
			var ownChannelId = lib.WhoAmI().ChannelId;
			if (!string.IsNullOrWhiteSpace(Settings.Default.ChannelName)) {
				try {
					lib.Send("channeledit", new CommandParameter("cid", ownChannelId),
			        new CommandParameter("channel_name", ParseNowPlayingString(Settings.Default.ChannelName, e)));
				} catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to set channel name: " + ex.Message); }
			}
			if (!string.IsNullOrWhiteSpace(Settings.Default.ChannelTopic)) {
				try {
					lib.Send("channeledit", new CommandParameter("cid", ownChannelId),
					new CommandParameter("channel_topic", ParseNowPlayingString(Settings.Default.ChannelTopic, e)));
				} catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to set channel topic: " + ex.Message); }
			}
	        if (!string.IsNullOrWhiteSpace(Settings.Default.ChannelDescription)) {
		        try {
					lib.Send("channeledit", new CommandParameter("cid", ownChannelId),
			        new CommandParameter("channel_description", ParseNowPlayingString(Settings.Default.ChannelDescription, e)));
		        } catch (Exception ex) { PluginLog(Log.Level.Warning, "Exeption thrown while trying to set channel description: " + ex.Message); }
	        }
		}

        public void Dispose() {
            bot.PlayManager.AfterResourceStarted -= PlayManager_AfterResourceStarted;
            PluginLog(Log.Level.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
        }

        [Command("nowplaying toggle", PluginInfo.Description)]
        public string CommandToggleNowPlaying()
        {
            Settings.Default.Enabled = !Settings.Default.Enabled;
            Settings.Default.Save();
            return PluginInfo.Name + " is now " + Settings.Default.Enabled;
		}

	    [Command("nowplaying set", "Changes a setting of this plugin")]
	    public string CommandNowPlayingSetSetting(string setting, string value)
	    {
		    Settings.Default[setting] = value;
			Settings.Default.Save();
		    return PluginInfo.Name + ": Set " + setting +" to " + value;
		}
	    [Command("nowplaying get", "Retrieves a setting of this plugin")]
	    public string CommandNowPlayingGetSetting(string setting) {
		    return PluginInfo.Name + ": " + setting + " = " + Settings.Default[setting];
		}
	    [Command("nowplaying list", "Lists all settings of this plugin")]
	    public string CommandNowPlayingListSetting() {
		    return PluginInfo.Name + ":\nSettings: Description, MetaData, ChannelChat, PrivateChat, ServerChat, NickName, PluginCommand, ChannelName, ChannelTopic, ChannelDescription\n"+
									 "Replacements: {title}, {invoker}, {invokeruid}, {volume}, {resourceid}, {uniqueid}, {playuri}";
	    }
	}
}
