using System;
using Mutual.Helpers;

namespace Mutual.Model
{
	public class Department : BaseNotify
	{
		public virtual uint Id { get; set; }
		public virtual string Code { get; set; }
		public virtual string Name { get; set; }
		public virtual string ShortName { get; set; }
		public virtual bool InStatus { get; set; }
		public virtual bool OutStatus { get; set; }
		public virtual uint StatusDep { get; set; }
	}
	
}