using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TS3AudioBot.Plugins;
using TS3Client.Full;
using TS3Client.Messages;

namespace VersionDetector
{
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name;
		public static readonly string Description;
		public static readonly string Url;
		public static readonly string Author = "Splamy";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;
		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}
	public class VersionDetector : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		private static WebClient wc = new WebClient();

		const string versionFile = "versions.csv";

		private HashSet<string> versions = new HashSet<string>();

		public Ts3FullClient Ts3Client { get; set; }

		public TS3AudioBot.Bot Bot { get; set; }

		public VersionDetector()
		{
			if (File.Exists(versionFile))
				versions.UnionWith(File.ReadAllLines(versionFile));
			else
				File.Create(versionFile).Dispose();
		}

		public void Initialize()
		{
			Ts3Client.OnEachClientEnterView += Ts3Client_OnEachClientEnterView;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void Ts3Client_OnEachClientEnterView(object sender, ClientEnterView e)
		{
			try
			{
				var info = Ts3Client.ClientInfo(e.ClientId).Unwrap();

				var checkVersion = string.Format("{0},{1},{2}", info.ClientVersion, info.ClientPlatform, info.ClientVersionSign);

				if (!versions.Contains(checkVersion))
				{
					//Ts3Client.SendPrivateMessage("Thanks for submitting your soul!", e.ClientId);
					try
					{
						wc.UploadData(string.Format("https://splamy.de/api/teamspeak/version/{0}/{1}?sign={2}",
							info.ClientVersion, info.ClientPlatform, Uri.EscapeDataString(info.ClientVersionSign)), "POST", Array.Empty<byte>());
					}
					catch { return; }

					// checkVersion += $"{}";

					versions.Add(checkVersion);
					File.AppendAllText(versionFile, checkVersion + "\n");
					Log.Debug("{0}: Got Version {1} from client {2} ({3}) [{4}] ip:{5}", Bot.Name, checkVersion, info.Name, info.Uid, info.MyTeamSpeakId, info.Ip);
				}
			}
			catch (InvalidOperationException)
			{
			}
		}

		public void Dispose()
		{
			Ts3Client.OnEachClientEnterView -= Ts3Client_OnEachClientEnterView;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
