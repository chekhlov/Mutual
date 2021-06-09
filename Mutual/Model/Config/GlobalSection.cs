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
using Mutual.Helpers.Xml;

namespace Mutual.Model.Config
{
	// --------------------------------------------------------------------------------------------------
	[Serializable]
	[XmlComment(Value = "Блок глобальных настроек программы")]
	public class GlobalSection
	{
		public string OrganizationName { get; set; }
		public string LastUseDirectory { get; set; }


		[DisplayName("Анализируемый лист Excel по умолчанию")]
		public string DefaultSheetNameForAnalyze { get; set; } = "Взаиморасчёты";

		[DisplayName("Проверка наличия на листе Excel")]
		public string ValidateHeader { get; set; } = "РАСШИФРОВКА";
		public string ValidateHeader1 { get; set; } = "суммы снятий в рамках централизованных взаиморасчётов между медицинскими организациями за услуги сторонних организаций";
		public string ValidatePeriod { get; set; } = "за период с";
		
		// Анализы могут быть сделаны поистечении нескольких дней с момента направления
		[DisplayName("Дней расширения периода")]
		public int ExpandPeriodDays { get; set; } = 7;

		// Номер строки на листе с которой происходит обработка строк
		[DisplayName("Начальный номер строки на листе")]
		public int StartSheetRow { get; set; } = 7;
		
		[DisplayName("Номер столбца - вывода результата анализа")]
		public int StartResultColumn { get; set; } = 25;		
		
		[DisplayName("Номер столбца - Фамилия")]
		public int LastNameColumn { get; set; } = 4;

		[DisplayName("Номер столбца - Имя")]
		public int FirstNameColumn { get; set; } = 5;
		[DisplayName("Номер столбца - Отчество")]
		public int SecondNameColumn { get; set; } = 6;

		[DisplayName("Номер столбца - Дата рождения")]
		public int BirthdateColumn { get; set; } = 7;
		
		[DisplayName("Номер столбца - код")]
		public int CodeColumn { get; set; } = 8;
		
		[DisplayName("Номер столбца - Тарифная группа")]
		public int TariffGroupColumn { get; set; } = 11;
		
		[DisplayName("Номер столбца - Услуга")]
		public int ServiceColumn { get; set; } = 12;
		
		[DisplayName("Номер столбца - Дата оказания услуги")]
		public int DateColumn { get; set; } = 13;
		
		[DisplayName("Номер столбца - Кол-во услуг")]
		public int CountColumn { get; set; } = 14;
		
		[DisplayName("Номер столбца - Стоимость услуги")]
		public int AmountColumn { get; set; } = 15;
	}
} 