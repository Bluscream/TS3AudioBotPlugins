using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoChannelCreate
{
	public class Config
	{
		private static FileIniDataParser ConfigParser;
		public bool LoadConfig()
		{
			if (!File.Exists(PluginInfo.ConfigFile))
			{
				AutoChannelCreate.PluginConfig.Sections.Add(new SectionData("default"));
				AutoChannelCreate.PluginConfig["Channel"]["Name"] = "auto";
				AutoChannelCreate.PluginConfig["Channel"]["Password"] = "auto";
				AutoChannelCreate.PluginConfig["Channel"]["Codec"] = "5";
				AutoChannelCreate.PluginConfig["Channel"]["Codec Quality"] = "10";
				AutoChannelCreate.PluginConfig["Channel"]["Maxclients"] = "-1";
				AutoChannelCreate.PluginConfig["Channel"]["Needed Talk Power"] = "0";
				AutoChannelCreate.PluginConfig["Channel"]["Topic Template"] = "Created: {now}";
				SaveConfig();
				return false;
			}
			ConfigParser = new FileIniDataParser();
			AutoChannelCreate.PluginConfig = ConfigParser.ReadFile(PluginInfo.ConfigFile);
			return true;
		}
		public void SaveConfig()
		{
			ConfigParser.WriteFile(PluginInfo.ConfigFile, AutoChannelCreate.PluginConfig);
		}
	}
	class DefaultConfig : IniData
	{
		public DefaultConfig()
		{

		}
	}
}
