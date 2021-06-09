using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mutual.Helpers
{
	public class CustomException : Exception
	{
		public CustomException(string message, Exception e) : base($"{message}{(e != null ? "\n" + e.Message : "")}")
		{ 
			Logger.Log.LogError($"Перехвачено исключение \"{this.GetType().FullName}: {message}{(e != null ? "\n" + e.Message : "")}\"");
		}
		public CustomException(string message) : base(message)
		{ 
			Logger.Log.LogError($"Перехвачено исключение \"{this.GetType().FullName}: {message}\"");
		}
		public CustomException(object sender, string message) : base($"{sender?.GetType().FullName}: {message}")
		{
			Logger.Log.LogError($"Перехвачено исключение \"{sender?.GetType().FullName}: {message}\"");
		}
	}
}