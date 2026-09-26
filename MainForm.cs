using System.Globalization;
using CsvHelper;

namespace KaspiParser;

public class MainForm : Form
{
    private readonly TextBox _txtUrl;
    private readonly NumericUpDown _numMaxPages;
    private readonly NumericUpDown _numDelay;
    private readonly Button _btnStart;
    private readonly Button _btnOpenFolder;
    private readonly TextBox _txtLog;
    private readonly Label _lblStatus;

    private string? _lastCsvPath;
    private CancellationTokenSource? _cts;

    public MainForm()
    {
        Text = "Kaspi Парсер категорий";
        Width = 720;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 420);

        var lblUrl = new Label { Text = "Ссылка на категорию Kaspi:", Left = 12, Top = 15, Width = 300 };
        _txtUrl = new TextBox
        {
            Left = 12,
            Top = 38,
            Width = 680,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "https://kaspi.kz/shop/c/smartphones/"
        };

        var lblPages = new Label { Text = "Макс. страниц:", Left = 12, Top = 72, Width = 100 };
        _numMaxPages = new NumericUpDown { Left = 115, Top = 68, Width = 70, Minimum = 1, Maximum = 200, Value = 20 };

        var lblDelay = new Label { Text = "Задержка (мс):", Left = 200, Top = 72, Width = 100 };
        _numDelay = new NumericUpDown { Left = 305, Top = 68, Width = 80, Minimum = 300, Maximum = 10000, Increment = 100, Value = 1500 };

        _btnStart = new Button { Text = "Старт", Left = 400, Top = 66, Width = 120, Height = 30 };
        _btnStart.Click += BtnStart_Click;

        _btnOpenFolder = new Button { Text = "Открыть CSV", Left = 530, Top = 66, Width = 150, Height = 30, Enabled = false };
        _btnOpenFolder.Click += BtnOpenFolder_Click;

        _lblStatus = new Label
        {
            Text = "Готов к запуску.",
            Left = 12,
            Top = 105,
            Width = 680,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _txtLog = new TextBox
        {
            Left = 12,
            Top = 130,
            Width = 680,
            Height = 380,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Consolas", 9)
        };

        Controls.AddRange(new Control[]
        {
            lblUrl, _txtUrl, lblPages, _numMaxPages, lblDelay, _numDelay,
            _btnStart, _btnOpenFolder, _lblStatus, _txtLog
        });
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        var url = _txtUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http"))
        {
            MessageBox.Show("Введи корректную ссылку на категорию Kaspi (начинается с https://).",
                "Некорректная ссылка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _txtLog.Clear();
        _btnOpenFolder.Enabled = false;
        _btnStart.Enabled = false;
        _btnStart.Text = "Идёт сбор...";
        _lblStatus.Text = "Работаю...";

        _cts = new CancellationTokenSource();

        // Progress<T> вызывает callback на том же потоке, где был создан — то есть на UI-потоке,
        // так что можно спокойно трогать элементы формы прямо внутри без лишнего Invoke.
        var progress = new Progress<string>(message =>
        {
            _txtLog.AppendText(message + Environment.NewLine);
        });

        try
        {
            var products = await Scraper.RunAsync(
                url,
                (int)_numMaxPages.Value,
                (int)_numDelay.Value,
                progress,
                _cts.Token);

            var fileName = $"kaspi_top_products_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            var path = Path.Combine(AppContext.BaseDirectory, fileName);

            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(products);
            }

            _lastCsvPath = path;
            _btnOpenFolder.Enabled = true;
            _lblStatus.Text = $"Готово! Собрано товаров: {products.Count}. Файл: {fileName}";
            _txtLog.AppendText(Environment.NewLine + $"Сохранено в: {path}" + Environment.NewLine);
        }
        catch (OperationCanceledException)
        {
            _lblStatus.Text = "Остановлено пользователем.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Ошибка — см. лог ниже.";
            _txtLog.AppendText(Environment.NewLine + "ОШИБКА: " + ex + Environment.NewLine);
        }
        finally
        {
            _btnStart.Enabled = true;
            _btnStart.Text = "Старт";
        }
    }

    private void BtnOpenFolder_Click(object? sender, EventArgs e)
    {
        if (_lastCsvPath != null && File.Exists(_lastCsvPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_lastCsvPath}\"");
        }
    }
}
