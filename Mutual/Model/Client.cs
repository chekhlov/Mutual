using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutual.Helpers;

namespace Mutual.Model
{
	public class Client : BaseNotify
	{
		public virtual int Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string Citizen { get; set; }
		public virtual string Phone { get; set; }
		public virtual string DocNumber { get; set; }
	}
}
