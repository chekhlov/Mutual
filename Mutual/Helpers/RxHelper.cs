using Caliburn.Micro;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Mutual.Helpers
{
	public static class RxHelper
	{
		public static IObservable<EventPattern<PropertyChangedEventArgs>> Changed(this INotifyPropertyChanged self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged");
		}

		public static IObservable<EventPattern<PropertyChangedEventArgs>> Changed<T>(this NotifyValue<T> self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged")
				.Where(e => e.EventArgs.PropertyName == "Value");
		}

		public static IObservable<T> ToObservable<T>(this NotifyValue<T> self)
		{
			return Observable.FromEventPattern<PropertyChangedEventArgs>(self, "PropertyChanged")
				.Where(e => e.EventArgs.PropertyName == "Value")
				.Select(e => ((NotifyValue<T>)e.Sender).Value);
		}

		public static IObservable<EventPattern<PropertyChangedEventArgs>> ChangedValue<T>(this IObservable<T> self)
		{
			return self
				.Select(v => v as INotifyPropertyChanged)
				.Select(v => v == null
					? Observable.Empty<EventPattern<PropertyChangedEventArgs>>()
					: Observable.FromEventPattern<PropertyChangedEventArgs>(v, "PropertyChanged"))
				.Switch();
		}

		public static IObservable<NotifyCollectionChangedEventArgs> Changed<T>(this ObservableCollection<T> self)
		{
			return self == null
				? Observable.Empty<NotifyCollectionChangedEventArgs>()
				: Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
				a => (s, e) => a(e),			
				handler  => self.CollectionChanged += handler,
				handler  => self.CollectionChanged -= handler);		
		}

		public static IObservable<NotifyCollectionChangedEventArgs> CollectionChanged<T>(this IObservable<ObservableCollection<T>> self)
		{
			return self.Select(v => Observable
					.Return(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))
					.Merge(v.Changed()))
				.Switch();
		}

		public static IObservable<NotifyCollectionChangedEventArgs> Changed(this INotifyCollectionChanged self)
		{
			return Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
				a => (s, e) => a(e),			
				handler  => self.CollectionChanged += handler,
				handler  => self.CollectionChanged -= handler);
		}
		
		public static IDisposable Subscribe(this INotifyCollectionChanged self, IObserver<NotifyCollectionChangedEventArgs> observer)
		{
			// Создаем наблюдателя из события
			var observable = Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
				a => new NotifyCollectionChangedEventHandler(new Action<object, NotifyCollectionChangedEventArgs>((s, e) => a(e))),			
				handler  => self.CollectionChanged += handler,
				handler  => self.CollectionChanged -= handler);
			return self.Changed().Subscribe(observer);
		}
		
	}
}