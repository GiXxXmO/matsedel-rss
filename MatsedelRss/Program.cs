using System.ServiceModel.Syndication;
using System.Xml;
using HtmlAgilityPack;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;

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

        // Generera PNG-bilder av veckomatsedeln (försök, men fortsätt om det misslyckas)
        try
        {
            GenerateWeeklyImages(menuData, "output");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Kunde inte generera bilder: {ex.Message}");
            Console.WriteLine("Fortsätter ändå med RSS-generering...");
        }

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
            var urlNoId = $"{BaseUrl}/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/matsedelfor{monthName}.html";
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

                        // Samla text från p, div, eller textnoden
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

        // Använd ISO 8601 veckonumrering (svenska systemet)
        // Vecka 1 är den första veckan som innehåller en torsdag
        // Hitta måndagen i den veckan

        // Börja från 1 januari och hitta första torsdagen
        var jan1 = new DateTime(month.Year, 1, 1);
        int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)jan1.DayOfWeek + 7) % 7;
        var firstThursday = jan1.AddDays(daysUntilThursday);

        // Hitta måndagen i vecka 1 (3 dagar före första torsdagen)
        var firstMondayOfWeek1 = firstThursday.AddDays(-3);

        // Beräkna måndagen för den önskade veckan
        var weekStart = firstMondayOfWeek1.AddDays((weekNumber - 1) * 7);

        Console.WriteLine($"  Vecka {weekNumber}: Veckostart beräknad till {weekStart:yyyy-MM-dd}");

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
            var resultDate = weekStart.AddDays(adjustedIndex);

            // Verifiera att resultatet är i rätt månad (ungefär)
            Console.WriteLine($"  {weekdayName} i vecka {weekNumber} = {resultDate:yyyy-MM-dd} (veckodag {resultDate.DayOfWeek})");

            return resultDate;
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
            "Menyinformation för skolan",
            new Uri("https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/"),
            "matsedel-vecka",
            DateTime.Now
        );

        // Lägg till language och copyright
        feed.Language = "sv";
        feed.Copyright = new TextSyndicationContent("© Skara kommun");

        // Publicera endast aktuell vecka (använder sv-SE / ISO 8601 regler)
        var svCulture = new CultureInfo("sv-SE");
        var calendar = svCulture.Calendar;
        var currentWeek = calendar.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        var items = new List<SyndicationItem>();

        var weeks = menuData.GroupBy(m => calendar.GetWeekOfYear(m.Key, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));
        var week = weeks.FirstOrDefault(w => w.Key == currentWeek);

        if (week != null)
        {
            var weekDays = week.OrderBy(d => d.Key).ToList();

            // Skapa ett item per dag (liknar Skolmaten)
            foreach (var day in weekDays)
            {
                // Konvertera veckodagsnamn till versaliserad form (Måndag, Tisdag, etc.)
                var dayNameCapitalized = char.ToUpper(day.Value.DayName[0]) + day.Value.DayName.Substring(1);
                
                // Titel: "Måndag - Vecka 5"
                var title = $"{dayNameCapitalized} - Vecka {currentWeek}";
                
                // Description: Formatera som "Huvudrätt, <br/>Veg alternativ"
                var dishes = day.Value.MainDish.Split(new[] { "<br/>" }, StringSplitOptions.RemoveEmptyEntries);
                var description = new StringBuilder();
                
                for (int i = 0; i < dishes.Length; i++)
                {
                    var dish = dishes[i].Trim();
                    
                    // Ta bort "Veg: " prefix om det finns
                    if (dish.StartsWith("Veg:", StringComparison.OrdinalIgnoreCase))
                    {
                        dish = dish.Substring(4).Trim();
                    }
                    
                    description.Append(dish);
                    
                    // Lägg till komma och <br/> mellan rätter (utom sista)
                    if (i < dishes.Length - 1)
                    {
                        description.Append(", <br/>");
                    }
                }
                
                var item = new SyndicationItem(
                    title,
                    SyndicationContent.CreateHtmlContent($"<![CDATA[{description}]]>"),
                    new Uri("https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/"),
                    $"{Guid.NewGuid()}", // Unikt GUID för varje dag
                    day.Key
                );
                
                items.Add(item);
            }
        }
        else
        {
            Console.WriteLine($"Ingen meny hittades för aktuell vecka {currentWeek}.");
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

    private void GenerateWeeklyImages(Dictionary<DateTime, MenuDay> menuData, string outputDir)
    {
        // Hämta aktuell vecka
        var svCulture = new CultureInfo("sv-SE");
        var calendar = svCulture.Calendar;
        var currentWeek = calendar.GetWeekOfYear(DateTime.Today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        var weeks = menuData.GroupBy(m => calendar.GetWeekOfYear(m.Key, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday));
        var week = weeks.FirstOrDefault(w => w.Key == currentWeek);

        if (week == null)
        {
            Console.WriteLine($"Ingen meny för vecka {currentWeek}, kan inte generera bild.");
            return;
        }

        var weekDays = week.OrderBy(d => d.Key).ToList();

        // Olika storlekar att generera
        var sizes = new[] 
        { 
            (800, "VeckansMeny_800.png"),      // TV/Tablet
            (1024, "VeckansMeny_1024.png"),    // Standard skärm
            (1200, "VeckansMeny_1200.png"),    // Laptop
            (1600, "VeckansMeny_1600.png"),    // Desktop/Pintomind
            (1920, "VeckansMeny_1920.png"),    // Full HD
            (800, "VeckansMeny.png")           // Standard (alias för 800)
        };

        foreach (var (width, filename) in sizes)
        {
            var path = Path.Combine(outputDir, filename);
            GenerateMenuImage(weekDays, currentWeek, width, path);
            Console.WriteLine($"Genererade bild: {path}");
        }
    }

    private void GenerateMenuImage(List<KeyValuePair<DateTime, MenuDay>> weekDays, int weekNumber, int width, string outputPath)
    {
        // Beräkna höjd baserat på antal dagar och text
        var lineHeight = 50f;
        var titleHeight = 80f;
        var padding = 40f;
        var daySpacing = 15f;

        // Räkna totalt antal rader (varje dag har minst 2 rader: huvudrätt + veg)
        var totalLines = weekDays.Count * 3;
        var height = (int)(titleHeight + (totalLines * lineHeight) + (weekDays.Count * daySpacing) + (padding * 2));

        // Skapa bild med vit bakgrund
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.Fill(Color.White));

        // Ladda systemfont (fallback till default om Arial inte finns)
        FontFamily? fontFamily = null;

        // Prova olika fonter i prioritetsordning
        var fontsToTry = new[] { "Arial", "DejaVu Sans", "Liberation Sans", "FreeSans" };

        foreach (var fontName in fontsToTry)
        {
            if (SystemFonts.TryGet(fontName, out var foundFont))
            {
                fontFamily = foundFont;
                Console.WriteLine($"Använder font: {fontName}");
                break;
            }
        }

        // Om ingen av de föredragna fontterna finns, använd första tillgängliga
        if (fontFamily == null)
        {
            if (SystemFonts.Families.Any())
            {
                var fallbackFont = SystemFonts.Families.First();
                fontFamily = fallbackFont;
                Console.WriteLine($"Använder fallback-font: {fallbackFont.Name}");
            }
            else
            {
                throw new Exception("Inga systemfonter tillgängliga!");
            }
        }

        // Nu är fontFamily garanterat inte null
        var actualFont = fontFamily.Value;

        // Skapa typsnitt
        var titleFont = actualFont.CreateFont(width / 20f, FontStyle.Bold);
        var dayFont = actualFont.CreateFont(width / 30f, FontStyle.Bold);
        var dishFont = actualFont.CreateFont(width / 35f, FontStyle.Regular);
        var vegFont = actualFont.CreateFont(width / 35f, FontStyle.Regular);

        // Textfärger
        var titleColor = Color.Black;
        var dayColor = Color.FromRgb(51, 51, 51);
        var dishColor = Color.FromRgb(85, 85, 85);
        var vegColor = Color.FromRgb(0, 128, 0);

        var y = padding;

        // Rita titel
        var title = $"Matsedel V.{weekNumber}";
        var titleSize = TextMeasurer.MeasureBounds(title, new TextOptions(titleFont));
        var titleX = (width - titleSize.Width) / 2;

        image.Mutate(ctx => ctx.DrawText(title, titleFont, titleColor, new PointF(titleX, y)));
        y += titleHeight;

        // Rita varje dag
        foreach (var day in weekDays)
        {
            // Dag
            var dayText = $"{day.Value.DayName}:";
            image.Mutate(ctx => ctx.DrawText(dayText, dayFont, dayColor, new PointF(padding, y)));
            y += lineHeight;

            // Dela upp MainDish på <br/> för att visa separat
            var dishes = day.Value.MainDish.Split(new[] { "<br/>" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < dishes.Length; i++)
            {
                var dish = dishes[i].Trim();

                // Kolla om det är veg-alternativet
                bool isVeg = dish.StartsWith("Veg:", StringComparison.OrdinalIgnoreCase);

                var font = isVeg ? vegFont : dishFont;
                var color = isVeg ? vegColor : dishColor;
                var indent = padding + 80;

                // Radbryt lång text om nödvändigt
                var maxWidth = width - indent - padding;
                var wrappedLines = WrapText(dish, font, maxWidth);

                foreach (var line in wrappedLines)
                {
                    image.Mutate(ctx => ctx.DrawText(line, font, color, new PointF(indent, y)));
                    y += lineHeight * 0.8f;
                }
            }

            y += daySpacing;
        }

        // Spara bild
        image.SaveAsPng(outputPath);
    }

    private List<string> WrapText(string text, Font font, float maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            var testLine = currentLine.Length > 0 ? $"{currentLine} {word}" : word;
            var lineSize = TextMeasurer.MeasureBounds(testLine, new TextOptions(font));

            if (lineSize.Width > maxWidth && currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0) currentLine.Append(" ");
                currentLine.Append(word);
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(currentLine.ToString());
        }

        return lines;
    }
}

public class MenuDay
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = "";
    public string MainDish { get; set; } = "";
    public string VegetarianDish { get; set; } = "";
}
