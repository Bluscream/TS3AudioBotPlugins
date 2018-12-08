using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Helper;
using TS3AudioBot.Plugins;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3AudioBot.Config;

namespace DropSystems
{
	public class Utils
	{
	}
	public static class PluginInfo
	{
		public static readonly string ShortName;
		public static readonly string Name;
		public static readonly string Description;
		public static readonly string Url;
		public static readonly string Author = "Bluscream";
		public static readonly Version Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version;
		static PluginInfo()
		{
			ShortName = typeof(PluginInfo).Namespace;
			var name = System.Reflection.Assembly.GetCallingAssembly().GetName().Name;
			Name = string.IsNullOrEmpty(name) ? ShortName : name;
		}
	}
	public class DropSystems : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");

		public Ts3FullClient Ts3FullClient { get; set; }
		public Ts3Client Ts3Client { get; set; }
		public Bot Bot { get; set; }
		public TickWorker Timer { get; set; }
		public ConfRoot ConfRoot { get; set; }
		private const string suid = "uSggaC3Um04QqMB8vrrQJrAMz5Y=";
		/*private ClientData bot_dropverify = new ClientData() { Name = "» DropVerify", Uid = "1pvCr4o4ME05vJ/wnByqETm1rc4=" };
		private ClientData bot_dropverifymc = new ClientData() { Uid = "/ngHhrrnQl/JB8seCsgHkpk3xCY=" };
		private ClientData bot_dropradio = new ClientData() { Name = "DropRadio » Wartungen", Uid = "NW+PLhA/dhnxvIVQZGn8GKUGoKE=" };
		private ClientData bot_dropradio_whisper = new ClientData() { Name = "DropRadio » Whisper", Uid = "wJhkVvpecZvKl1EucawPBB5I4QM=" };*/
		private const string uid_bot_dropsystems = "9Xx3ciS1+gktsG6Z6MSGM6Z3974=";
		private const string uid_bot_dropranking = "/Evwwvnf2EepICM+M/bVBrmwIIs=";
		private const string uid_bot_dropcloud = "QuZDxb1ilQHEyo0nnrMLI6toAZ8=";
		private const string uid_bot_dropverify = "1pvCr4o4ME05vJ/wnByqETm1rc4=";
		private const string uid_bot_dropverifymc = "/ngHhrrnQl/JB8seCsgHkpk3xCY=";
		private const string uid_bot_dropradio = "NW+PLhA/dhnxvIVQZGn8GKUGoKE=";
		private const string uid_bot_dropradio_whisper = "wJhkVvpecZvKl1EucawPBB5I4QM=";
		private const string msg_verification_required = "Damit Sie unseren TeamSpeak umfangreich nutzen können, müssen Sie sich Verifizieren.";
		private const string msg_verification_success = "Sie wurden erfolgreich Verifieziert!";
		private const string msg_nextup_response = "RangSteigerung";
		private const string msg_nextup_newlvl = "Sie haben einen höheren Rang bekommen";
		private static Regex regex_nextup = new Regex(@"Level: (.*)\n\s*Tage: (\d+)\s*\nStunden: (\d+)\s*\nMinuten: (\d+)\s*\n");
		private const ulong cid_idle = 1585;
		private const ulong cid_create = 1597;

		private const string channel_name = "Kein Support";
		private const string channel_password = "blu";
		private const string channel_maxclients = "10";
		private const string channel_needed_tp = "6";
		private const string channel_topic_template = "";
		private string channel_description_file;

		private string[] channel_joinfor = {"e3dvocUFTE1UWIvtW8qzulnWErI=", "BnOoI1/YoLpF5jOBxQXxRYc+a28=" };

		private (string, TimeSpan) nextUp;
		/*
		 * [URL=client://24//Evwwvnf2EepICM+M/bVBrmwIIs=~%C2%BB%20DropRanking]» DropRanking[/URL]
		[B]Willkommen[\/B] » Verifizierung

		Damit Sie unseren TeamSpeak umfangreich nutzen können, müssen Sie sich Verifizieren.
		Hierfür gibt es bei uns zwei Möglichkeiten: Sie können entweder diesen Bot mit [B]!verify[\/B] anschreiben oder in Minecraft [B]\/verify[\/B] eingeben.
		Die beiden Variationen geben Ihnen zwar unterschiedliche Ränge, diese haben aber die gleichen Features.

		Wir wünschen Ihnen noch einen schönen Aufenthalt auf unseren Netzwerken!
		---
		[B]Verifizierung[\/B] » Sie wurden erfolgreich Verifieziert!
		---
		\n[B]RangSteigerung »[/B] [url=http://teamspeak.dropsystems.eu]LevelUp-Stastiken[/url]\r


		\nHey CONTACT_FRIEND, Sie haben einen höheren Rang bekommen und können sofort anfangen Ihre neuen Features des Levelup's zu entdecken,\r\nMit dem Befehl [B]!nextup[/B] sehen Sie Ihre aktuellen Statisken auf dem DropSystems Teamspeak!\r\nVielen Dank für Ihren Aufenthalt auf unserem Server und noch weiterhin viel Spaß beim Punkten.



		[B]AFKChecker[/B] » Sobald Sie für 10 Minuten inaktiv sind, werden Sie in den AFK-Channel verschoben! Nachdem Sie dann für längere Zeit inaktiv sind, werden Sie vom Server gekickt!"

		[B]AFKChecker[\/B]\s»\sSie\ssind\snun\sschon\slänger\sals\s10\sMinuten\sinaktiv,\sSie\swerden\sjetzt\sverschoben! target=9 invokerid=6 invokername=»\sDropSystem invokeruid=9Xx3ciS1+gktsG6Z6MSGM6Z3974=

		*/

		public DropSystems() { }

		public void Initialize()
		{
			var _suid = Ts3FullClient.WhoAmI().Value.VirtualServerUid;
			if (_suid != suid) {
				Log.Warn("Server UID {} does not match {}. Plugin disabled!", _suid, suid); return;
			}
			channel_description_file = Path.Combine(ConfRoot.Plugins.Path.Value, $"{PluginInfo.ShortName}.txt");
			Ts3FullClient.OnEachTextMessage += OnEachTextMessage;
			// Ts3FullClient.OnChannelListFinished += OnChannelListFinished;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnChannelListFinished(object sender, IEnumerable<ChannelListFinished> e)
		{
			// we are ready
		}

		private void OnEachTextMessage(object sender, TextMessage e)
		{
			if (e.Target != TS3Client.TextMessageTargetMode.Private) return;
			if (e.InvokerId == Ts3FullClient.ClientId) return;
			var trimmed = e.Message.Trim().Replace("\t", " ").Replace("\r", "").Replace("\n", " ");
			switch (e.InvokerUid) {
				case uid_bot_dropverify:
					if (e.Message.Contains(msg_verification_required)) {
						Ts3Client.SendMessage("!verify", e.InvokerId);
						Log.Warn("Trying to verify...");
					} else if (e.Message.Contains(msg_verification_success)) {
						Log.Warn("Successfully verified...");
					}
					// Log.Debug("Message from uid_bot_dropverify: \"{}\"", e.InvokerUid, trimmed);
					break;
				case uid_bot_dropranking:
					if (e.Message.Contains(msg_nextup_newlvl)) {
						getNextUp();
					}
					else if (e.Message.Contains(msg_nextup_response))
					{
						var g = regex_nextup.Match(e.Message).Groups;
						try {
							nextUp.Item1 = g[1].Value; var days = int.Parse(g[2].Value); var hours = int.Parse(g[3].Value); var minutes = int.Parse(g[4].Value); // var seconds = int.Parse(g[5].Value)
							nextUp.Item2 = new TimeSpan(days: days, hours: hours, minutes: minutes, seconds: 0); //  .AddDays(days).AddHours(hours).AddMinutes(minutes);
						} catch (Exception ex) { Log.Error("Error while handling nextup: {}", ex.StackTrace); }
						// Log.Warn("Got nextup: {}", e.Message);
					}
					// Log.Debug("Message from uid_bot_dropranking: \"{}\"", e.InvokerUid, trimmed);
					break;
				default:
					// File.WriteAllText("msg.txt", e.Message);
					Log.Info("Message from Unknown UID ({}): \"{}\"", e.InvokerUid, trimmed);
					break;
			}
		}

		public void getNextUp()
		{
			var t = Ts3FullClient.GetClientIds(uid_bot_dropranking).Unwrap();
			Ts3Client.SendMessage("!nextup", t[0].ClientId).UnwrapThrow();
		}

		[Command("nextup")]
		public string CommandNextUp()
		{
			getNextUp();
			return $"Next Rank: \"{nextUp.Item1}\" in {nextUp.Item2}";
		}

		public void Dispose()
		{
			Ts3FullClient.OnEachTextMessage -= OnEachTextMessage;
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
