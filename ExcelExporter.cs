using ClosedXML.Excel;

namespace KaspiParser;

public static class ExcelExporter
{
    public static void Export(IReadOnlyList<Product> products, string categoryUrl, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Товары");

        string[] headers = { "№", "Название", "Цена, ₸", "Рейтинг", "Отзывов", "Спрос", "Продавцов", "Ссылка" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 2;
        foreach (var p in products)
        {
            ws.Cell(row, 1).Value = p.Rank;
            ws.Cell(row, 2).Value = p.Title;
            ws.Cell(row, 3).Value = p.Price;
            ws.Cell(row, 4).Value = p.Rating;
            ws.Cell(row, 5).Value = p.ReviewsCount;
            ws.Cell(row, 6).Value = p.Demand;
            ws.Cell(row, 7).Value = p.SellerCount.HasValue ? p.SellerCount.Value.ToString() : "—";

            var linkCell = ws.Cell(row, 8);
            linkCell.Value = "Открыть";
            if (Uri.TryCreate(p.Url, UriKind.Absolute, out var uri))
                linkCell.SetHyperlink(new XLHyperlink(uri));

            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

            // Подсветка уровня спроса (колонки "Отзывов" и "Спрос")
            var fill = p.Demand switch
            {
                "Очень высокий" => "#63BE7B",
                "Высокий" => "#C6EFCE",
                "Средний" => "#FFEB9C",
                "Нет отзывов" => "#D9D9D9",
                _ => null
            };
            if (fill != null)
                ws.Range(row, 5, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml(fill);

            row++;
        }

        if (products.Count > 0)
            ws.Range(1, 1, row - 1, headers.Length).SetAutoFilter();

        ws.SheetView.FreezeRows(1);

        ws.Column(1).Width = 6;
        ws.Column(2).Width = 65;
        ws.Column(3).Width = 14;
        ws.Column(4).Width = 10;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 18;
        ws.Column(7).Width = 12;
        ws.Column(8).Width = 12;

        // Вторая вкладка — справка, чтобы через месяц было понятно, откуда данные
        var info = wb.Worksheets.Add("Инфо");
        info.Cell(1, 1).Value = "Категория";
        info.Cell(1, 2).Value = categoryUrl;
        info.Cell(2, 1).Value = "Дата сбора";
        info.Cell(2, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        info.Cell(3, 1).Value = "Товаров";
        info.Cell(3, 2).Value = products.Count;
        info.Cell(5, 1).Value = "Как читать колонку «Спрос»";
        info.Cell(6, 1).Value =
            "Kaspi не показывает точное число продаж. Спрос — оценка по количеству отзывов " +
            "относительно других товаров этой категории: топ-10% — очень высокий, " +
            "следующие 20% — высокий, следующие 30% — средний, остальные — низкий.";
        info.Range(1, 1, 3, 1).Style.Font.Bold = true;
        info.Cell(5, 1).Style.Font.Bold = true;
        info.Column(1).Width = 28;
        info.Column(2).Width = 70;

        wb.SaveAs(path);
    }
}
