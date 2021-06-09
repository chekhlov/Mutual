using System;
using System.ComponentModel;
using Mutual.Helpers;

namespace Mutual.Model
{
	public class DbInfo : BaseNotify
	{
		public virtual uint Id { get; set; }
		public virtual string Info { get; set; } = Global.Constaint.ProgramName;
		public virtual string Version { get; set; } = Global.Constaint.ProgramVersion;
	}
}