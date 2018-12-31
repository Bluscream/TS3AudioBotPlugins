using System;
using System.Linq;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3Client.Helper;
using TS3AudioBot.Web.Api;
using TS3AudioBot.Helper;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using TS3AudioBot.Config;
using TS3AudioBot.Helper.Environment;
using System.Collections.Generic;

namespace Tools
{
	public class Tools : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Ts3FullClient TS3FullClient { get; set; }
		public Ts3Client TS3Client { get; set; }
		public ConfBot Conf { get; set; }
		public PlayManager BotPlayer { get; set; }
		public IPlayerConnection PlayerConnection { get; set; }
		public IVoiceTarget targetManager { get; set; }
		public ConfRoot ConfRoot { get; set; }

		public void Initialize()
		{
			TS3FullClient.OnChannelCreated += OnChannelCreated;
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
		}

		private void OnChannelCreated(object sender, System.Collections.Generic.IEnumerable<ChannelCreated> e)
		{
			TS3FullClient.ChannelSubscribeAll(); // Proper implementation
		}

		[Command("id")]
		public static JsonValue<ClientData> CommandGetUserByName(Ts3Client ts3Client, string username)
		{
			var client = ts3Client.GetClientByName(username).UnwrapThrow();
			return new JsonValue<ClientData>(client, $"\nClient: [url=client://{client.ClientId}/{client.Uid}]{client.Name}[/url]\nClient ID: [b]{client.ClientId}[/b]\nDatabase ID: [b]{client.DatabaseId}[/b]\nChannel ID: [b]{client.ChannelId}[/b]\nUnique ID: [b]{client.Uid}");
		}

		[Command("isowner", "Check if you're owner")]
		public string CommandCheckOwner(ExecutionInformation info)
		{
			return info.HasRights("*").ToString();
		}

		[Command("talkpowerrequest", "Request Talk Power!")]
		//[RequiredParameters(0)]
		public void CommandTPRequest(ExecutionInformation info, string message)
		{
			TS3FullClient.Send("clientupdate", new CommandParameter("client_talk_request", 1), new CommandParameter("client_talk_request_msg", message = message ?? ""));
		}

		[Command("rawcmd")]
		//[RequiredParameters(1)]
		public string CommandRawCmd(ExecutionInformation info, string cmd, params string[] cmdpara)
		{
			try
			{
				var result = TS3FullClient.Send<TS3Client.Messages.ResponseDictionary>(cmd,
					cmdpara.Select(x => x.Split(new[] { '=' }, 2)).Select(x => new CommandParameter(x[0], x[1])).Cast<ICommandPart>().ToList());
				if (!result.Ok) return result.Error.ErrorFormat();
				return string.Join("\n", result.Value.Select(x => string.Join(", ", x.Select(kvp => kvp.Key + "=" + kvp.Value))));
				//return "Sent command.";
			}
			catch (Ts3Exception ex) { throw new CommandException(ex.Message, CommandExceptionReason.CommandError); }
		}

		[Command("hashpassword")]
		public string CommandHashPassword(ExecutionInformation info, string pw)
		{
			return Ts3Crypt.HashPassword(pw);
		}

		[Command("ownchannel")]
		public string CommandGetOwnChannelID(ExecutionInformation info)
		{
			return TS3FullClient.WhoAmI().Unwrap().ChannelId.ToString();
		}

		[Command("subscribe ownchannel")]
		public void CommandSubscribeOwnChannel(IVoiceTarget targetManager)
		{
			targetManager.WhisperChannelSubscribe(true, TS3FullClient.WhoAmI().Unwrap().ChannelId);
		}

		[Command("getchannel byname")]
		public ChannelIdT CommandGetChannelIdByName(params string[] _name)
		{
			var name = string.Join(" ", _name);
			var command = new Ts3Command("channellist", new List<ICommandPart>() { });
			var createResult = TS3FullClient.SendNotifyCommand(command, NotificationType.ChannelList);
			if (!createResult.Ok) { }
			var channellist = createResult.Value.Notifications.Cast<ChannelList>();
			foreach (var channel in channellist)
			{
				if (channel.Name == name)
					return channel.ChannelId;
			}
			return 0;
		}

		[Command("isplaying playerconnection")]
		public bool CommandIsPlaying(IPlayerConnection playerConnection)
		{
			return !playerConnection.Paused;
		}

		[Command("isplaying playmanager")]
		public bool CommandIsPlaying2(PlayManager playManager)
		{
			return playManager.IsPlaying;
		}

		[Command("isplaying weird")]
		public bool CommandIsPlaying3(IPlayerConnection playerConnection, PlayManager playManager)
		{
			var paused = playerConnection.Paused;
			var playing = playManager.IsPlaying;
			if (paused && !playing)
				return false;
			else if (!paused && playing)
				return true;
			else throw new Exception("Unknown");
		}

		[Command("bug")]
		public string CommandReportBug()
		{
			// WebClient client = new WebClient(); string downloadString = client.DownloadString("https://raw.githubusercontent.com/Splamy/TS3AudioBot/master/.github/ISSUE_TEMPLATE/bug_report.md");
			return $@"https://github.com/Bluscream/TS3AudioBot/issues/new?template=bug_report_auto.md&version={SystemData.AssemblyData.Version}&branch={SystemData.AssemblyData.Branch}&commit={SystemData.AssemblyData.Branch}&platform={SystemData.PlatformData.ToString()}&runtime={SystemData.RuntimeData.FullName}&log=Nothing%20recorded";
		}

		[Command("channellist")]
		public JsonArray<ChannelList> CommandListChannels()
		{
			var command = new Ts3Command("channellist", new List<ICommandPart>() {});
			var createResult = TS3FullClient.SendNotifyCommand(command, NotificationType.ChannelList);
			if (!createResult.Ok) { }
			var channellist = createResult.Value.Notifications.Cast<ChannelList>();
			var channelList = new List<ChannelList>();
			foreach (var channel in channellist)
			{
				channelList.Add(channel);
			}
			return new JsonArray<ChannelList>(channelList.ToArray());
		}

		public void Dispose()
		{
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
