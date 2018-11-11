using System;
using System.Linq;
using TS3AudioBot.Plugins;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3AudioBot.Helper;
using TS3Client.Commands;
using System.Collections.Generic;
//using ServerGroupIdT = System.UInt64;

namespace DefaultServerGroup
{
	public class PluginInfo
	{
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "";
		public const string Url = "https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/DefaultServerGroup";
		public const string Author = "Bluscream <admin@timo.de.vc>";
		public const int Version = 1;
	}
	public class AutoChannelCreate : IBotPlugin
	{
		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public Ts3FullClient TS3FullClient { get; set; }

		public void Initialize()
		{
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			TS3FullClient.ServerGroupAddClient(9, e.DatabaseId);
		}

		[TS3AudioBot.CommandSystem.Command("sgadd")]
		public string CommandServerGroupClientAdd(ulong sgid, ulong cldbid) {
			var err = TS3FullClient.ServerGroupAddClient(sgid, cldbid);
			if (!err.Ok) {
				return $"{PluginInfo.Name}: {err.Error.Message} ({err.Error.ExtraMessage})";
			}
			return $"Added group \"{sgid}\" to client \"{cldbid}\"";
		}

		[TS3AudioBot.CommandSystem.Command("sgrem")]
		public string CommandServerGroupClientDelete(ulong sgid, ulong cldbid) {
			var err = TS3FullClient.ServerGroupDelClient(sgid, cldbid);
			if (!err.Ok) {
				return $"{PluginInfo.Name}: {err.Error.Message} ({err.Error.ExtraMessage})";
			}
			return $"Removed group \"{sgid}\" from client \"{cldbid}\"";
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
