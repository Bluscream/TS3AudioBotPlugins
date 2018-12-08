#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot.ResourceFactories;
using TS3Client.Full;
using TS3Client.Messages;

namespace AutoPause
{
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name = "Auto Pause (Save traffic)";
		public static readonly string Description = "Lets you save traffic by pausing/stopping the bot while noone is connected/in the channel";
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
	public enum Mode {
		Pause = 0,
		Stop,
		Silent
	}

	public class AutoPause : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot ConfBot { get; set; }
		public ConfRoot ConfRoot { get; set; }
		public BotManager BotManager { get; set; }
		public PlayManager BotPlayer { get; set; }
		public IPlayerConnection PlayerConnection { get; set; }
		//public IVoiceTarget targetManager { get; set; }
		//public ConfHistory confHistory { get; set; }
		private static FileIniDataParser ConfigParser;
		private static string PluginConfigFile;
		private static IniData PluginConfig;
		private const string ConfigSection = "General";

		private static ulong ownChannelID;
		private static List<ulong> ownChannelClients;

		private static Mode Mode;
		private static bool oldStatus = false;
		private static float oldVolume = 15;
		private static (float, InvokerData, PlayResource, MetaData) backup;

		public void Initialize()
		{
			PluginConfigFile = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.ini");
			ConfigParser = new FileIniDataParser();
			if (!File.Exists(PluginConfigFile))
			{
				PluginConfig = new IniData();
				PluginConfig[ConfigSection]["Stop or Pause or Silent"] = "Pause";
				// PluginConfig[ConfigSection]["Channel or Server"] = "Channel";
				// PluginConfig[section].GetKeyData("Channel or Server").Comments.Add();
				ConfigParser.WriteFile(PluginConfigFile, PluginConfig);
				Log.Warn("Config for plugin {} created, please modify it and reload!", PluginInfo.Name);
				return;
			}
			else { PluginConfig = ConfigParser.ReadFile(PluginConfigFile); }
			var mode = PluginConfig[ConfigSection]["Stop or Pause or Silent"];
			switch (mode.ToLower())
			{
				case "pause": Mode = Mode.Pause; break;
				case "stop": Mode = Mode.Stop; break;
				case "silent": Mode = Mode.Silent; break;
				default: break;
			}
			ownChannelClients = new List<ulong>();
			updateOwnChannel();
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientMoved += OnEachClientMoved;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void updateOwnChannel(ulong channelID = 0) {
			if (channelID < 1) channelID = TS3FullClient.WhoAmI().Value.ChannelId;
			ownChannelID = channelID;
			ownChannelClients.Clear();
			var clientlist = TS3FullClient.ClientList();
			if (!clientlist.Ok) { throw new Exception("Could not get Clientlist {}"); }
			foreach (var client in clientlist.Value)
			{
				if (client.ChannelId == channelID) {
					if (client.ClientId == TS3FullClient.ClientId) continue;
					ownChannelClients.Add(client.ClientId);
					}
			}
		}

		private void checkOwnChannel(bool pause) {
			if (ownChannelClients.Count < 1)
			{
				updatePlayingStatus(true);
			} else {
				updatePlayingStatus(false);
			}
		}

		private void updatePlayingStatus(bool pause)
		{
			var silent = PlayerConnection.Volume == 0;
			var paused = PlayerConnection.Paused;
			// if (pause && (silent || paused)) return;
			// if (!pause && (!silent || !paused)) return;
			switch (Mode)
			{
				case Mode.Pause:
					if (pause) {
						oldStatus = paused;
						PlayerConnection.Paused = true;
					} else {
						PlayerConnection.Paused = false;  // oldStatus
					}
					break;
				case Mode.Stop:
					if (pause) {
						backup.Item2 = BotPlayer.CurrentPlayData.Invoker;
						backup.Item3 = BotPlayer.CurrentPlayData.PlayResource;
						backup.Item4 = BotPlayer.CurrentPlayData.MetaData;
						BotPlayer.Stop();
					} else {
						BotPlayer.Play(backup.Item2, backup.Item3.PlayUri, meta: backup.Item4);
						Log.Warn("Started {}", backup.Item3.PlayUri);
					}
					break;
				case Mode.Silent:
					if (pause) {
						backup.Item1 = PlayerConnection.Volume;
						Log.Warn("BACKED UP {}", backup.Item1);
						PlayerConnection.Volume = 0;
					} else {
						var vol = BotPlayer.CurrentPlayData.MetaData.Volume;
						PlayerConnection.Volume = (vol is null) ? backup.Item1 : (float)vol;
						Log.Warn("RESTORED {}", backup.Item1);
					}
					break;
				default: break;
			}
			Log.Warn("{}: updatePlayingStatus: {} ({}, {})", PluginInfo.Name, pause, paused, silent);
		}

		private void OnEachClientMoved(object sender, ClientMoved e)
		{
			if (e.ClientId == TS3FullClient.ClientId) {
				updateOwnChannel(e.TargetChannelId); return; }
			var hasClient = ownChannelClients.Contains(e.ClientId);
			if (e.TargetChannelId == ownChannelID) {
				if (!hasClient) ownChannelClients.Add(e.ClientId);
				checkOwnChannel(false);
			} else if (hasClient) {
				ownChannelClients.Remove(e.ClientId);
				checkOwnChannel(true);
			}
			Log.ConditionalDebug("{}: OnEachClientMoved: {} {}", PluginInfo.Name, e.ClientId, e.TargetChannelId);

		}
		private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			if (e.ClientId == TS3FullClient.ClientId) return;
			if (e.TargetChannelId == ownChannelID) ownChannelClients.Add(e.ClientId);
			checkOwnChannel(true);
			Log.ConditionalDebug("{}: OnEachClientEnterView: {} {}", PluginInfo.Name, e.ClientId, e.TargetChannelId);
		}
		private void OnEachClientLeftView(object sender, ClientLeftView e)
		{
			if (e.ClientId == TS3FullClient.ClientId) return;
			if (e.SourceChannelId == ownChannelID) ownChannelClients.Remove(e.ClientId);
			checkOwnChannel(false);
			Log.ConditionalDebug("{}: OnEachClientLeftView: {} {}", PluginInfo.Name, e.ClientId, e.TargetChannelId);
		}

		[Command("autopause")]
		public string CommandInfo()
		{
			return string.Join(", ", ownChannelClients);
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientMoved -= OnEachClientMoved;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView -= OnEachClientLeftView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
