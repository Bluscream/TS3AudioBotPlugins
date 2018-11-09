using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Config;
using TS3AudioBot.Plugins;
using TS3AudioBot;
using TS3Client.Commands;
using TS3Client.Full;
using TS3Client.Messages;
using TS3Client;
using ClientIdT = System.UInt16;
using ChannelIdT = System.UInt64;
using TS3AudioBot.Web.Api;
using TS3AudioBot.Helper;

namespace Tools
{
	public class Tools : IBotPlugin
	{

		public class PluginInfo
		{
			public static readonly string Name = typeof(PluginInfo).Namespace;
			public const string Description = "";
			public const string URL = "https://github.com/Bluscream/TS3AudioBotPlugins/tree/develop/Tools";
			public const string Author = "Bluscream <admin@timo.de.vc>";
			public const int Version = 1337;
		}
		public Ts3FullClient lib;

		public void PluginLog(LogLevel logLevel, string Message) { Console.WriteLine($"[{logLevel.ToString()}] {PluginInfo.Name}: {Message}"); }

		public void Initialize()
		{
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " v" + PluginInfo.Version + " by " + PluginInfo.Author + " loaded.");

		}

		public void Dispose()
		{
			PluginLog(LogLevel.Debug, "Plugin " + PluginInfo.Name + " unloaded.");
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
			lib.Send("clientupdate", new CommandParameter("client_talk_request", 1), new CommandParameter("client_talk_request_msg", message = message ?? ""));
		}

		[Command("rawcmd")]
		//[RequiredParameters(1)]
		public string CommandRawCmd(ExecutionInformation info, string cmd, params string[] cmdpara)
		{
			try
			{
				var result = lib.Send<TS3Client.Messages.ResponseDictionary>(cmd,
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
			return lib.WhoAmI().Value.ChannelId.ToString();
		}
	}
}
