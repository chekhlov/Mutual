using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Mutual.Helpers;
using Mutual.Helpers.Utilities;
using Mutual.Helpers.Xml;
using Microsoft.Extensions.Logging;

namespace Mutual.Model.Config
{
	//Класс с настройками программы, который выгружается в xml файл
	[Serializable]
	[XmlComment(Value = "Конфигурационный файл " + global::Mutual.Global.Constaint.ProgramName)]
	[XmlRoot("Config", Namespace = global::Mutual.Global.Constaint.ProgramNamespace, IsNullable = false)]
	public class Config
	{
		[XmlIgnore, JsonIgnore]
		// имя файла конфигурации по умолчанию
		public string ConfigFileName { get; protected set; } = "default.config";

		public GlobalSection Global { get; set; } = new GlobalSection();
		public DatabaseSection Database { get; set; } = new DatabaseSection();
		public LoggingSection Logging { get; set; } = new LoggingSection();

		public static Config LoadXml(string configFileName)
		{
			try
			{
				if (!File.Exists(configFileName))
					throw new Exception($"Файл конфигурации {configFileName} не обнаружен");

				var xmlFormat = new CustomXmlSerializer(typeof(Config));
				using var fs = new FileStream(configFileName, FileMode.Open);
				var config = (Config) xmlFormat.Deserialize(fs);
				config.ConfigFileName = configFileName;
				fs.Close();
				return config;
			}
			catch (Exception e)
			{
				Logger.Log.LogError(e, "Ошибка чтения файла конфигурации ");
			}

			return new Config() { ConfigFileName = configFileName };
		}
		
		public static void SaveXml(string fileName, Config conf)
		{
			var xmlFormat = new CustomXmlSerializer(typeof(Config));
			using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
			xmlFormat.Serialize(fs, conf);
		}
		
		public void Save()
		{
			Config.SaveXml(ConfigFileName, this);
		}
	} 
} 