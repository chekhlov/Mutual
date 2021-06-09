using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mutual.Helpers.NHibernate;
using Mutual.Helpers.Xml;
using Mutual.Helpers.Utilities;
using ConnectType = Mutual.Helpers.NHibernate.OracleConnection.ConnectType;
using IConnectionInfo = Mutual.Helpers.NHibernate.OracleConnection.IConnectionInfo;

namespace Mutual.Model.Config
{
	[Serializable]
	[XmlComment(Value = "Блок настроек соединения с сервером ORACLE")]
	public class OracleConnectionInfo : OracleConnection.IOracleConnectionInfo
	{
		public OracleConnectionInfo() {}
		public OracleConnectionInfo(IConnectionInfo info)
		{
			if (info != null) 
				_Type.Copy(info, this);
		}
		public virtual string Server { get; set; } = "localhost";
		public virtual uint Port { get; set; } = 1521;
		public virtual string Database { get; set; } = "DB";
		public virtual string Login { get; set; }
		public virtual string PasswordHash 
		{
			get => Mutual.Helpers.Crypto.EncodeFromBase64(Password);
			set => Password = Mutual.Helpers.Crypto.DecodeToBase64(value);
		}

		[XmlIgnore, Ignore] 
		public virtual string Password { get; set; }
		
		[XmlComment(Value = "Valid values - Normal, SYSDBA, SYSOPER")]
		public virtual ConnectType ConnectType { get; set; } = ConnectType.Normal;
	}
	
	[Serializable]
	[XmlComment(Value = "Блок настроек соединения с сервером MySQL")]
	public class MySqlConnectionInfo : IConnectionInfo
	{
		public virtual string Server { get; set; } = "localhost";
		public virtual uint Port { get; set; } = 3306;
		public virtual string Database { get; set; }
		public virtual string Login { get; set; }

		public virtual string PasswordHash 
		{
			get => Mutual.Helpers.Crypto.EncodeFromBase64(Password);
			set => Password = Mutual.Helpers.Crypto.DecodeToBase64(value);
		}

		[XmlIgnore, Ignore] 
		public virtual string Password { get; set; }
	}		
}