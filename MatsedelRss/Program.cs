using System.ServiceModel.Syndication;
using System.Xml;
using HtmlAgilityPack;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Globalization;

var scraper = new MatsedelScraper();
await scraper.RunAsync();

public class MatsedelScraper
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://www.skara.se";
    private const string PushIdFile = "pushid.json";
    private const int DefaultPushId = 3671;
    private const int Tries = 10;
    
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

        // Läs senaste sparade pushID (om finns) och testa sekventiellt
        var savedId = LoadLastPushId() ?? DefaultPushId;

        foreach (var monthName in monthNames)
        {
            bool found = false;

            for (int offset = 0; offset < Tries; offset++)
            {
                var tryId = savedId + offset;
                var urlWithId = $"{BaseUrl}/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/matsedelfor{monthName}.{tryId}.html";

                try
                {
                    Console.WriteLine($"Försöker URL: {urlWithId}");
                    html = await _httpClient.GetStringAsync(urlWithId);
                    usedUrl = urlWithId;
                    Console.WriteLine($"Lyckades hämta sida från: {urlWithId}");
                    SaveLastPushId(tryId);
                    found = true;
                    break;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Kunde inte hämta {urlWithId}: {ex.Message}");
                }
            }

            if (found) break;

            // Fallback: testa utan id
            var urlNoId = $"{BaseUrl}/forskolaskolaochforskola/matsedelforskolaochskola/matsedelfor{monthName}.html";
            try
            {
                Console.WriteLine($"Försöker URL: {urlNoId}");
                html = await _httpClient.GetStringAsync(urlNoId);
                usedUrl = urlNoId;
                Console.WriteLine($"Lyckades hämta sida från: {urlNoId}");
                break;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Kunde inte hämta {urlNoId}: {ex.Message}");
            }
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

            // Hämta alla textnoder och element från menycellen
            var menuCell = cells[1];
            var menuParts = new List<string>();

            // Kolla om det finns flera p-element eller br-taggar i cellen
            var paragraphs = menuCell.SelectNodes(".//p | .//div");
            if (paragraphs != null && paragraphs.Count > 1)
            {
                // Dela upp per paragraph eller div
                foreach (var p in paragraphs)
                {
                    var text = p.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text) && !menuParts.Contains(text))
                    {
                        menuParts.Add(text);
                    }
                }
            }

            if (menuParts.Count == 0)
            {
                // Ingen struktur, försök dela upp på "Veg;" eller "Veg:"
                var menuText = menuCell.InnerText.Trim();

                // Olika mönster för att hitta Veg-alternativet
                // Matchar: "...grönsakerVeg; Thaigryta..." eller "...grönsaker Veg: Thaigryta..."
                var vegPattern = System.Text.RegularExpressions.Regex.Match(
                    menuText, 
                    @"^(.*?)\s*[Vv]eg[;:]\s*(.+)$", 
                    System.Text.RegularExpressions.RegexOptions.Singleline
                );

                if (vegPattern.Success)
                {
                    menuParts.Add(vegPattern.Groups[1].Value.Trim());
                    menuParts.Add(vegPattern.Groups[2].Value.Trim());
                }
                else
                {
                    menuParts.Add(menuText);
                }
            }

            if (TryParseDate(dateText, month, out var date))
            {
                // Formatera med radbrytningar
                var mainDish = menuParts.Count > 0 ? menuParts[0] : "";
                if (menuParts.Count > 1)
                {
                    mainDish += "<br/>Veg: " + menuParts[1];
                }
                for (int i = 2; i < menuParts.Count; i++)
                {
                    mainDish += "<br/>" + menuParts[i];
                }

                menuData[date] = new MenuDay
                {
                    Date = date,
                    MainDish = mainDish,
                    VegetarianDish = menuParts.Count > 1 ? menuParts[1] : "",
                    DayName = date.ToString("dddd", new CultureInfo("sv-SE"))
                };

                Console.WriteLine($"ParseTable: {date:yyyy-MM-dd} - Delar: {menuParts.Count} - {mainDish}");
            }
        }
    }
    
    private void ParseDateHeaders(HtmlNodeCollection headers, Dictionary<DateTime, MenuDay> menuData, DateTime month)
    {
        var culture = new CultureInfo("sv-SE");
        var dayNames = culture.DateTimeFormat.DayNames.Concat(culture.DateTimeFormat.AbbreviatedDayNames).ToArray();

        // Håll koll på aktuellt veckonummer och veckotext för att beräkna datum
        string currentWeekText = "";
        int currentWeekNumber = 0;

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var headerText = header.InnerText.Trim();

            Console.WriteLine($"Analyserar header [{i}]: {headerText}");

            // Kolla om detta är en vecka-header
            var weekMatch = System.Text.RegularExpressions.Regex.Match(headerText, @"[Vv]ecka\s+(\d+)");
            if (weekMatch.Success)
            {
                currentWeekText = headerText;
                currentWeekNumber = int.Parse(weekMatch.Groups[1].Value);
                Console.WriteLine($"  Hittade vecka: {currentWeekNumber}");
                continue;
            }

            // Kolla om detta är en veckodag
            bool isWeekday = dayNames.Any(d => !string.IsNullOrEmpty(d) && 
                headerText.Equals(d, StringComparison.OrdinalIgnoreCase));

            if (isWeekday)
            {
                Console.WriteLine($"  Detta är en veckodag: {headerText}");
                DateTime date = DateTime.MinValue;

                // Försök hitta datum i samma header eller föregående
                if (TryParseDate(headerText, month, out date))
                {
                    Console.WriteLine($"  Datum fanns i header: {date:yyyy-MM-dd}");
                }
                else if (currentWeekNumber > 0)
                {
                    // Använd aktuellt veckonummer för att beräkna datum
                    date = FindDateForWeekday(headerText, currentWeekText, month);
                    Console.WriteLine($"  Beräknat datum från vecka {currentWeekNumber}: {date:yyyy-MM-dd}");
                }
                else if (i > 0)
                {
                    // Kolla föregående header för veckonummer eller datum
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var prevHeader = headers[j];
                        var prevText = prevHeader.InnerText.Trim();

                        if (prevText.Contains("Vecka") || prevText.Contains("vecka"))
                        {
                            date = FindDateForWeekday(headerText, prevText, month);
                            Console.WriteLine($"  Beräknat datum från tidigare vecko-header: {date:yyyy-MM-dd}");
                            break;
                        }
                        else if (TryParseDate(prevText, month, out date))
                        {
                            Console.WriteLine($"  Datum från tidigare header: {date:yyyy-MM-dd}");
                            break;
                        }
                    }
                }

                if (date == DateTime.MinValue)
                {
                    Console.WriteLine($"  Kunde inte hitta datum för {headerText}, hoppar över");
                    continue;
                }

                // Nu har vi datum och veckodag, samla maträtter
                var dishes = new List<string>();
                var nextNode = header.NextSibling;
                int dishCount = 0;

                while (nextNode != null && dishCount < 10) // Max 10 rätter per dag
                {
                    // Stoppa vid nästa header (nästa veckodag)
                    if (nextNode.NodeType == HtmlNodeType.Element)
                    {
                        if (nextNode.Name == "h2" || nextNode.Name == "h3" || nextNode.Name == "h4" || nextNode.Name == "strong")
                        {
                            var nextHeaderText = nextNode.InnerText.Trim();
                            // Kolla om det är nästa veckodag eller vecka
                            if (dayNames.Any(d => !string.IsNullOrEmpty(d) && nextHeaderText.Equals(d, StringComparison.OrdinalIgnoreCase)) ||
                                nextHeaderText.Contains("Vecka") || nextHeaderText.Contains("vecka"))
                            {
                                Console.WriteLine($"  Stoppar vid nästa header: {nextHeaderText}");
                                break;
                            }
                        }

                        // Samla text från p, div, eller textnoder
                        if (nextNode.Name == "p" || nextNode.Name == "div" || nextNode.Name == "li")
                        {
                            var dishText = nextNode.InnerText.Trim();
                            if (!string.IsNullOrWhiteSpace(dishText))
                            {
                                dishes.Add(dishText);
                                dishCount++;
                                Console.WriteLine($"  Hittade rätt {dishCount}: {dishText}");
                            }
                        }
                    }
                    else if (nextNode.NodeType == HtmlNodeType.Text)
                    {
                        var dishText = nextNode.InnerText.Trim();
                        if (!string.IsNullOrWhiteSpace(dishText) && dishText.Length > 3)
                        {
                            dishes.Add(dishText);
                            dishCount++;
                            Console.WriteLine($"  Hittade rätt {dishCount}: {dishText}");
                        }
                    }

                    nextNode = nextNode.NextSibling;
                }

                if (dishes.Count > 0)
                {
                    // Om första rätten innehåller "Veg;" - dela upp den
                    if (dishes.Count == 1 && dishes[0].Contains("Veg;"))
                    {
                        var vegPattern = System.Text.RegularExpressions.Regex.Match(
                            dishes[0], 
                            @"^(.*?)\s*[Vv]eg[;:]\s*(.+)$", 
                            System.Text.RegularExpressions.RegexOptions.Singleline
                        );

                        if (vegPattern.Success)
                        {
                            dishes.Clear();
                            dishes.Add(vegPattern.Groups[1].Value.Trim());
                            dishes.Add(vegPattern.Groups[2].Value.Trim());
                        }
                    }

                    // Formatera rätter: första är huvudrätt, andra är vegetariskt
                    var mainDish = dishes[0];
                    if (dishes.Count > 1)
                    {
                        mainDish += "<br/>Veg: " + dishes[1];
                    }

                    // Lägg till eventuella fler rätter
                    for (int j = 2; j < dishes.Count; j++)
                    {
                        mainDish += "<br/>" + dishes[j];
                    }

                    menuData[date] = new MenuDay
                    {
                        Date = date,
                        MainDish = mainDish,
                        VegetarianDish = dishes.Count > 1 ? dishes[1] : "",
                        DayName = date.ToString("dddd", new CultureInfo("sv-SE"))
                    };

                    Console.WriteLine($"  Sparade meny för {date:yyyy-MM-dd}: {mainDish}");
                }
                else
                {
                    Console.WriteLine($"  Hittade inga rätter för {headerText} {date:yyyy-MM-dd}");
                }
            }
            else if (TryParseDate(headerText, month, out var date))
            {
                // Gamla parsningslogiken för datum i headers
                Console.WriteLine($"  Detta är ett datum: {date:yyyy-MM-dd}");
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
                            break;
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
                    Console.WriteLine($"  Sparade meny för datum-header: {date:yyyy-MM-dd}");
                }
            }
        }
    }

    private DateTime FindDateForWeekday(string weekdayName, string weekText, DateTime month)
    {
        // Försök extrahera datumintervall från "Vecka X (datum - datum)" eller liknande format
        var culture = new CultureInfo("sv-SE");

        // Extrahera veckonummer
        var weekMatch = System.Text.RegularExpressions.Regex.Match(weekText, @"[Vv]ecka\s+(\d+)");
        if (!weekMatch.Success) return DateTime.MinValue;

        var weekNumber = int.Parse(weekMatch.Groups[1].Value);

        // Hitta första dagen i denna vecka för given månad/år
        var jan1 = new DateTime(month.Year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        if (daysOffset < 0) daysOffset += 7;

        var firstMonday = jan1.AddDays(daysOffset);
        var weekStart = firstMonday.AddDays((weekNumber - 1) * 7);

        // Hitta vilken dag i veckan weekdayName motsvarar
        var dayIndex = Array.FindIndex(culture.DateTimeFormat.DayNames, 
            d => d.Equals(weekdayName, StringComparison.OrdinalIgnoreCase));

        if (dayIndex == -1)
        {
            dayIndex = Array.FindIndex(culture.DateTimeFormat.AbbreviatedDayNames,
                d => d.Equals(weekdayName, StringComparison.OrdinalIgnoreCase));
        }

        if (dayIndex >= 0)
        {
            // Justera från söndag = 0 till måndag = 0
            var adjustedIndex = dayIndex == 0 ? 6 : dayIndex - 1;
            return weekStart.AddDays(adjustedIndex);
        }

        return DateTime.MinValue;
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

    private int? LoadLastPushId()
    {
        try
        {
            if (!File.Exists(PushIdFile)) return null;
            var json = File.ReadAllText(PushIdFile);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("pushId", out var elem) && elem.TryGetInt32(out var id))
            {
                return id;
            }
        }
        catch
        {
            // Ignorera fel vid läsning
        }
        return null;
    }

    private void SaveLastPushId(int id)
    {
        try
        {
            var obj = new { pushId = id };
            var json = JsonSerializer.Serialize(obj);
            File.WriteAllText(PushIdFile, json);
        }
        catch
        {
            // Ignorera skrivfel
        }
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
            //description.AppendLine($"<h3>Vecka {week.Key}: {weekStart:d MMM} - {weekEnd:d MMM}</h3>");
            //description.AppendLine("<ul>");

            foreach (var day in weekDays)
            {
                description.AppendLine($"<li><strong>{day.Value.DayName} {day.Key:d/M}:</strong> {day.Value.MainDish}</li>");
            }

            description.AppendLine("</ul>");

            var item = new SyndicationItem(
                $"Matsedel vecka {week.Key}",
                SyndicationContent.CreateHtmlContent(description.ToString()),
                new Uri($"https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/#week{week.Key}"),
                $"week-{week.Key}-{weekStart.Year}",
                weekStart
            );

            items.Add(item);
        }

        feed.Items = items;

        using var writer = XmlWriter.Create(outputPath, new XmlWriterSettings 
        { 
            Indent = true, 
            Encoding = new UTF8Encoding(false), // UTF8 utan BOM
            OmitXmlDeclaration = false
        });
        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(writer);

        Console.WriteLine($"Veckomatsedel sparad: {outputPath}");
    }
    
    private void GenerateDailyFeed(Dictionary<DateTime, MenuDay> menuData, string outputPath)
    {
        var today = DateTime.Today;

        // Försök hitta dagens meny, annars ta nästa tillgängliga dag
        var todayMenu = menuData.FirstOrDefault(m => m.Key.Date == today);

        if (todayMenu.Key == DateTime.MinValue)
        {
            // Ingen mat idag, hitta nästa dag
            todayMenu = menuData
                .Where(m => m.Key.Date > today)
                .OrderBy(m => m.Key)
                .FirstOrDefault();
        }

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
            var isToday = todayMenu.Key.Date == today;
            var prefix = isToday ? "Dagens lunch" : $"({todayMenu.Value.DayName})";

            //description.AppendLine($"<h3>{todayMenu.Value.DayName} {todayMenu.Key:d MMMM yyyy}</h3>");
            description.AppendLine($"<p>{todayMenu.Value.MainDish}</p>");

            var item = new SyndicationItem(
                //$"{prefix} - {todayMenu.Value.DayName} {todayMenu.Key:d/M}",
                $"{prefix}",
                SyndicationContent.CreateHtmlContent(description.ToString()),
                new Uri($"https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/#day{todayMenu.Key:yyyyMMdd}"),
                $"day-{todayMenu.Key:yyyyMMdd}",
                todayMenu.Key
            );

            items.Add(item);
        }
        else
        {
            // Ingen mat tillgänglig
            var item = new SyndicationItem(
                "Ingen lunch tillgänglig",
                SyndicationContent.CreateHtmlContent("<p>Ingen matsedel tillgänglig för närvarande.</p>"),
                new Uri("https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/"),
                $"no-menu-{today:yyyyMMdd}",
                today
            );

            items.Add(item);
        }

        feed.Items = items;

        using var writer = XmlWriter.Create(outputPath, new XmlWriterSettings 
        { 
            Indent = true, 
            Encoding = new UTF8Encoding(false), // UTF8 utan BOM
            OmitXmlDeclaration = false
        });
        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(writer);

        Console.WriteLine($"Dagens matsedel sparad: {outputPath} ({(todayMenu.Key.Date == today ? "idag" : "nästa dag")})");
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

            var titlePreview = day.Value.MainDish.Length > 50 
                ? day.Value.MainDish.Substring(0, 50) + "..." 
                : day.Value.MainDish;

            var item = new SyndicationItem(
                $"{day.Value.DayName} {day.Key:d/M} - {titlePreview}",
                SyndicationContent.CreateHtmlContent(description.ToString()),
                new Uri($"https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/#day{day.Key:yyyyMMdd}"),
                $"day-{day.Key:yyyyMMdd}",
                day.Key
            );

            items.Add(item);
        }

        feed.Items = items;

        using var writer = XmlWriter.Create(outputPath, new XmlWriterSettings 
        { 
            Indent = true, 
            Encoding = new UTF8Encoding(false), // UTF8 utan BOM
            OmitXmlDeclaration = false
        });
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
    public string VegetarianDish { get; set; } = "";
}
