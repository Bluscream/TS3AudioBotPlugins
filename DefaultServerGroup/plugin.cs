using System;
using System.Linq;
using TS3AudioBot.Plugins;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using ServerGroupIdT = System.UInt64;

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
		public ServerGroupIdT[] defaultGroups = {9};

		public void Initialize()
		{
			/*var parser = new FileIniDataParser();
			IniData data = parser.ReadFile($"{PluginInfo.Name}.ini");
			defaultGroups = data["groups"]["defaultgroups"].Split(',').Select(ServerGroupIdT.Parse).ToArray();*/
			TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			bool hasAnyDefaultGroup = e.ServerGroups.Intersect(defaultGroups).Any();
			if (!hasAnyDefaultGroup) return;
			foreach (var group in defaultGroups)
			{
				TS3FullClient.ServerGroupAddClient(group, e.DatabaseId);
			}
		}

		public void Dispose()
		{
			TS3FullClient.OnEachClientEnterView -= OnEachClientEnterView;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
