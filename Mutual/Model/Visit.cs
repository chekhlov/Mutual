using System;
using Mutual.Helpers;

namespace Mutual.Model
{
	public class Visit : BaseNotify
	{
		public enum VisitType
		{
			Comming = 101,  // Поступление в приемный покой
			Receipt = 102,	// Поступление в отделение
			Moving = 103,	// Перевод
			ChangeBed = 104,// Смена койки
			Operation = 105	//Операция
		}
		
		public virtual uint Id { get; set; }
		public virtual uint Num { get; set; }
		public virtual DateTime FromDate { get; set; }
		public virtual DateTime? ToDate { get; set; }
		public virtual VisitType VisType { get; set; }
		public virtual Patient Patient { get; set; }
		
		// Поступление в приемное отделение
		public virtual Visit Comming { get; set; } 	

		public virtual Department DepartmentIn { get; set; }
		public virtual Department DepartmentOut { get; set; }
		public virtual Department DepartmentProfile { get; set; }
	}
	
}