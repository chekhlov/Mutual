using System;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using Mutual.Model;

using static Mutual.Helpers.NHibernate.BaseConnection;

namespace Mutual.Helpers.NHibernate
{
	public static class ConnectionFabric
	{
		public static BaseConnection GetConnection(DbmsType type, IConnectionInfo connectionInfo)
		{
			return type switch
			{
				DbmsType.ORACLE => new OracleConnection(connectionInfo, NHibernateMapper.Mapping),
				DbmsType.MYSQL => new MySqlConnection(connectionInfo, NHibernateMapper.Mapping),
				DbmsType.SQLITE => new SqliteConnection(NHibernateMapper.Mapping),
				_ => throw new CustomException("Config: неподдерживаемое СУБД")

			};
		}
	}
}