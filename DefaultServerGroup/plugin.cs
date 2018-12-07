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

namespace DefaultServerGroup {
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
	public class DefaultServerGroup : IBotPlugin {
		private static readonly PluginInfo PluginInfo = new PluginInfo();
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");


		public TS3FullClient TS3FullClient { get; set; }

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
