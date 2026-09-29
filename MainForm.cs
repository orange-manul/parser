namespace KaspiParser;

public class MainForm : Form
{
    private readonly TextBox _txtUrl;
    private readonly NumericUpDown _numMaxPages;
    private readonly NumericUpDown _numDelay;
    private readonly NumericUpDown _numMinReviews;
    private readonly TextBox _txtBrands;
    private readonly Button _btnStart;
    private readonly Button _btnOpenExcel;
    private readonly DataGridView _grid;
    private readonly TextBox _txtLog;
    private readonly Label _lblStatus;

    private readonly NumericUpDown _numCheckSellers;
    private readonly NumericUpDown _numMinSellers;
    private string? _lastExcelPath;

    public MainForm()
    {
        Text = "Kaspi Парсер категорий";
        Width = 920;
        Height = 780;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 560);

        var lblUrl = new Label { Text = "Ссылка на категорию Kaspi:", Left = 12, Top = 12, Width = 300 };
        _txtUrl = new TextBox
        {
            Left = 12, Top = 35, Width = 880,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "https://kaspi.kz/shop/c/smartphones/"
        };

        var lblPages = new Label { Text = "Макс. страниц:", Left = 12, Top = 76, Width = 100 };
        _numMaxPages = new NumericUpDown { Left = 115, Top = 72, Width = 70, Minimum = 1, Maximum = 200, Value = 20 };

        var lblDelay = new Label { Text = "Задержка (мс):", Left = 205, Top = 76, Width = 100 };
        _numDelay = new NumericUpDown { Left = 310, Top = 72, Width = 80, Minimum = 300, Maximum = 10000, Increment = 100, Value = 1500 };

        var lblMin = new Label { Text = "Мин. отзывов:", Left = 410, Top = 76, Width = 95 };
        _numMinReviews = new NumericUpDown { Left = 510, Top = 72, Width = 80, Minimum = 0, Maximum = 100000, Value = 0 };

        var lblBrands = new Label { Text = "Исключить бренды:", Left = 12, Top = 108, Width = 125 };
        _txtBrands = new TextBox
        {
            Left = 140, Top = 104, Width = 752,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            // Пример — отредактируй под свою нишу. Через запятую, регистр не важен.
            Text = "Apple, Samsung, Xiaomi, Huawei, Honor, Sony, JBL, Anker, Baseus, Ugreen, Hoco, Remax, Realme"
        };

        var lblCheckSellers = new Label { Text = "Продавцов проверить у топ:", Left = 12, Top = 140, Width = 165 };
        _numCheckSellers = new NumericUpDown { Left = 180, Top = 136, Width = 70, Minimum = 0, Maximum = 200, Value = 30 };

        var lblMinSellers = new Label { Text = "Мин. продавцов:", Left = 260, Top = 140, Width = 110 };
        _numMinSellers = new NumericUpDown { Left = 375, Top = 136, Width = 70, Minimum = 0, Maximum = 50, Value = 2 };

        var lblSellersHint = new Label
        {
            Text = "(0 в \"проверить у топ\" = не проверять; товар с 1 продавцом обычно чей-то собственный бренд)",
            Left = 455, Top = 140, Width = 437,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, 7.5f)
        };

        _btnStart = new Button { Text = "Старт", Left = 12, Top = 169, Width = 130, Height = 32 };
        _btnStart.Click += BtnStart_Click;

        _btnOpenExcel = new Button { Text = "Открыть Excel", Left = 152, Top = 169, Width = 170, Height = 32, Enabled = false };
        _btnOpenExcel.Click += BtnOpenExcel_Click;

        _lblStatus = new Label
        {
            Text = "Готов к запуску.",
            Left = 335, Top = 177, Width = 555,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _grid = new DataGridView
        {
            Left = 12, Top = 212, Width = 880, Height = 316,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "№", DataPropertyName = "Rank", Width = 45 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Название", DataPropertyName = "Title",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 100
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Цена, ₸", DataPropertyName = "Price", Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Рейтинг", DataPropertyName = "Rating", Width = 70,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "0.0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Отзывов", DataPropertyName = "ReviewsCount", Width = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Спрос", DataPropertyName = "Demand", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Продавцов", DataPropertyName = "SellerCount", Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, NullValue = "—" }
        });
        _grid.CellDoubleClick += Grid_CellDoubleClick;

        _txtLog = new TextBox
        {
            Left = 12, Top = 538, Width = 880, Height = 195,
            Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = new Font("Consolas", 9)
        };

        Controls.AddRange(new Control[]
        {
            lblUrl, _txtUrl, lblPages, _numMaxPages, lblDelay, _numDelay, lblMin, _numMinReviews, lblBrands, _txtBrands,
            lblCheckSellers, _numCheckSellers, lblMinSellers, _numMinSellers, lblSellersHint,
            _btnStart, _btnOpenExcel, _lblStatus, _grid, _txtLog
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
        _grid.DataSource = null;
        _btnOpenExcel.Enabled = false;
        _btnStart.Enabled = false;
        _btnStart.Text = "Идёт сбор...";
        _lblStatus.Text = "Работаю...";

        // Progress<T> вызывает callback на UI-потоке, поэтому Invoke не нужен.
        var progress = new Progress<string>(message => _txtLog.AppendText(message + Environment.NewLine));

        try
        {
            var raw = await Scraper.RunAsync(url, (int)_numMaxPages.Value, (int)_numDelay.Value, progress);
            var brands = _txtBrands.Text.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var products = Scraper.Finalize(raw, (int)_numMinReviews.Value, brands);

            int unique = raw.Select(r => r.Url).Distinct().Count();
            ((IProgress<string>)progress).Report(
                $"Скрыто фильтрами (бренды, мин. отзывов): {unique - products.Count} из {unique}");

            int checkTop = (int)_numCheckSellers.Value;
            int minSellers = (int)_numMinSellers.Value;

            if (checkTop > 0)
            {
                await Scraper.FetchSellerCountsAsync(products, checkTop, (int)_numDelay.Value, progress);

                // Оставляем только те товары, которых реально проверили на число
                // продавцов (топ N), иначе непроверенные ниже топа будут выглядеть
                // как прошедшие фильтр, хотя по ним просто нет данных.
                products = products.Take(checkTop).ToList();

                if (minSellers > 0)
                {
                    int before = products.Count;
                    products = products.Where(p => p.SellerCount is null || p.SellerCount >= minSellers).ToList();
                    ((IProgress<string>)progress).Report(
                        $"Скрыто как вероятный собственный бренд (продавцов < {minSellers}): {before - products.Count}");
                }
            }

            _grid.DataSource = products;
            ColorizeRows();

            if (products.Count == 0)
            {
                _lblStatus.Text = "Товары не найдены — проверь ссылку и селекторы (см. лог).";
                return;
            }

            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KaspiParser");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"kaspi_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx");

            ExcelExporter.Export(products, url, path);

            _lastExcelPath = path;
            _btnOpenExcel.Enabled = true;

            int hot = products.Count(p => p.Demand == "Очень высокий" || p.Demand == "Высокий");
            _lblStatus.Text = $"Готово: {products.Count} товаров, с высоким спросом: {hot}. Excel сохранён.";
            _txtLog.AppendText(Environment.NewLine + $"Сохранено: {path}" + Environment.NewLine);
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

    private void ColorizeRows()
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is not Product p) continue;
            row.DefaultCellStyle.BackColor = p.Demand switch
            {
                "Очень высокий" => Color.FromArgb(99, 190, 123),
                "Высокий" => Color.FromArgb(198, 239, 206),
                "Средний" => Color.FromArgb(255, 235, 156),
                "Нет отзывов" => Color.FromArgb(217, 217, 217),
                _ => Color.White
            };
        }
    }

    // Двойной клик по строке открывает товар на Kaspi в браузере
    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_grid.Rows[e.RowIndex].DataBoundItem is Product p && !string.IsNullOrEmpty(p.Url))
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(p.Url) { UseShellExecute = true });
        }
    }

    private void BtnOpenExcel_Click(object? sender, EventArgs e)
    {
        if (_lastExcelPath != null && File.Exists(_lastExcelPath))
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(_lastExcelPath) { UseShellExecute = true });
        }
    }
}
