using System;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Mutual.Helpers;
using Mutual.Helpers.NHibernate;
using Mutual.Helpers.Xml;

namespace Mutual.Model.Config
{
	[Serializable]
	[XmlComment(Value = "Блок настроек работы с базой данных")]
	public class DatabaseSection
	{
		// Тип базы данных
		[XmlComment(Value = "Используемая СУБД (поддерживается ORACLE)")]
		public virtual DbmsType Dbms { get; set; } = DbmsType.ORACLE;

		public BaseConnection.IConnectionInfo ConnectionInfo { get; set; } = new OracleConnectionInfo();
	}
}