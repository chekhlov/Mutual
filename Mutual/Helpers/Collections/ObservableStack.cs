using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;

namespace Mutual.Helpers.Collections
{
	public class ObservableStack<T> : Stack<T>, IObservable<NotifyCollectionChangedEventArgs>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		public ObservableStack()
		{
		}

		public ObservableStack(IEnumerable<T> collection) : base(collection)
		{
		}

		public ObservableStack(List<T> list) : base(list)
		{
		}


		public new virtual void Clear()
		{
			base.Clear();
			OnCollectionChanged(NotifyCollectionChangedAction.Reset, default);
		}

		public new virtual T Pop()
		{
			var item = base.Pop();
			OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);
			return item;
		}

		public new virtual void Push(T item)
		{
			base.Push(item);
			OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
		}

		public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

	    protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
	    {
	        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
	            action
	            , item
	            , item == null ? -1 : 0)
	        );

	        OnPropertyChanged(nameof(Count));
	    }

	    public virtual event PropertyChangedEventHandler PropertyChanged;

	    protected virtual void OnPropertyChanged(string propertyName)
	    {
	        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { this.PropertyChanged += value; }
			remove { this.PropertyChanged -= value; }
		}
		
		public IDisposable Subscribe(IObserver<NotifyCollectionChangedEventArgs> observer)
		{
			
			
			// Создаем наблюдателя из события
			var observable = Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
				a => new NotifyCollectionChangedEventHandler(new Action<object, NotifyCollectionChangedEventArgs>((s, e) => a(e))),			
				handler  => CollectionChanged += handler,
				handler  => CollectionChanged -= handler);
			return observable.Subscribe(observer);
		}
	}
}