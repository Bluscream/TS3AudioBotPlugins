using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TS3AudioBot;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot.Helper;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client.Commands;
using TS3AudioBot.CommandSystem;

namespace AutoFollow
{
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name = "Auto Follow";
		public static readonly string Description = "Follow users into other channels.";
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
	public class AutoFollow : IBotPlugin
    {
        private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

        // public Bot bot { get; set; }
		public TS3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }

		//private static Dictionary<int, string> UidCache;
		private static List<string> following;

        public void Initialize()
		{
			// UidCache = new Dictionary<int, string>();
			following = new List<string>();
			//TS3FullClient.OnEachClientEnterView += OnEachClientEnterView;
			//TS3FullClient.OnEachClientLeftView += OnEachClientLeftView;
			TS3FullClient.OnEachClientMoved += OnEachClientMoved;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		/*private void OnEachClientLeftView(object sender, ClientLeftView e)
		{
			if (!UidCache.ContainsKey(e.ClientId)) return;
			UidCache.Remove(e.ClientId);
		}

		private void OnEachClientEnterView(object sender, ClientEnterView e)
		{
			UidCache.Add(e.ClientId, e.Uid);
		}*/

		private void OnEachClientMoved(object sender, ClientMoved client) {
			var cached = TS3Client.GetCachedClientById(client.ClientId).Value;
			if (!following.Contains(cached.Uid)) return;
			Log.Debug("Client-to-follow \"{}\" { changed channels, following into #{}", cached.Name, client.TargetChannelId);
			TS3Client.MoveTo(client.TargetChannelId);
			return;
		}

        public void Dispose() {
			TS3FullClient.OnEachClientMoved -= OnEachClientMoved;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}

        [Command("follow", "")]
        public string CommandToggleAutoFollow(Ts3Client ts3Client, string name) {
			var client = ts3Client.GetClientByName(name).UnwrapThrow();
			if (string.IsNullOrEmpty(client.Uid)) return "Could not find client!";
			if (following.Contains(client.Uid))
            {
                following.Remove(client.Uid);
                return $"{PluginInfo.Name}: [color=gray]No longer following[/color] client {ClientURL(client.ClientId, client.Uid, client.Name)}";
            } else {
                following.Add(client.Uid);
				return $"{PluginInfo.Name}: [color=green]Now following[/color] client {ClientURL(client.ClientId, client.Uid, client.Name)}";
			}
		}

		[Command("follow clear", "")]
		public string CommandAutoFollowClear()
		{
			following.Clear();
			return $"{PluginInfo.Name}: [color=red]No longer following anyone here!";
		}

		public static string ClientURL(ushort clientID, string uid = "unknown", string nickname = "Unknown User")
		{
			var sb = new StringBuilder("[URL=client://");
			sb.Append(clientID);
			sb.Append("/");
			sb.Append(uid);
			//sb.Append("~");
			sb.Append("]\"");
			sb.Append(nickname);
			sb.Append("\"[/URL]");
			return sb.ToString();
		}
	}
}
