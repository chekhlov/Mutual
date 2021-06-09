using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Connection;
using NHibernate.Dialect;
using NHibernate.Driver.OracleManagedDataAccessCore;

namespace Mutual.Helpers.NHibernate
{
	public class OracleConnectionException : BaseConnectionException
	{
		public OracleConnectionException(string message) : base(message)
		{ 
		}
		public OracleConnectionException(object sender, string message) : base(sender, message)
		{
		}
	}
	
	public class OracleConnection: BaseConnection
	{
		public enum ConnectType { Normal, SYSDBA, SYSOPER }
		
		public interface IOracleConnectionInfo : IConnectionInfo
		{
			/// <summary>
			/// Тип соединения  Normal, SYSDBA, SYSOPER
			/// </summary>
			ConnectType ConnectType { get; set; }
		}
		public OracleConnection(HbmMapping mapping) : base(mapping)
		{
			_configuration.DataBaseIntegration(x =>
			{
				x.Driver<OracleManagedDriver>();
				x.ConnectionProvider<DriverConnectionProvider>();
				x.Dialect<Oracle12cDialect>();
				x.Timeout = 60;
				x.LogSqlInConsole = true;
				x.LogFormattedSql = true;
				x.IsolationLevel = IsolationLevel.ReadCommitted;
			});
		}

		public OracleConnection(string connString, HbmMapping mapping) : this(mapping)
		{ 
			ConnectionString = connString;
		}
		
		public OracleConnection(IConnectionInfo info, HbmMapping mapping) : this(mapping)
		{
			if (info is IOracleConnectionInfo connectionInfo)
				ConnectionString = BuildConnectionString(connectionInfo);
			else 
				throw new OracleConnectionException("Неполные настройки соединения");
		}

		public virtual string BuildConnectionString(IOracleConnectionInfo info)
		{
			var privileges = string.Empty;
			switch (info.ConnectType) {
				case ConnectType.SYSDBA:
					privileges = "SYSDBA";
					break;
				case ConnectType.SYSOPER:
					privileges = "SYSOPER";
					break;
			}

			return $"Data Source={info.Server}:{info.Port}/{info.Database};User Id={info.Login};Password={info.Password};DBA Privilege={privileges}";
		}		
	}
}