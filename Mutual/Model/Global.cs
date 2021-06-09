using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using Mutual.Helpers;
using Mutual.Helpers.NHibernate;
using Mutual.Helpers.Utilities;
using Mutual.Model;
using Mutual.Model.Config;

namespace Mutual
{
	public static partial class Global
	{
		private static Config _currentCongif;
		private static TaskScheduler _uiScheduler;

		public static Config Config
		{
			get
			{
				if (_currentCongif == null)
					_currentCongif = Config.LoadXml($"{AppName}.config");

				return _currentCongif;
			}
			set
			{
				if (_currentCongif == value) return;

				_currentCongif = value;
				_connection = null;
			}
		}

		public static string AppName => ApplicationInformation.ExecutingAssembly.GetName().Name;

		private static BaseConnection _connection;

		public static BaseConnection Connection
		{
			get
			{
				if (_connection == null)
					_connection = ConnectionFabric.GetConnection(Config.Database.Dbms, Config.Database.ConnectionInfo);

				return _connection;
			}
			set => _connection = value;
		}

		public static SynchronizationContext UiContext => AppBootstrapper.UIContext;
	    public static TaskScheduler UiScheduler
	    {
	            get { return _uiScheduler = _uiScheduler ?? TaskScheduler.FromCurrentSynchronizationContext(); }
	            set { _uiScheduler = value; }
	    }
		
		public static object MainWindow { get; set; }
	}
}