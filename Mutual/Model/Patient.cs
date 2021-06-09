using System;
using Mutual.Helpers;
using Mutual.Helpers.NHibernate;

namespace Mutual.Model
{
	public class Patient : BaseNotify
	{
		public virtual uint Id { get; set; }
		public virtual uint Num { get; set; }
		public virtual string FirstName { get; set; }
		public virtual string LastName { get; set; }
		public virtual string SecondName { get; set; }
		public virtual bool Sex { get; set; }
		public virtual DateTime Birthdate { get; set; }
		public virtual string ToShortString() => $"{LastName?.ToUpper()} {FirstName?.ToUpper()} {SecondName?.ToUpper()} {Birthdate.ToShortDateString()}";
		public override string ToString() => $"Id={Id}, Num={Num}, ФИО={LastName?.ToUpper()} {FirstName?.ToUpper()} {SecondName?.ToUpper()}, {Birthdate.ToShortDateString()}";	
	
		[Ignore]
		public virtual string Comment { get; set; }
	}
	
}