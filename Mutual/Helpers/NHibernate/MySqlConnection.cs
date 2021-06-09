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
	public class MySqlConnection : BaseConnection
	{
		public MySqlConnection(HbmMapping mapping) : base(mapping)
		{
			_configuration.DataBaseIntegration(x => {
				x.Driver<MySqlDataDriver>();
				x.ConnectionProvider<DriverConnectionProvider>();
				x.Dialect<MySQL57Dialect>();
				x.Timeout = 60;
				x.LogSqlInConsole = true;
				x.LogFormattedSql = true;
				x.IsolationLevel = IsolationLevel.ReadCommitted;
			});		}

		public MySqlConnection(string connectionString, HbmMapping mapping) : this(mapping)
		{
			ConnectionString = connectionString;
		}
		
		public MySqlConnection(IConnectionInfo info, HbmMapping mapping) : this(mapping)
		{
			ConnectionString = BuildConnectionString(info);
		}

		public virtual string BuildConnectionString(IConnectionInfo info)
		{
			return $"Data Source={info.Server};Port={info.Port};Database={info.Database};User Id={info.Login};Password={info.Password};Connect Timeout=300;Default command timeout=300;Allow User Variables=true;SslMode=none;";
		}	
	}
}