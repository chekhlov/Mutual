using System;
using Mutual.Helpers.Xml;
using Microsoft.Extensions.Logging;

namespace Mutual.Model.Config
{
	[Flags]
	public enum LogOutput { None = 0, Console = 1, File = 2, Debug = 4 }

	[Serializable]
	[XmlComment(Value = "Блок настроек ведения журнала событий (логов)")]
	public class LoggingSection
	{
		[XmlComment(Value = "Путь к лог-файлам")]
		public string Path { get; set; } = @"Log\";

		[XmlComment(Value = "Маска файла - %APP%_%DD%%MM%%YYYY%.log")]
		public string MaskFileName { get; set; } = "%APP%_%DD%%MM%%YYYY%.log";

		[XmlComment(Value = "Уровень логирования (Trace, Debug, Information, Warning, Error, Critical, None")]
		public LogLevel LogLevel { get; set; }  = LogLevel.None;

		[XmlComment(Value = "Вывод лога (None, Console, File, Debug) можно указать несколько значений через пробел - 'Console File'")]
		public LogOutput Logger { get; set; } = LogOutput.Console;
	} 
}