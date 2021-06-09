using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mutual.Helpers.Utilities
{
	// Класс для получения информации о текущем файле (сборке) данных о Assembly
	public static class ApplicationInformation
	{
		private static Assembly _executingAssembly;
		public static Assembly ExecutingAssembly =>  _executingAssembly ?? (_executingAssembly = Assembly.GetExecutingAssembly());

		public static string Title = ExecutingAssembly?.GetCustomAttributes<AssemblyTitleAttribute>()?.FirstOrDefault()?.Title ?? String.Empty;

		public static Version Version => ExecutingAssembly.GetName().Version;
	}
}