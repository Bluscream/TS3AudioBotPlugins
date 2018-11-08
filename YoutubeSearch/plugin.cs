using System;
//using System.Net;
using System.Collections.Generic;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;

//YTAPIKEY: AIzaSyBHTLNDGw7yj_gnw7_e1_ztZ5nJigP9fEg

namespace YoutubeSearch
{
	public class PluginInfo
	{
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "";
		public const string Url = "https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/YoutubeSearch";
		public const string Author = "Bluscream";
		public const int Version = 1;
	}
	public class YoutubeSearch : IBotPlugin
	{
		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		//private static WebClient wc = new WebClient();

		public Ts3FullClient Ts3Client { get; set; }

		public void Initialize()
		{
			CloudRail.AppKey = "5be4c85b21b62e5228d2e70b";

			YouTube service = new YouTube(
				new LocalReceiver(8082),
				"453686325027-v073p5l12gauif6tbno0p9mb9cdcv55p.apps.googleusercontent.com",
				"UzzEfLmly6KDaOfexSA0sXp6",
				"http://localhost:8082/auth",
				"someState"
			);

			List<VideoMetaData> result = service.SearchVideos(
				"CloudRail",
				50,
				42
			);
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
		}

		[Command("yt", PluginInfo.Description)]
		public string CommandSearchYoutube(string query)
		{

			return "";
		}

		public void Dispose()
		{
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
