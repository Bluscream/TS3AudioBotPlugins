using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3Client.Full;
using TS3Client.Audio;
using TS3Client;
using TS3AudioBot.History;
using TS3AudioBot.Helper;
using Newtonsoft.Json;
using TS3Client.Commands;

using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using ClientUidT = System.String;
using TS3Client.Messages;
using System.Text;
using TS3AudioBot.Sessions;

namespace NoDirectConnect
{
	public class PluginInfo
	{
		public static readonly string ShortName = typeof(PluginInfo).Namespace;
		public static readonly string Name = string.IsNullOrEmpty(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name) ? ShortName : System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
		public static string Description = "";
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

	public class NoDirectConnect : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfRoot ConfRoot { get; set; }

		private bool Initialized = false;
		private static Dictionary<ClientIdT, ClientUidT> clientCache;
		private static Dictionary<ClientUidT,DateTime> timeoutCache;
		private static ChannelIdT defaultChannelID = 0;
		private static string KickMessage = "Nutze die Eingangshalle!";
		private static TimeSpan maxTimeOutPassed = TimeSpan.FromMinutes(2);

		public void Initialize()
		{
			clientCache = new Dictionary<ClientIdT, ClientUidT>();
			timeoutCache = new Dictionary<ClientUidT, DateTime>();
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			TS3FullClient.OnEachChannelList += OnEachChannelList;
			TS3FullClient.OnEachChannelListFinished += OnEachChannelListFinished;
			GetDefaultChannel();
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void GetDefaultChannel()
		{
			var channels = TS3FullClient.Send<ChannelData>("channellist");
			if (channels.Ok)
			{
				foreach (var channel in channels.Value)
				{
					if (channel.IsDefault) { defaultChannelID = channel.Id; return; }
				}
				Log.Error("No default channel found!");
			} else { Log.Error("Unable to get channel list: {}", channels.Error.Message); }
		}

		private void OnEachChannelList(object sender, ChannelList channel)
		{
			if (!channel.IsDefault) return;
			defaultChannelID = channel.ChannelId;
		}

		private void OnEachChannelListFinished(object sender, ChannelListFinished e)
		{
			if (Initialized) return;
			Initialized = true;
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			if (!clientCache.ContainsKey(client.ClientId))
				clientCache.Add(client.ClientId, client.Uid);
			if (!Initialized) return;
			if (client.SourceChannelId > 0) return;
			if (defaultChannelID < 1) return;
			if (client.TargetChannelId == defaultChannelID) return;
			if (timeoutCache.ContainsKey(client.Uid))
			{
				var timeout = timeoutCache[client.Uid];
				var timeoutvalid = (timeout - DateTime.Now) < maxTimeOutPassed;
				Log.Debug("Found matching {} timeout for client {} ({}): {} ({})", (timeoutvalid?"valid":"invalid"), client.Name, client.Uid, timeout);
				timeoutCache.Remove(client.Uid);
				if (timeoutvalid) return;
			}
			Log.Debug("Client {} ({}) tried to join into channel {} ({})", client.Name, client.Uid, client.TargetChannelId, defaultChannelID);
			TS3FullClient.KickClientFromChannel(client.ClientId, KickMessage);
		}

		private void OnEachClientLeftView(object sender, ClientLeftView client)
		{
			if (!Initialized) return;
			if (clientCache.ContainsKey(client.ClientId)) {
				if (client.Reason == Reason.Timeout) {
					var uid = clientCache[client.ClientId];
					if (timeoutCache.ContainsKey(uid))
					Log.Debug("Client {} timed out, adding him to timeout cache.", uid);
					timeoutCache.Add(uid, DateTime.Now);
				}
				clientCache.Remove(client.ClientId);
			}
		}

		[Command("nodirectconnect", "")]
		public string CommandInfo()
		{
			var sb = new StringBuilder(PluginInfo.Name);
			sb.AppendLine();
			sb.AppendLine($"Initialized: {Initialized}");
			sb.AppendLine($"defaultChannelID: {defaultChannelID}");
			sb.AppendLine($"KickMessage: {KickMessage}");
			sb.AppendLine($"clients ({clientCache.Count}): {string.Join(",", clientCache)}");
			sb.AppendLine($"timeouts ({timeoutCache.Count}): {string.Join(",", timeoutCache)}");
			// GetDefaultChannel();
			return sb.ToString();
		}

		[Command("nodirectconnect clear", "")]
		public string CommandClear()
		{
			var cleared = timeoutCache.Count;
			timeoutCache.Clear();
			return $"Cleared timeout cache with {cleared} timeouts.";
		}

		public void Dispose()
		{
			clientCache.Clear();timeoutCache.Clear();
			TS3FullClient.OnEachChannelListFinished -= OnEachChannelListFinished;
			TS3FullClient.OnEachChannelList -= OnEachChannelList;
			TS3FullClient.OnEachClientLeftView -= OnEachClientLeftView;
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
