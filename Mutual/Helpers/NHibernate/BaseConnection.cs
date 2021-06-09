using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Dapper;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Tool.hbm2ddl;

namespace Mutual.Helpers.NHibernate
{
	public enum DbmsType {
		[Description("Встроенный сервер SQLite")]
		SQLITE = 0,
		[Description("Сервер баз данных MySQL")]
		MYSQL,
		[Description("Сервер баз данных Oracle")]
		ORACLE 
	};
	
	public class BaseConnectionException : CustomException
	{
		public BaseConnectionException(string message) : base(message)
		{ 
		}
		public BaseConnectionException(string message, Exception e) : base(message, e)
		{ 
		}
		public BaseConnectionException(object sender, string message) : base(sender, message)
		{
		}
	}
	
	// Базовый класс соединения с БД
	public abstract class BaseConnection : IDisposable
	{
		
		public interface IConnectionInfo
		{
			/// <summary>
			/// Имя/Ip-адрес сервера
			/// </summary>
			string Server { get; set; }

			/// <summary>
			/// Используемый порт для соединения с БД (если используется
			/// </summary>
			uint Port { get; set; }

			/// <summary>
			/// Наименование базы данных
			/// </summary>
			string Database { get; set; }

			string Login { get; set; }

			string PasswordHash { get; set; } 
			
			string Password { get; set; }			
		}		
		
		protected HbmMapping _mapping;
		protected Configuration _configuration;
		protected ISessionFactory _factory;
		protected ISession _session;
		protected string _connetionString;
		public virtual Configuration Configuration => _configuration;
		public virtual HbmMapping Mapping => _mapping;
		public virtual DbConnection Connection => _session?.Connection;

		public virtual string ConnectionString 
		{ 
			get => _connetionString;
			protected set
			{
				if (_connetionString == value)
					return;
			
				_connetionString = value;
				_configuration.DataBaseIntegration(x => x.ConnectionString = _connetionString);
			}				
		}

		public BaseConnection(HbmMapping mapping)
		{
			_configuration = new Configuration();
			_configuration.AddAssembly(Assembly.GetExecutingAssembly());
			_mapping = mapping;
			_configuration.AddMapping(mapping);
		}
		
		public virtual ISessionFactory Factory => (_factory = _factory ?? _configuration.BuildSessionFactory());

		/// <summary>
		/// Создает сессию запроса к БД
		/// </summary>
		public virtual Task<T> Session<T>(Func<ISession, T> action, CancellationToken token = default)
		{
			var task = Task.Run<T>(() => {
				try {
					using var session = Factory.OpenSession();
					using (session.BeginTransaction()) {

						var value = action(session);

						// в action может самостоятельно управлять транзакцией - откатить или закоммитить
						if (session.Transaction.IsActive)
							session.Transaction.Commit();

						return value;
					}
				} catch (Exception e) {
					throw new BaseConnectionException("BaseConnection: Произошла ошибка при выполнении запроса к БД", e);
				}
			}, token);

			return task;
		}

		/// <summary>
		/// Создает сессию запроса к БД
		/// </summary>
		public virtual Task Session(Action<ISession> action, CancellationToken token = default)
		{
			var task = Task.Run(() => {
				try {
					using var session = Factory.OpenSession();
					using (session.BeginTransaction()) {

						action(session);

						// в action может самостоятельно управлять транзакцией - откатить или закоммитить
						if (session.Transaction.IsActive)
							session.Transaction.Commit();
					}
				} catch (Exception e) {
					throw new BaseConnectionException("BaseConnection: Произошла ошибка при выполнении запроса к БД", e);
				}
			}, token);

			return task;
		}

		public virtual void Open(string connString = null)
		{
			try {
				if(!String.IsNullOrEmpty(connString))
					ConnectionString = connString;

				_session?.Close();
				_session?.Dispose();
				_session = Factory.OpenSession();
			} catch (Exception e) {
				throw new BaseConnectionException("BaseConnection: Произошла ошибка при установке соединения с БД", e);
			}
		}		
		
		public Task OpenAsync(string connString = null, CancellationToken token = default) => Task.Run(() => Open(connString), token);

		public virtual void Close()
		{
			_session?.Close();
			_session?.Dispose();
			_session = null;
			_factory?.Close();
			_factory?.Dispose();
			_factory = null;
		}

		public virtual void Dispose()
		{
			Close();
		}
		
		public async Task UpdateScheme(CancellationToken token = default)
		{
			Logger.Log.LogInformation("Обновление схемы БД");
			var export = new SchemaUpdate(_configuration);
			var alters = new List<string>();
            await export.ExecuteAsync(x => alters.Add(x), false, token);

			await Session(s => Execute(s, alters), token);
		}

		public async Task CreateScheme(CancellationToken token = default)
		{
			Logger.Log.LogInformation("Создание схемы БД");
			var export = new SchemaExport(_configuration);
			var alters = new List<string>();
			await export.CreateAsync(x => alters.Add(x), false, token);

			await Session(s => Execute(s, alters), token);
		}

		private void Execute(ISession session, List<string> alters)
		{
			foreach (var alter in alters) {
				Logger.Log.LogDebug(alter);
				session.Connection.Execute(alter);
			}
		}
		
		
		
	}
}