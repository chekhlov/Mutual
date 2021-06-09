using System.Data;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Driver;

namespace Mutual.Helpers.NHibernate
{
	public class SqliteConnection : BaseConnection
	{
		public readonly string defDatabase = "data.db";
		public SqliteConnection(HbmMapping mapping) : base(mapping)
		{
			_configuration.DataBaseIntegration(x => {
				x.ConnectionString = BuildConnectionString(defDatabase);
				x.Driver<SQLite20Driver>();
				x.ConnectionProvider<DriverConnectionProvider>();
				x.Dialect<SQLiteDialect>();
				x.Timeout = 60;
				x.LogSqlInConsole = true;
				x.LogFormattedSql = true;
				x.IsolationLevel = IsolationLevel.ReadCommitted;
			});		
		}

		public SqliteConnection(string dbName, HbmMapping mapping) : this(mapping)
		{
			ConnectionString = BuildConnectionString(dbName);
		}

		public virtual string BuildConnectionString(string dbName)
		{
			return $"Data Source={dbName}; Version=3; UseUTF16Encoding=True;"; 
		}
	}
}