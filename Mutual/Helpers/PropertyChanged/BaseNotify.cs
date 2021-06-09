using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Mutual.Helpers.NHibernate;

namespace Mutual.Helpers
{
	public abstract class BaseNotify : INotifyPropertyChanged
	{
		[JsonIgnore, XmlIgnore, Ignore]
		public virtual bool IsChanged { get; set; } = false;

		[JsonIgnore, XmlIgnore, Ignore]
		public virtual bool IsNotifying { get; set; } = true;
		
		public virtual event PropertyChangedEventHandler PropertyChanged;
		
		public virtual bool OnPropertyChanged<T>(ref T field, T value, [CallerMemberName]string propertyName = "")
		{
			if (Equals(field, value))
				return false;

			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
		
		public virtual void OnPropertyChanged([CallerMemberName]string property = "")
		{
			IsChanged = true;
			if (!IsNotifying)
				return;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}
		
	}
}