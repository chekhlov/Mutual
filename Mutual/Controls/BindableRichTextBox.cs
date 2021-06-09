using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using Mutual.Helpers.Window;

namespace Mutual.Controls
{
	public class BindableRichTextBox : RichTextBox
	{
		private bool _isAutoScroll = true;

		protected override void OnInitialized(EventArgs e)
	    {
			base.OnInitialized(e);
			
			object operation = null;

			this.TextChanged += (sender, eventArgs) => {
				if (operation != null || !_isAutoScroll) return;

				var scrollViewer = this.GetChildOfType<ScrollViewer>().FirstOrDefault();
				if (scrollViewer == null) return;
				
				operation = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
					operation = null;
					scrollViewer.ScrollToBottom();
				}));
			};
			
	    }
		
		public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(nameof(Document), typeof(FlowDocument), typeof(BindableRichTextBox), new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDocumentChanged)));
		public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register(nameof(AutoScroll), typeof (bool), typeof (BindableRichTextBox), new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnAutoScrollChanged)));

		public new FlowDocument Document {
			get => (FlowDocument) this.GetValue(DocumentProperty);
			set => this.SetValue(DocumentProperty, value);
		}

		public bool AutoScroll
		{
			get => (bool) this.GetValue(AutoScrollProperty); 
			set => this.SetValue(AutoScrollProperty, value); 
		}		

		public delegate void DocumentChangedEventHandler(object sender, FlowDocument doc);
		public virtual event DocumentChangedEventHandler DocumentChanged;
	
		public static void OnDocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is RichTextBox rtb) {
				rtb.Document = (FlowDocument) args.NewValue;
				if (rtb is BindableRichTextBox brtb) 
					brtb.DocumentChanged?.Invoke(rtb, rtb.Document);
			}
		}

		public delegate void AutoScrollChangedEventHandler(object sender, bool autoScroll);
		public virtual event AutoScrollChangedEventHandler AutoScrollChanged;
		
		private static void OnAutoScrollChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			if (obj is BindableRichTextBox brtb) {
				var newState = (bool) args.NewValue;
				if (brtb._isAutoScroll != newState) {
					brtb._isAutoScroll = newState;
					brtb.AutoScrollChanged?.Invoke(brtb, newState);
				}
			} 
		}
	}
}