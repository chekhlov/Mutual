using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Mutual.ViewModels;

namespace Mutual.Views
{
	public partial class AboutView : BaseScreenView
	{
		protected DispatcherTimer _timer;
		protected int _step;
		protected int _tick;
		public AboutView()
		{
			InitializeComponent();

			Loaded += (sender, args) => {
				_step = -1;
				_tick = 0;
				_timer = new DispatcherTimer(DispatcherPriority.Normal);
				_timer.Interval = TimeSpan.FromMilliseconds(50);
				_timer.Start();
				_timer.Tick += (sender, args) => {
					Ellipse1.Width += _step;
					Ellipse1.Height += _step;
					Ellipse2.Width += _step;
					Ellipse2.Height += _step;
					Ellipse3.Width += _step;
					Ellipse3.Height += _step;
					if (Ellipse1.Width < 10 || Ellipse1.Width > 80)
						_step = -_step;

					_tick++;
				};
			};
		}
		
	}
}