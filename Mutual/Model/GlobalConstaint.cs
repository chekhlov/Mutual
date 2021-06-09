using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutual.Helpers.Utilities;

namespace Mutual
{
	public static partial class Global
	{
		public static class Constaint
		{
			// в C# const - не означает константное значение (как в С++) и соответственно не инициализируется - используется только как литеральный макрос,
			// поэтому если нужно const значение переменной, то используется следующа конструкция - static readonly

			public const string ProgramName = "Анализ взаиморасчетов ГОБУЗ \"Мурманская областная детская клиническая больница\"";
			public const string BuildDate = "19.05.2020";
			public const string Copyright = "(c) 2019,2020 Copyright by HSi";
			public const string ProgramNamespace = "http://hsi.ru/mdgb/mutual/v01";

			public const string ProgramVersion = "2.0.0.1-alpha";
			public static string BuildCopyright => $"{ProgramName} - {ProgramVersion}({BuildDate}) {Copyright}";
		}
	}
}