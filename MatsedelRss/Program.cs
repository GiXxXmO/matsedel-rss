using System.ServiceModel.Syndication;
using System.Xml;
using HtmlAgilityPack;
using System.Text;
using System.Globalization;

var scraper = new MatsedelScraper();
await scraper.RunAsync();

public class MatsedelScraper
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://www.skara.se";
    
    public MatsedelScraper()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }
    
    public async Task RunAsync()
    {
        Console.WriteLine("Hämtar matsedel från Skara kommun...");
        
        // Hämta aktuell månads matsedel
        var currentMonth = DateTime.Now;
        var menuData = await GetMenuForMonthAsync(currentMonth);
        
        // Om vi är i slutet av månaden, försök också hämta nästa månads matsedel
        var daysLeftInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month) - currentMonth.Day;
        if (daysLeftInMonth <= 7)
        {
            Console.WriteLine("\nKontrollerar om nästa månads matsedel finns tillgänglig...");
            var nextMonth = currentMonth.AddMonths(1);
            var nextMonthMenu = await GetMenuForMonthAsync(nextMonth);
            
            // Slå ihop matsedlarna
            foreach (var item in nextMonthMenu)
            {
                menuData[item.Key] = item.Value;
            }
            
            if (nextMonthMenu.Count > 0)
            {
                Console.WriteLine($"Hämtade {nextMonthMenu.Count} menyer från nästa månad.");
            }
        }
        
        if (menuData.Count == 0)
        {
            Console.WriteLine("Kunde inte hämta matsedel.");
            return;
        }
        
        // Skapa output-katalog
        Directory.CreateDirectory("output");
        
        // Generera RSS-feeds
        GenerateWeeklyFeed(menuData, "output/matsedel-vecka.xml");
        GenerateDailyFeed(menuData, "output/matsedel-dagens.xml");
        GenerateAllDaysFeed(menuData, "output/matsedel-alla-dagar.xml");
        
        Console.WriteLine($"\nRSS-feeds skapade i output-katalogen.");
        Console.WriteLine($"Totalt {menuData.Count} menyer hittades.");
    }
    
    private async Task<Dictionary<DateTime, MenuDay>> GetMenuForMonthAsync(DateTime month)
    {
        var menuData = new Dictionary<DateTime, MenuDay>();
        
        // Försök olika URL-format för att hitta matsedeln
        var monthNames = new[] 
        { 
            month.ToString("MMMM", new CultureInfo("sv-SE")).ToLower(),
            month.ToString("MMMM", CultureInfo.InvariantCulture).ToLower()
        };
        
        string? html = null;
        string? usedUrl = null;
        
        // Försök olika URL-kombinationer
        foreach (var monthName in monthNames)
        {
            var urls = new[]
            {
                $"{BaseUrl}/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/matsedelfor{monthName}.{GetPageId(month)}.html",
                $"{BaseUrl}/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/matsedelfor{monthName}.html"
            };
            
            foreach (var url in urls)
            {
                try
                {
                    Console.WriteLine($"Försöker URL: {url}");
                    html = await _httpClient.GetStringAsync(url);
                    usedUrl = url;
                    Console.WriteLine($"Lyckades hämta sida från: {url}");
                    break;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Kunde inte hämta {url}: {ex.Message}");
                }
            }
            
            if (html != null) break;
        }
        
        if (html == null)
        {
            Console.WriteLine("Kunde inte hitta matsedel för aktuell månad.");
            return menuData;
        }
        
        // Parsa HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        // Försök hitta matsedelsinformation i olika format
        // Vanligt format: tabell eller lista med datum och maträtter
        
        // Försök 1: Leta efter tabeller
        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables != null)
        {
            foreach (var table in tables)
            {
                ParseTable(table, menuData, month);
            }
        }
        
        // Försök 2: Leta efter listor med datum
        var dateHeaders = doc.DocumentNode.SelectNodes("//h2 | //h3 | //h4 | //strong");
        if (dateHeaders != null)
        {
            ParseDateHeaders(dateHeaders, menuData, month);
        }
        
        return menuData;
    }
    
    private void ParseTable(HtmlNode table, Dictionary<DateTime, MenuDay> menuData, DateTime month)
    {
        var rows = table.SelectNodes(".//tr");
        if (rows == null) return;
        
        foreach (var row in rows)
        {
            var cells = row.SelectNodes(".//td | .//th");
            if (cells == null || cells.Count < 2) continue;
            
            var dateText = cells[0].InnerText.Trim();
            var menuText = cells[1].InnerText.Trim();
            
            if (TryParseDate(dateText, month, out var date))
            {
                menuData[date] = new MenuDay
                {
                    Date = date,
                    MainDish = menuText,
                    DayName = date.ToString("dddd", new CultureInfo("sv-SE"))
                };
            }
        }
    }
    
    private void ParseDateHeaders(HtmlNodeCollection headers, Dictionary<DateTime, MenuDay> menuData, DateTime month)
    {
        foreach (var header in headers)
        {
            var headerText = header.InnerText.Trim();
            
            if (TryParseDate(headerText, month, out var date))
            {
                var menuText = "";
                var nextNode = header.NextSibling;
                
                while (nextNode != null)
                {
                    if (nextNode.NodeType == HtmlNodeType.Text)
                    {
                        menuText += nextNode.InnerText.Trim() + " ";
                    }
                    else if (nextNode.NodeType == HtmlNodeType.Element)
                    {
                        if (nextNode.Name == "p" || nextNode.Name == "div")
                        {
                            menuText += nextNode.InnerText.Trim() + " ";
                        }
                        else if (nextNode.Name == "h2" || nextNode.Name == "h3" || nextNode.Name == "h4")
                        {
                            break; // Nästa datum hittades
                        }
                    }
                    
                    nextNode = nextNode.NextSibling;
                    if (!string.IsNullOrWhiteSpace(menuText) && menuText.Length > 20) break;
                }
                
                if (!string.IsNullOrWhiteSpace(menuText))
                {
                    menuData[date] = new MenuDay
                    {
                        Date = date,
                        MainDish = menuText.Trim(),
                        DayName = date.ToString("dddd", new CultureInfo("sv-SE"))
                    };
                }
            }
        }
    }
    
    private bool TryParseDate(string text, DateTime referenceMonth, out DateTime date)
    {
        date = DateTime.MinValue;
        
        // Ta bort extra whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        
        // Försök olika datum-format
        var formats = new[]
        {
            "dddd d MMMM",
            "dddd d/M",
            "d MMMM",
            "d/M",
            "dd/MM",
            "yyyy-MM-dd"
        };
        
        var culture = new CultureInfo("sv-SE");
        
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(text, format, culture, DateTimeStyles.None, out date))
            {
                // Om året saknas, använd referensmånaden
                if (date.Year == 1)
                {
                    date = new DateTime(referenceMonth.Year, date.Month, date.Day);
                }
                
                return true;
            }
        }
        
        // Försök generell parsning
        if (DateTime.TryParse(text, culture, DateTimeStyles.None, out date))
        {
            if (date.Year == 1)
            {
                date = new DateTime(referenceMonth.Year, date.Month, date.Day);
            }
            return true;
        }
        
        return false;
    }
    
    private string GetPageId(DateTime month)
    {
        // Generera ett page ID baserat på månad (detta är en gissning, kan behöva justeras)
        var baseId = 3672;
        var monthOffset = (month.Year - 2025) * 12 + (month.Month - 2);
        return (baseId + monthOffset).ToString();
    }
    
    private void GenerateWeeklyFeed(Dictionary<DateTime, MenuDay> menuData, string outputPath)
    {
        var feed = new SyndicationFeed(
            "Matsedel Skara - Veckovy",
            "Matsedel för veckan från Skara kommun",
            new Uri("https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/"),
            "matsedel-vecka",
            DateTime.Now
        );
        
        var items = new List<SyndicationItem>();
        
        // Gruppera efter vecka
        var weeks = menuData.GroupBy(m => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            m.Key, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));
        
        foreach (var week in weeks.OrderBy(w => w.Key))
        {
            var weekDays = week.OrderBy(d => d.Key).ToList();
            var weekStart = weekDays.First().Key;
            var weekEnd = weekDays.Last().Key;
            
            var description = new StringBuilder();
            description.AppendLine($"<h3>Vecka {week.Key}: {weekStart:d MMM} - {weekEnd:d MMM}</h3>");
            description.AppendLine("<ul>");
            
            foreach (var day in weekDays)
            {
                description.AppendLine($"<li><strong>{day.Value.DayName} {day.Key:d/M}:</strong> {day.Value.MainDish}</li>");
            }
            
            description.AppendLine("</ul>");
            
            var item = new SyndicationItem(
                $"Matsedel vecka {week.Key}",
                description.ToString(),
                new Uri($"https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/#week{week.Key}"),
                $"week-{week.Key}-{weekStart.Year}",
                weekStart
            );
            
            items.Add(item);
        }
        
        feed.Items = items;
        
        using var writer = XmlWriter.Create(outputPath, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(writer);
        
        Console.WriteLine($"Veckomatsedel sparad: {outputPath}");
    }
    
    private void GenerateDailyFeed(Dictionary<DateTime, MenuDay> menuData, string outputPath)
    {
        var today = DateTime.Today;
        var todayMenu = menuData.FirstOrDefault(m => m.Key.Date == today);
        
        var feed = new SyndicationFeed(
            "Matsedel Skara - Dagens",
            "Dagens matsedel från Skara kommun",
            new Uri("https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/"),
            "matsedel-dagens",
            DateTime.Now
        );
        
        var items = new List<SyndicationItem>();
        
        if (todayMenu.Key != DateTime.MinValue)
        {
            var description = new StringBuilder();
            description.AppendLine($"<h3>{todayMenu.Value.DayName} {todayMenu.Key:d MMMM yyyy}</h3>");
            description.AppendLine($"<p>{todayMenu.Value.MainDish}</p>");
            
            var item = new SyndicationItem(
                $"Dagens lunch - {todayMenu.Value.DayName} {todayMenu.Key:d/M}",
                description.ToString(),
                new Uri($"https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/#day{todayMenu.Key:yyyyMMdd}"),
                $"day-{todayMenu.Key:yyyyMMdd}",
                todayMenu.Key
            );
            
            items.Add(item);
        }
        else
        {
            // Ingen mat idag (helg eller lov)
            var item = new SyndicationItem(
                "Ingen lunch idag",
                "<p>Ingen matsedel tillgänglig för idag.</p>",
                new Uri("https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/"),
                $"no-menu-{today:yyyyMMdd}",
                today
            );
            
            items.Add(item);
        }
        
        feed.Items = items;
        
        using var writer = XmlWriter.Create(outputPath, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(writer);
        
        Console.WriteLine($"Dagens matsedel sparad: {outputPath}");
    }
    
    private void GenerateAllDaysFeed(Dictionary<DateTime, MenuDay> menuData, string outputPath)
    {
        var feed = new SyndicationFeed(
            "Matsedel Skara - Alla dagar",
            "Fullständig matsedel från Skara kommun",
            new Uri("https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/"),
            "matsedel-alla-dagar",
            DateTime.Now
        );
        
        var items = new List<SyndicationItem>();
        
        // Sortera efter datum, visa bara framtida och dagens mat
        var relevantDays = menuData
            .Where(m => m.Key.Date >= DateTime.Today)
            .OrderBy(m => m.Key)
            .Take(30); // Max 30 dagar framåt
        
        foreach (var day in relevantDays)
        {
            var description = new StringBuilder();
            description.AppendLine($"<h3>{day.Value.DayName} {day.Key:d MMMM yyyy}</h3>");
            description.AppendLine($"<p>{day.Value.MainDish}</p>");
            
            var item = new SyndicationItem(
                $"{day.Value.DayName} {day.Key:d/M} - {day.Value.MainDish.Substring(0, Math.Min(50, day.Value.MainDish.Length))}...",
                description.ToString(),
                new Uri($"https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/#day{day.Key:yyyyMMdd}"),
                $"day-{day.Key:yyyyMMdd}",
                day.Key
            );
            
            items.Add(item);
        }
        
        feed.Items = items;
        
        using var writer = XmlWriter.Create(outputPath, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(writer);
        
        Console.WriteLine($"Alla dagars matsedel sparad: {outputPath}");
    }
}

public class MenuDay
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = "";
    public string MainDish { get; set; } = "";
}
