using System;
using System.Linq;
using TS3AudioBot.Plugins;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3AudioBot.Helper;
using TS3Client.Commands;
using System.Collections.Generic;
using TS3AudioBot;

using Duration = System.TimeSpan;
using DurationSeconds = System.TimeSpan;
using DurationMilliseconds = System.TimeSpan;
using SocketAddr = System.Net.IPAddress;

using Uid = System.String;
using ClientDbId = System.UInt64;
using ClientId = System.UInt16;
using ChannelId = System.UInt64;
using ServerGroupId = System.UInt64;
using ChannelGroupId = System.UInt64;
using IconHash = System.Int32;
using ConnectionId = System.UInt32;

namespace BackToDefaultChannel
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
	public class BackToDefaultChannel : IBotPlugin
	{
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public Ts3Client TS3Client { get; set; }
		public TS3FullClient TS3FullClient { get; set; }

		Dictionary<ClientId, ChannelId> cache = new Dictionary<ClientId, ChannelId>();

		public void Initialize()
		{
			TS3FullClient.OnEachClientMoved += OnEachClientMoved;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		private void OnEachClientMoved(object sender, ClientMoved e)
		{
			if (!cache.ContainsKey(e.ClientId))
			{
				cache.Add(e.ClientId, e.TargetChannelId);
			}
			if (cache[e.ClientId] == e.TargetChannelId) return;
			cache[e.ClientId] = e.TargetChannelId;
			var myChan = cache[TS3FullClient.ClientId]; // TS3FullClient.WhoAmI().Value.ChannelId; TS3
			

		}

		private ChannelInfoRequest GetChannelInfo(ChannelId cid)
		{
			var commandChannelInfo = new Ts3Command("channelinfo", new List<ICommandPart>() { new CommandParameter("cid", cid) });
			var result = TS3FullClient.SendNotifyCommand(commandChannelInfo, NotificationType.ChannelInfoRequest);
			if (!result.Ok) {
				PluginLog(LogLevel.Debug, $"{PluginInfo.Name}: Could not get Channel Info! ({result.Error.Message})"); return new ChannelInfoRequest();
			}
			var res = result.Value.Notifications.Cast<ChannelInfoRequest>().FirstOrDefault();
			return res;
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientMoved -= OnEachClientMoved;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
