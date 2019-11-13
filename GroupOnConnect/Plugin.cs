using System;
using System.Linq;
using TS3AudioBot.Plugins;
using TS3Client.Full;
using TS3Client.Messages;
using System.Collections.Generic;

namespace GroupOnConnect
{
	public class GroupOnConnect : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Ts3FullClient TS3FullClient { get; set; }

		private static readonly List<ulong> sgids = new List<ulong>() { 233, 203, 229 };
		private static readonly List<string> uids = new List<string>() {
			"e3dvocUFTE1UWIvtW8qzulnWErI=",	// blu
			"v5x09qguvFwL30pbDtTT/2xKeRU=",	// chazo
			"Vjk27xsQUqG0aOlomPxVZ+qcamg=",	// markus1
			"xxjnc14LmvTk+Lyrm8OOeo4tOqw="	// markus2
		};

		public void Initialize()
		{
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView; 
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnEachClientEnterView(object sender, ClientEnterView client)
		{
			if (!uids.Contains(client.Uid)) return;
			foreach (var sgid in sgids)
			{
				if (client.ServerGroups.Contains(sgid)) continue;
				TS3FullClient.ServerGroupAddClient(sgid, client.DatabaseId);
			}
			
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
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
