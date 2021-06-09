using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using Caliburn.Micro;
using Microsoft.Extensions.Logging;
using Splat;
using Mutual.Helpers;
using Mutual.Helpers.Window;
using Mutual.Model;
using Mutual.ViewModel;

namespace Mutual.ViewModels
{
	public class AboutViewModel : BaseScreen
	{
		public virtual string ProgramName => Global.Constaint.ProgramName; 
		public virtual string ProgramVersion => Global.Constaint.ProgramVersion; 
		public virtual string Author => Global.Constaint.Copyright; 

		public async Task Ok()
		{
			WasCanceled = false;
			await TryCloseAsync();
		}
	}
}