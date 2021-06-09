using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;

namespace Mutual.Helpers.Window
{
	public static class WpfHelper
	{
		public static IEnumerable<T> GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null) 
				yield break;
			
			if (depObj is ContentControl contentControl) {
				var content = contentControl.Content as DependencyObject;
				if (content is T) 
					yield return (T) content;

				var childs = content?.GetChildOfType<T>();
				if (childs != null) {
					foreach (var o in childs)
						yield return o;
				}
				yield break;
			}

			if (depObj is TabControl tabControl) {
				foreach (TabItem tab in tabControl.Items) {
					var childs =  tab?.GetChildOfType<T>();
					if (childs != null) {
						foreach (var o in childs)
							yield return o;
					}
				}
				yield break;
			}
			
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);
				if (child is T) 
					yield return (T) child;
				
				var childs = child?.GetChildOfType<T>();
				if (childs != null) {
					foreach (var o in childs)
						yield return o;
				}
			}
		}		
		
	}
}