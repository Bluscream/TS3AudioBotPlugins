using System;
using System.Linq;
using TS3AudioBot;
using TS3AudioBot.Plugins;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Config;
using TS3Client;
using TS3Client.Commands;
using TS3Client.Full;

namespace NowPlaying {
	public class PluginInfo {
		public static readonly string ShortName = typeof(PluginInfo).Namespace;
		public static readonly string Name = string.IsNullOrEmpty(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name) ? ShortName : System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
		public static string Description = "Allows you to set several locations where the current track is being announced.\n" +
										"Edit the file NowPlaying.dll.config to your needs.\n" +
										"Possible replacements: {title}, {invoker}, {invokeruid}, {volume}, {resourceid}, {uniqueid}, {playuri}";
		public static string Url = $"https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/{ShortName}";
		public static string Author = "Bluscream <admin@timo.de.vc>";
		public static readonly Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		public PluginInfo()
		{
			var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
			Description = versionInfo.FileDescription;
			Author = versionInfo.CompanyName;
		}
	}
	public class NowPlaying : IBotPlugin {
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Bot bot;
		public TS3FullClient lib;
		public Ts3Client TS3Client;
		public ConfTools ConfTools;

		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public void Initialize()
		{
			bot.PlayManager.AfterResourceStarted += PlayManager_AfterResourceStarted;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
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

		private void PlayManager_AfterResourceStarted(object sender, PlayInfoEventArgs e)
		{
			if (!Settings.Default.Enabled) { return; }
			PluginLog(LogLevel.Debug, "Track changed. Applying now playing values");
			if (!string.IsNullOrWhiteSpace(Settings.Default.Description))
			{
				try
				{
					TS3Client.ChangeDescription(ParseNowPlayingString(Settings.Default.Description, e));
				}
				catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to change Description: " + ex.Message); }
			}
			if (!string.IsNullOrWhiteSpace(Settings.Default.ServerChat))
			{
				try
				{
					TS3Client.SendServerMessage(ParseNowPlayingString(Settings.Default.ServerChat, e));
				}
				catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to send Server Message: " + ex.Message); }
			}
			if (!string.IsNullOrWhiteSpace(Settings.Default.ChannelChat))
			{
				try
				{
					TS3Client.SendChannelMessage(ParseNowPlayingString(Settings.Default.ChannelChat, e));
				}
				catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to send Channel Message: " + ex.Message); }
			}
			if (!string.IsNullOrWhiteSpace(Settings.Default.PrivateChat))
			{
				try
				{
					var clientbuffer = lib.ClientList(ClientListOptions.uid).Value.ToList();
					foreach (var client in clientbuffer)
					{
						foreach (var uid in Settings.Default.PrivateChatUIDs)
						{
							if (client.Uid == uid)
							{
								TS3Client.SendMessage(ParseNowPlayingString(Settings.Default.PrivateChat, e), client.ClientId);
							}
						}
					}
				}
				catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to send private Message: " + ex.Message); }
			}
			if (!string.IsNullOrWhiteSpace(Settings.Default.NickName))
			{
				try
				{
					TS3Client.ChangeName(ParseNowPlayingString(Settings.Default.NickName, e));
				}
				catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to change nickname: " + ex.Message); }
			}
			if (!string.IsNullOrWhiteSpace(Settings.Default.PluginCommand))
			{
				try
				{
					lib.Send("plugincmd", new CommandParameter("name", "TS3AudioBot"),
						new CommandParameter("targetmode", 0),
						new CommandParameter("data", ParseNowPlayingString(Settings.Default.PluginCommand, e)));
				}
				catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to send Plugin Command: " + ex.Message); }
			}
			if (!string.IsNullOrWhiteSpace(Settings.Default.MetaData))
			{
				try
				{
					/* TODO: Append Meta Data
					var clid = lib.ClientId;
					var ownClient = lib.Send<ClientData>("clientinfo", new CommandParameter("clid", clid)).FirstOrDefault();
					ownClient.*/
					lib.Send("clientupdate", new CommandParameter("client_meta_data", ParseNowPlayingString(Settings.Default.MetaData, e)));
				}
				catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to set Meta Data: " + ex.Message); }
			}
			bool ChannelName = !string.IsNullOrWhiteSpace(Settings.Default.ChannelName);
			bool ChannelTopic = !string.IsNullOrWhiteSpace(Settings.Default.ChannelTopic);
			bool ChannelDescription = !string.IsNullOrWhiteSpace(Settings.Default.ChannelDescription);
			if (ChannelName || ChannelTopic || ChannelDescription)
			{
				var ownChannelId = lib.WhoAmI().Value.ChannelId;
				if (ChannelName)
				{
					try
					{
						lib.Send("channeledit", new CommandParameter("cid", ownChannelId),
						new CommandParameter("channel_name", ParseNowPlayingString(Settings.Default.ChannelName, e)));
					}
					catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to set channel name: " + ex.Message); }
				}
				if (ChannelTopic)
				{
					try
					{
						lib.Send("channeledit", new CommandParameter("cid", ownChannelId),
						new CommandParameter("channel_topic", ParseNowPlayingString(Settings.Default.ChannelTopic, e)));
					}
					catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to set channel topic: " + ex.Message); }
				}
				if (ChannelDescription)
				{
					try
					{
						lib.Send("channeledit", new CommandParameter("cid", ownChannelId),
						new CommandParameter("channel_description", ParseNowPlayingString(Settings.Default.ChannelDescription, e)));
					}
					catch (Exception ex) { PluginLog(LogLevel.Warning, "Exeption thrown while trying to set channel description: " + ex.Message); }
				}
			}
		}

		public void Dispose()
		{
			bot.PlayManager.AfterResourceStarted -= PlayManager_AfterResourceStarted;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
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
			return PluginInfo.Name + ": Set " + setting + " to " + value;
		}
		[Command("nowplaying get", "Retrieves a setting of this plugin")]
		public string CommandNowPlayingGetSetting(string setting)
		{
			return PluginInfo.Name + ": " + setting + " = " + Settings.Default[setting];
		}
		[Command("nowplaying list", "Lists all settings of this plugin")]
		public string CommandNowPlayingListSetting()
		{
			return PluginInfo.Name + ":\nSettings: Description, MetaData, ChannelChat, PrivateChat, ServerChat, NickName, PluginCommand, ChannelName, ChannelTopic, ChannelDescription\n" +
									 "Replacements: {title}, {invoker}, {invokeruid}, {volume}, {resourceid}, {uniqueid}, {playuri}";
		}
	}
}
