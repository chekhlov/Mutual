using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Linq;
using System.Windows;
using DynamicData.Binding;
using Microsoft.Win32;
using Mutual.Model.Config;
using Mutual.ViewModels;
using ReactiveUI;

namespace Mutual.Views
{
	/// <summary>
	/// Interaction logic for MainFormView.xaml
	/// </summary>
	public partial class MainFormView
	{
		public ViewModels.MainFormViewModel Model => DataContext as MainFormViewModel;

		public MainFormView()
		{
			InitializeComponent();
		}

		private void Files_OnDragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effects = DragDropEffects.Copy;
		}

		private void Files_OnDrop(object sender, DragEventArgs e)
		{
			var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
			foreach (var value in files)
				Model.AddFile(value);

			e.Handled = true;
		}

		private static string _lastUseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

		private void btnOpenFiles_Click(object sender, RoutedEventArgs e)
		{
			
			var dialog = new OpenFileDialog();
			dialog.Title = "Выберите файлы или каталог";
			dialog.CheckPathExists = true;
			dialog.CheckFileExists = false;
			dialog.Multiselect = true;
			dialog.FileName = "Выбор папки";
			dialog.Filter = "Файлы Excel (*.xls, *.xlsx)|*.xls;*.xlsx|Все файлы (*.*)|*.*";
			dialog.RestoreDirectory = true;
			dialog.InitialDirectory = _lastUseDirectory;
			if (dialog.ShowDialog() == true) {
				_lastUseDirectory = Path.GetDirectoryName(dialog.FileName);
				foreach (string filename in dialog.FileNames)
					Model.AddFile(filename.EndsWith("Выбор папки.xls") ? Path.GetDirectoryName(filename) : filename);
			}
		}
	}
}