using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Caliburn.Micro;
using Mutual.ViewModel;
using Mutual.Helpers.NHibernate;
using Mutual.Helpers;
using static Mutual.Helpers.NHibernate.BaseConnection;
using Mutual.Model.Config;
using static Mutual.Helpers.NHibernate.OracleConnection;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Mutual.Helpers.Utilities;

namespace Mutual.ViewModels
{
	public class SettingViewModel : BaseScreen
	{

		public Config Config { get; set; } = new Config();
		public NotifyValue<bool> OracleInfoVisible{ get; set; }
		public NotifyValue<bool> MySQLInfoVisible { get; set; }
		public NotifyValue<bool> SQLiteInfoVisible { get; set; }
		
		public NotifyValue<IOracleConnectionInfo> ConnectionInfo { get; set; }
		public NotifyValue<LoggingSection> Log { get; set; }
		public NotifyValue<bool> LogFile { get; set; }
		public NotifyValue<bool> LogConsole { get; set; }
		public NotifyValue<bool> LogDebug { get; set; }
		public NotifyValue<bool> TabConnection { get; set; }

		public SettingViewModel(bool showOnlyTabConnection = false)
		{
			Config = Config.LoadXml($"{Global.AppName}.config");
		
			TabConnection.Value = showOnlyTabConnection;

			ConnectionInfo.Value = new OracleConnectionInfo(Config.Database.ConnectionInfo);
			
			Log.Value = Global.Config.Logging;
			LogFile.Value = Log.Value.Logger.HasFlag(LogOutput.File); 
			LogConsole.Value = Log.Value.Logger.HasFlag(LogOutput.Console); 
			LogDebug.Value = Log.Value.Logger.HasFlag(LogOutput.Debug); 
			Config.ObservableForProperty(x => x.Database.Dbms).Subscribe(x => {
				OracleInfoVisible.Value = x.Value == DbmsType.ORACLE;
				MySQLInfoVisible.Value = x.Value == DbmsType.MYSQL;
				SQLiteInfoVisible.Value = x.Value == DbmsType.SQLITE;
			});

			LogFile.Subscribe(x => Log.Value.Logger = Log.Value.Logger.SetFlag(LogOutput.File, x));
			LogConsole.Subscribe(x =>  Log.Value.Logger = Log.Value.Logger.SetFlag(LogOutput.Console, x));
			LogDebug.Subscribe(x => Log.Value.Logger = Log.Value.Logger.SetFlag(LogOutput.Debug, x));
		}

		public async Task TestConnection()
		{
			try {
				using var _ = TryLockUI();
				var conn = ConnectionFabric.GetConnection(Config.Database.Dbms, Config.Database.ConnectionInfo);
				await conn.OpenAsync(null, CancellationToken.Token);
				WindowManager.Notify($"Соединение с БД установлено успешно!");
				
			}
			catch (Exception e) {
				WindowManager.Error($"Неудалось соединится с БД, проверьте настройки соединения!\n{e.Message}");
			}
		}
		
		public async Task Ok()
		{
			IConnectionInfo connectionInfo = Config.Database.Dbms switch {
				DbmsType.MYSQL => new MySqlConnectionInfo(),
				DbmsType.ORACLE => new OracleConnectionInfo(),
				_ => null
			};
			
			if (connectionInfo != null)
				_Type.Copy(Config.Database.ConnectionInfo, connectionInfo);

			Config.Logging = Log.Value;
			Config.Database.ConnectionInfo = ConnectionInfo.Value;
			Config.Save();
			WasCanceled = false;
			await TryCloseAsync();
		}
	}
}