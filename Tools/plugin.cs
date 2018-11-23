using System;
using System.Linq;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using TS3AudioBot.Web.Api;
using TS3AudioBot.Helper;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;

namespace Tools
{
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
	public class Tools : IBotPlugin
	{
		private static NLog.Logger Log = NLog.LogManager.GetLogger($"TS3AudioBot.Plugins.{PluginInfo.ShortName}");
		public Ts3FullClient Lib { get; set; }

		public void Initialize()
		{
			Log.Info("Plugin {0} v{1} by {2} loaded.", PluginInfo.Name, PluginInfo.Version, PluginInfo.Author);
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
			Lib.Send("clientupdate", new CommandParameter("client_talk_request", 1), new CommandParameter("client_talk_request_msg", message = message ?? ""));
		}

		[Command("rawcmd")]
		//[RequiredParameters(1)]
		public string CommandRawCmd(ExecutionInformation info, string cmd, params string[] cmdpara)
		{
			try
			{
				var result = Lib.Send<TS3Client.Messages.ResponseDictionary>(cmd,
					cmdpara.Select(x => x.Split(new[] { '=' }, 2)).Select(x => new CommandParameter(x[0], x[1])).Cast<ICommandPart>().ToList());
				//return string.Join("\n", result.Select(x => string.Join(", ", x.Select(kvp => kvp.Key + "=" + kvp.Value))));
				return "Sent command.";
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
			return Lib.WhoAmI().Unwrap().ChannelId.ToString();
		}

		[Command("subscribe ownchannel")]
		public void CommandSubscribeOwnChannel(IVoiceTarget targetManager)
		{
			targetManager.WhisperChannelSubscribe(Lib.WhoAmI().Unwrap().ChannelId, true);
		}

		public void Dispose()
		{
			Log.Info("Plugin {} unloaded.", PluginInfo.Name);
		}
	}
}
