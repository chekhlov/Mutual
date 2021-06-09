using Mutual.Helpers;
using Mutual.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Fastenshtein;
using Mutual.Helpers.NHibernate;
using NHibernate;
using NHibernate.Linq;
using OfficeOpenXml.FormulaParsing;
using OfficeOpenXml.FormulaParsing.Utilities;

namespace Mutual.Model
{
	public class AnalyzeException : CustomException
	{
		public AnalyzeException(string message) : base(message)
		{
		}
	}

	public class AnalyzeItem : BaseNotify
	{
		public virtual string FirstName { get; set; }
		public virtual string LastName { get; set; }
		public virtual string SecondName { get; set; }
		public virtual DateTime Birthdate { get; set; }
		public virtual DateTime Date { get; set; }
		public virtual string Code { get; set; }
		public virtual string Service { get; set; }
		public virtual string TariffGroup { get; set; }
		public virtual int Count { get; set; }
		public virtual double Amount { get; set; }
		public virtual Patient Patient { get; set; }
		public virtual Visit Visit { get; set; }
		public override string ToString() => $"{LastName?.ToUpper()} {FirstName?.ToUpper()} {SecondName?.ToUpper()} {Birthdate.ToShortDateString()}";
	}

	public class Analyze
	{
		public Analyze()
		{
		}

		private CancellationToken _cancelation;
		private BaseConnection _connection;
		private Dictionary<string, Patient> _cachePatient = new Dictionary<string, Patient>();

		public virtual BaseConnection Connection => _connection ?? Global.Connection;
		protected virtual Task<T> Session<T>(Func<ISession, T> action, CancellationToken token = default) => Connection.Session<T>(action, token == default ? _cancelation : token);
		protected virtual Task Session(Action<ISession> action, CancellationToken token = default) => Connection.Session(action, token == default ? _cancelation : token);

		private List<string> GetFilesFromDirectoryRecursive(string path)
		{
			var files = new List<string>();

			if (_cancelation.IsCancellationRequested)
				return null;

			Logger.Log.LogInformation($"Обработка каталога {path}");
			var dir = Directory.GetDirectories(path);
			var fs = Directory.GetFiles(path, "*.xls?");

			if (dir.Any())
				Logger.Log.LogInformation($"  найдено {dir.Length} каталога(ов)");

			Logger.Log.LogInformation($"  найдено {fs.Length} файла(ов)");
			foreach (var f in fs) {
				Logger.Log.LogInformation($"  - файл {f}");
				files.Add(f);

				if (_cancelation.IsCancellationRequested)
					return null;
			}

			foreach (var d in dir)
				files.AddRange(GetFilesFromDirectoryRecursive(d));

			return files;
		}


		public async Task Run(List<string> paths, CancellationToken cancelation)
		{
			_cancelation = cancelation;
			try {
				if (!paths.Any())
					throw new AnalyzeException("Нет файлов для анализа");

				var files = new List<string>();
				paths.ForEach(x => {
					if (_cancelation.IsCancellationRequested)
						return;

					Logger.Log.LogInformation($"Проверка пути {x}");

					if (File.Exists(x)) {
						Logger.Log.LogInformation($"Найден файл {x}");
						files.Add(x);
						return;
					}

					var attr = File.GetAttributes(x);
					if (attr.HasFlag(FileAttributes.Directory)) {
						files.AddRange(GetFilesFromDirectoryRecursive(x));
						return;
					}

					Logger.Log.LogInformation($"Файл не найден {x}");
				});

				if (!files.Any())
					throw new AnalyzeException("Нет файлов для анализа");

				files = files.Distinct().ToList();

				foreach (var file in files) {
					if (_cancelation.IsCancellationRequested)
						return;

					Logger.Log.LogInformation($"Анализ файла {file}");
					await AnalyzeFile(file);
				}
			} catch (Exception e) {
				Logger.Log.LogError(e, "При анализе произошла ошибка");
			}
		}

		private async Task AnalyzeFile(string file)
		{
			if (!File.Exists(file))
				throw new AnalyzeException($"Не найден файл {file}");

			Logger.Log.LogInformation($"  открытие файла Excel");

			var excelFile = new ExcelPackage(new FileInfo(file));
			var workbook = excelFile.Workbook;

			var sheets = workbook.Worksheets.ToList();

			if (!sheets.Any())
				throw new AnalyzeException("  в книге нет ни одного листа");

			Logger.Log.LogInformation($"    обнаружены листы");
			sheets.ForEach(x => {
				Logger.Log.LogInformation($"    - {x.Name}");
			});

			var sheetName = Global.Config.Global.DefaultSheetNameForAnalyze;
			var sheet = sheets.FirstOrDefault(x => x.Name == sheetName);
			if (sheet == null)
				throw new AnalyzeException("    в книге нет листа для анализа '{sheetName}'");

			await AnalyzeSheet(sheet);
			Logger.Log.LogInformation($"  сохранение файла");

			// Используем сохранение через временный файл - OfficeOpenXml, некорректно сохраняет на сетевых дисках 
			var path = Path.GetDirectoryName(file);
			var tmpFileName = Path.Combine(path, Path.GetRandomFileName());
			excelFile.SaveAs(new FileInfo(tmpFileName));
			excelFile.Dispose();
			File.Replace(tmpFileName, file, null);
			Logger.Log.LogInformation($"  Файл сохранен успешно!");
		}

		private async Task AnalyzeSheet(ExcelWorksheet sheet)
		{
			Logger.Log.LogInformation($"    анализируем лист '{sheet.Name}'");

			var rowCount = sheet.Dimension.End.Row;
			var colCount = sheet.Dimension.End.Column;

			Logger.Log.LogInformation($"      обнаружено {rowCount} строк, {colCount} столбца(ов)");

			var isValidSheets = sheet.Cells[1, 1].Text.Contains(Global.Config.Global.ValidateHeader, StringComparison.OrdinalIgnoreCase);
			isValidSheets = isValidSheets && sheet.Cells[2, 1].Text.Contains(Global.Config.Global.ValidateHeader1, StringComparison.OrdinalIgnoreCase);
			var periodStr = sheet.Cells[3, 1].Text;
			isValidSheets = isValidSheets && periodStr.StartsWith(Global.Config.Global.ValidatePeriod, StringComparison.OrdinalIgnoreCase);

			if (!isValidSheets)
				throw new AnalyzeException($"      нераспознаный формат листа");

			var regex = new Regex(@"(\d{2}[\/.-]\d{2}[\/.-]\d{4})\D+(\d{2}[\/.-]\d{2}[\/.-]\d{4})");

			var match = regex.Match(periodStr);
			if (!match.Success)
				throw new AnalyzeException($"      неудалось определить период сверки взаиморасчетов {periodStr}");
			
			var strDateFrom = match.Groups[1].Value;
			var strDateTo = match.Groups[2].Value;
			var delimeter = strDateFrom.Substring(2,1);
			var strDateFormat = $"dd{delimeter}MM{delimeter}yyyy";

			if (!DateTime.TryParseExact(strDateFrom, strDateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out var begin)) 
				throw new AnalyzeException($"      неверный формат даты с {strDateFrom}");

			if (!DateTime.TryParseExact(strDateTo, strDateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out var end))
				throw new AnalyzeException($"      неверный формат даты по {strDateTo}");

			Logger.Log.LogInformation($"      обнаружен период с {begin.ToShortDateString()} по {end.ToShortDateString()}");

			// Проверяем период выборки - ограничиваем максимум 2мя месяцами
			var diff = end - begin;
			if (diff.Duration() > TimeSpan.FromDays(62))
				throw new AnalyzeException($"      слишком большой период выборки > 2 месяцев - с {begin.ToShortDateString()} по {end.ToShortDateString()}");

			var startRow = Global.Config.Global.StartSheetRow;
			var startResultColumn = Global.Config.Global.StartResultColumn;
			var expandPeriodDays = Global.Config.Global.ExpandPeriodDays;
			var firstNameColumn = Global.Config.Global.FirstNameColumn;
			var lastNameColumn = Global.Config.Global.LastNameColumn;
			var secondNameColumn = Global.Config.Global.SecondNameColumn;
			var birthdateColumn = Global.Config.Global.BirthdateColumn;
			var codeColumn = Global.Config.Global.CodeColumn;
			var tariffGroupColumn = Global.Config.Global.TariffGroupColumn;
			var serviceColumn = Global.Config.Global.ServiceColumn;
			var dateColumn = Global.Config.Global.DateColumn;
			var countColumn = Global.Config.Global.CountColumn;
			var amountColumn = Global.Config.Global.AmountColumn;

			sheet.Cells[startRow - 1, startResultColumn].Value = "№ и/б";
			sheet.Cells[startRow - 1, startResultColumn + 1].Value = "комп.№";
			sheet.Cells[startRow - 1, startResultColumn + 2].Value = "ФИО";
			sheet.Cells[startRow - 1, startResultColumn + 3].Value = "Дата рождения";
			sheet.Cells[startRow - 1, startResultColumn + 4].Value = "Дата поступление в отделение";
			sheet.Cells[startRow - 1, startResultColumn + 5].Value = "Отделение поступления";
			sheet.Cells[startRow - 1, startResultColumn + 6].Value = "Дата нахождения в отделении с";
			sheet.Cells[startRow - 1, startResultColumn + 7].Value = "по";
			sheet.Cells[startRow - 1, startResultColumn + 8].Value = "Отделение";
			var commentColumn = startResultColumn + 9;
			sheet.Cells[startRow - 1, commentColumn].Value = "Комментарий";
			
			var rowsCount = sheet.Dimension.End.Row;

			var visits = await getVisits(begin, end);
			var vis = visits.FirstOrDefault(x => x.Patient.ToString().Contains("ГАПЕЕВА"));
			if (vis != null) {
				int cnt = visits.Count;
			}
			
			var patients = visits.Select(x => x.Patient).Distinct().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.SecondName).ToList();
			Logger.Log.LogDebug($"      за период с {begin.ToShortDateString()} по {end.ToShortDateString()} обнаружено посещение {patients.Count} пациентов");
			Logger.Log.LogInformation($"Сверка данных");
			
			Logger.Log.LogInformation($"      анализ листа:");
			var currentRow = startRow;
			for (var i = 0; i < rowsCount; i++) {
				if (_cancelation.IsCancellationRequested)
					break;

				if (string.IsNullOrEmpty(sheet.Cells[currentRow, lastNameColumn].Text)) {
					Logger.Log.LogInformation($"         обнаружено окончание данных на строке {currentRow}");
					break;
				}

				var item = new AnalyzeItem() {
					LastName = sheet.Cells[currentRow, lastNameColumn].Text.Trim(),
					FirstName = sheet.Cells[currentRow, firstNameColumn].Text.Trim(),
					SecondName = sheet.Cells[currentRow, secondNameColumn].Text.Trim(),
					Birthdate = ParseDate(sheet.Cells[currentRow, birthdateColumn]),
					Code = sheet.Cells[currentRow, codeColumn].Text.Trim(),
					TariffGroup = sheet.Cells[currentRow, tariffGroupColumn].Text.Trim(),
					Service = sheet.Cells[currentRow, serviceColumn].Text.Trim(),
					Date = ParseDate(sheet.Cells[currentRow, dateColumn]),
					Count = ParseInt(sheet.Cells[currentRow, countColumn]),
					Amount = ParseDouble(sheet.Cells[currentRow, amountColumn]),
				};

				Patient patient = null;
				var key = item.ToString();
				if (!_cachePatient.ContainsKey(key)) {
					patient = ComparePatient(item, patients);
					_cachePatient.Add(key, patient);
					if (patient == null)
						Logger.Log.LogInformation($"  не удалось найти пациента {item}");
					else
						Logger.Log.LogInformation($"  найден {patient}");
				} else
					patient = _cachePatient[key];

				item.Patient = patient;
				if (patient == null) { 
					sheet.Cells[currentRow, startResultColumn].Value = $"Пациент '{item}' за период с {begin.AddDays(-expandPeriodDays).ToShortDateString()} по {end.ToShortDateString()} не найден";
					goto next;
				}

				sheet.Cells[currentRow, commentColumn].Value = patient.Comment;

				var fndVisits = AnalyzeVisit(item, visits);
				if (item.Patient.ToString().Contains("АХМЕДОВА")) {
					int cnt = fndVisits.Count;
				}

				if (!fndVisits.Any()) {
					sheet.Cells[currentRow, startResultColumn].Value = $"Посещения пациента за период с {begin.AddDays(-expandPeriodDays).ToShortDateString()} по {end.ToShortDateString()} не найдены";
					goto next;
				}
				var visit = fndVisits.Last();
				if (item.Date < visit.FromDate.Date) {
					sheet.Cells[currentRow, startResultColumn].Value = $"Дата проведения исследования {item.Date.ToShortDateString()} меньше даты поступления {visit.FromDate.ToShortDateString()}!";
					goto next;
				}

				visit = fndVisits.First();
				item.Visit = visit;

				// Если пациент был выписан, проверям через сколько дней был сделан анализ после выписки
				if (visit.DepartmentOut?.OutStatus == true
					&& visit.ToDate.HasValue
					&& visit.ToDate.Value.AddDays(expandPeriodDays) < item.Date) {
					sheet.Cells[currentRow, commentColumn].Value = $"Анализ был сделан по истечении {expandPeriodDays} дней с момента выписки";
				}

				sheet.Cells[currentRow, startResultColumn].Value = item.Visit.Num;
				sheet.Cells[currentRow, startResultColumn + 1].Value = item.Patient.Num;
				sheet.Cells[currentRow, startResultColumn + 2].Value = $"{item.Patient.LastName?.ToUpper()} {item.Patient.FirstName?.ToUpper()} {item.Patient.SecondName?.ToUpper()}";
				SetDate(sheet.Cells[currentRow, startResultColumn + 3], item.Patient.Birthdate);
				SetDate(sheet.Cells[currentRow, startResultColumn + 4], (item.Visit.Comming?.Id ?? 0) == 0 ? item.Visit.FromDate : item.Visit.Comming.FromDate);
				sheet.Cells[currentRow, startResultColumn + 5].Value = item.Visit.DepartmentIn.Name;
				SetDate(sheet.Cells[currentRow, startResultColumn + 6], item.Visit.FromDate);
				SetDate(sheet.Cells[currentRow, startResultColumn + 7], item.Visit?.ToDate);
				sheet.Cells[currentRow, startResultColumn + 8].Value = (item.Visit.Comming?.Id ?? 0) == 0 ? item.Visit.DepartmentOut?.Name 
					: (item.Visit.Comming.DepartmentOut?.StatusDep == 102 ? item.Visit.Comming.DepartmentProfile?.Name : item.Visit.Comming.DepartmentOut?.Name);
				
next:				
				currentRow++;

				if (i != 0 && i % 1000 == 0)
					Logger.Log.LogInformation($"         - обработано строк {i}");
			}
			Logger.Log.LogInformation($"         всего обработано строк {currentRow - startRow}");
		}

		private IList<Visit> AnalyzeVisit(AnalyzeItem item, List<Visit> visits)
		{
			var patientVisits = visits
				.Where(x => x.Patient.Id == item.Patient.Id)
				.Where(x => x.FromDate.Date <= item.Date)
				.OrderByDescending(x => x.FromDate.Date)
				.ToList();

			return patientVisits;
		}

		private static void SetDate(ExcelRange cell, DateTime? date)
		{
			cell.Value = date;
			cell.Style.Numberformat.Format = "dd.mm.yyyy";
		}

		private Patient ComparePatient(AnalyzeItem item, List<Patient> patients)
		{
			if (_cancelation.IsCancellationRequested)
				return null;

			var comment = string.Empty;

			// Четкое сравнение
			var fndPatients = patients.Where(x =>
					String.Compare(x.LastName, item.LastName, StringComparison.CurrentCultureIgnoreCase) == 0
					&& String.Compare(x.FirstName, item.FirstName, StringComparison.CurrentCultureIgnoreCase) == 0
					&& String.Compare(x.SecondName, item.SecondName, StringComparison.CurrentCultureIgnoreCase) == 0
					&& x.Birthdate == item.Birthdate)
				.ToList();

			if (fndPatients.Any()) {
				// поскольку мы не можем определить кого выбирать, выбираем 1го пациента
				if (fndPatients.Count() > 1) {
					Logger.Log.LogInformation($"  в БД найдены пациенты с одинаковым ФИО");
					fndPatients.ForEach(x => {
						comment = $"{comment}\n- {x.ToString()}";
						Logger.Log.LogInformation($"     - {x.ToString()}");
					});

					comment =  $"в БД найдены пациенты с одинаковым ФИО\n{comment}";
				}

			} else {

				// По четкому поиску не нашли, используем нечеткий поиск
				var lev = new Fastenshtein.Levenshtein(item.ToString());

				fndPatients = patients.Select(x => new { lev = lev.DistanceFrom(x.ToShortString()), value = x })
					.Where(x => x.lev < 3)
					.OrderBy(x => x.lev)
					.Select(x => x.value)
					.ToList();

				if (!fndPatients.Any())
					return null;

				if (fndPatients.Count() > 1) {
					Logger.Log.LogInformation($"  в результате нечеткого сравнения найдены пациенты:");
					fndPatients.ForEach(x => {
						comment = $"{comment}\n- {x.ToString()}";
						Logger.Log.LogInformation($"     - {x.ToString()}");
					});

					comment = $"в результате нечеткого сравнения найдены пациенты:\n{comment}";
				}
			}

			var patient = fndPatients.First();
			patient.Comment = comment;
			return patient;
		}

		private static DateTime ParseDate(ExcelRange cell)
		{
			if (!DateTime.TryParseExact(cell.Text, "dd/MM/yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out var date)) {
				Logger.Log.LogWarning($"      неверный формат даты {cell.Text}  в ячейке  {cell.Address}");
			}

			return date;
		}

		private static int ParseInt(ExcelRange cell)
		{
			var value = 0;
			try {
				if (!cell.IsNumeric())
					value = Int32.Parse(cell.Text);
				else
					value = (int) cell.Value;
			} catch (Exception e) {
				Logger.Log.LogWarning($"      ошибка преобразования числа {cell.Text} в ячейке {cell.Address}\n{e.Message}");
			}

			return value;
		}

		private static double ParseDouble(ExcelRange cell)
		{
			var value = 0.0;
			try {
				if (cell.IsNumeric())
					value = Double.Parse(cell.Text);
				else
					value = (double) cell.Value;
			} catch (Exception e) {
				Logger.Log.LogWarning($"      ошибка преобразования числа {cell.Text} в ячейке {cell.Address}\n{e.Message}");
			}

			return value;
		}

		private DateTime _begin;
		private DateTime _end;
		private List<Visit> _visits = new List<Visit>();

		private async Task<List<Visit>> getVisits(DateTime begin, DateTime end)
		{
			if (_begin == begin && _end == end && _visits != null && _visits.Any())
				return _visits;

			Logger.Log.LogInformation($"      получаем пациентов находившихся за период c {begin.ToShortDateString()} по {end.ToShortDateString()} в стационаре");

			// Делаем корректировку периода выборки - анализы могут быть сделаны через несколько дней после выписки, перевода
			begin = begin.AddDays(-Global.Config.Global.ExpandPeriodDays);
			end = end.AddDays(1);

			var start = DateTime.Now;
			Logger.Log.LogDebug($"      обращение к БД: {start}");
			_visits = await Session(s => s.Query<Visit>()
				.Fetch(x => x.DepartmentIn)
				.Fetch(x => x.DepartmentOut)
				.Fetch(x => x.Patient)
				.Fetch(x => x.Comming)
				.Where(x => x.ToDate >= begin && x.FromDate <= end)
				.Where(x => x.VisType == Visit.VisitType.Receipt || x.VisType == Visit.VisitType.Moving)
				.ToList(), _cancelation);
			Logger.Log.LogDebug($"      выбрано строк: {_visits.Count} за {DateTime.Now - start}");
			return _visits;
		}
	}
}