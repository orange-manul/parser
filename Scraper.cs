using Microsoft.Playwright;
using System.Globalization;

namespace KaspiParser;

public class Product
{
    public string Title { get; set; } = "";
    public int Price { get; set; }
    public decimal Rating { get; set; }
    public int ReviewsCount { get; set; }
    public string Url { get; set; } = "";

    // Заполняются в Scraper.Finalize после сортировки
    public int Rank { get; set; }
    public string Demand { get; set; } = "";
}

/// <summary>
/// Вся логика скрейпинга категории Kaspi вынесена сюда — этим классом пользуется
/// и консоль (если понадобится), и GUI-форма. Сообщения о ходе работы отдаются
/// через IProgress&lt;string&gt;, чтобы форма могла показывать их в текстовом логе.
/// </summary>
public static class Scraper
{
    // ВАЖНО: селекторы ниже нужно сверять с реальной разметкой Kaspi через
    // "Просмотреть код" (Inspect) в браузере — сайт может поменять вёрстку.
    private const string SelectorProductCard = ".item-card";
    private const string SelectorTitle = ".item-card__name";
    private const string SelectorPrice = ".item-card__prices-price";
    private const string SelectorRating = ".item-card__rating";
    private const string SelectorReviews = ".item-card__rating-count";
    private const string SelectorNextPage = "li.pagination__el:has-text('Следующая')";

    public static async Task<List<Product>> RunAsync(
        string categoryUrl,
        int maxPages,
        int delayMs,
        IProgress<string> progress,
        CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                        "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            Locale = "ru-RU",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();

        progress.Report($"Открываю категорию: {categoryUrl}");
        await page.GotoAsync(categoryUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForTimeoutAsync(delayMs);

        for (int pageNum = 1; pageNum <= maxPages; pageNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report($"Страница {pageNum}...");
            await page.WaitForTimeoutAsync(delayMs);

            var cards = await page.QuerySelectorAllAsync(SelectorProductCard);
            progress.Report($"  Найдено карточек: {cards.Count}");

            foreach (var card in cards)
            {
                try
                {
                    var titleEl = await card.QuerySelectorAsync(SelectorTitle);
                    var priceEl = await card.QuerySelectorAsync(SelectorPrice);
                    var ratingEl = await card.QuerySelectorAsync(SelectorRating);
                    var reviewsEl = await card.QuerySelectorAsync(SelectorReviews);
                    var linkEl = await card.QuerySelectorAsync("a");

                    string title = titleEl != null ? (await titleEl.InnerTextAsync()).Trim() : "—";
                    string priceText = priceEl != null ? (await priceEl.InnerTextAsync()).Trim() : "0";
                    string ratingText = ratingEl != null ? (await ratingEl.InnerTextAsync()).Trim() : "0";
                    string reviewsText = reviewsEl != null ? (await reviewsEl.InnerTextAsync()).Trim() : "0";
                    string url = linkEl != null ? await linkEl.GetAttributeAsync("href") ?? "" : "";

                    products.Add(new Product
                    {
                        Title = title,
                        Price = CleanNumber(priceText),
                        Rating = CleanDecimal(ratingText),
                        ReviewsCount = CleanNumber(reviewsText),
                        Url = url.StartsWith("http") ? url : $"https://kaspi.kz{url}"
                    });
                }
                catch (Exception ex)
                {
                    progress.Report($"  Пропустил карточку из-за ошибки: {ex.Message}");
                }
            }

            var nextButton = page.Locator(SelectorNextPage);
            bool hasNext = await nextButton.CountAsync() > 0 && await nextButton.First.IsVisibleAsync();
            bool isDisabled = hasNext && (
                await nextButton.First.GetAttributeAsync("disabled") != null ||
                (await nextButton.First.GetAttributeAsync("class") ?? "").Contains("disabled")
            );

            if (!hasNext || isDisabled)
            {
                progress.Report("Кнопки \"Следующая\" больше нет — это последняя страница.");
                break;
            }

            // Обычный ClickAsync() у Playwright требует, чтобы элемент был виден
            // в вьюпорте — на Kaspi это иногда виснет из-за sticky-элементов.
            // JS-клик надёжнее.
            await nextButton.First.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
            await page.WaitForTimeoutAsync(300);
            await nextButton.First.EvaluateAsync("el => el.click()");
            await page.WaitForTimeoutAsync(delayMs);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        progress.Report($"Готово. Всего собрано карточек: {products.Count}");
        return products;
    }

    /// <summary>
    /// Убирает дубли (по ссылке), отсекает товары с числом отзывов меньше minReviews,
    /// сортирует по отзывам и присваивает место в рейтинге и уровень спроса.
    /// Kaspi не показывает точное число продаж, поэтому спрос — это оценка
    /// по количеству отзывов относительно остальных товаров категории:
    /// топ-10% — очень высокий, следующие 20% — высокий, следующие 30% — средний, остальные — низкий.
    /// </summary>
    public static List<Product> Finalize(List<Product> raw, int minReviews)
    {
        var sorted = raw
            .GroupBy(p => p.Url)
            .Select(g => g.First())
            .Where(p => p.ReviewsCount >= minReviews)
            .OrderByDescending(p => p.ReviewsCount)
            .ThenByDescending(p => p.Rating)
            .ToList();

        int withReviews = sorted.Count(p => p.ReviewsCount > 0);

        for (int i = 0; i < sorted.Count; i++)
        {
            var p = sorted[i];
            p.Rank = i + 1;

            if (p.ReviewsCount == 0)
            {
                p.Demand = "Нет отзывов";
                continue;
            }

            double position = (double)i / withReviews;
            p.Demand = position < 0.10 ? "Очень высокий"
                     : position < 0.30 ? "Высокий"
                     : position < 0.60 ? "Средний"
                     : "Низкий";
        }

        return sorted;
    }

    private static int CleanNumber(string raw)
    {
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return digits.Length > 0 ? int.Parse(digits) : 0;
    }

    private static decimal CleanDecimal(string raw)
    {
        var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
            .Replace(',', '.');
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }
}
