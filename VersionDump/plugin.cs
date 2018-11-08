using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TS3AudioBot.Plugins;
using TS3Client;
using TS3Client.Full;
using TS3Client.Messages;

namespace VersionDetector
{
	public class PluginInfo
	{
		public static readonly string Name = typeof(PluginInfo).Namespace;
		public const string Description = "";
		public const string Url = "";
		public const string Author = "Splamy";
		public const int Version = 1;
	}
	public class VersionDetector : IBotPlugin
	{
		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		private static WebClient wc = new WebClient();

		const string versionFile = "versions.csv";

		private HashSet<string> versions = new HashSet<string>();

		public Ts3FullClient Ts3Client { get; set; }

		public VersionDetector()
		{
			if (File.Exists(versionFile))
				versions.IntersectWith(File.ReadAllLines(versionFile));
			else
				File.Create(versionFile).Dispose();
		}

		public void Initialize()
		{
			Ts3Client.OnEachClientEnterView += Ts3Client_OnEachClientEnterView;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");
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

					versions.Add(checkVersion);
					File.AppendAllText(versionFile, checkVersion + "\n");
				}
			}
			catch (InvalidOperationException)
			{
			}
		}

		public void Dispose()
		{
			Ts3Client.OnEachClientEnterView -= Ts3Client_OnEachClientEnterView;
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
		}
	}
}
