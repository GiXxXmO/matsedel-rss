# Matsedel RSS - Skara Kommun

En RSS-läsare som automatiskt skrapar matsedeln från Skara kommuns webbplats och genererar RSS-feeds som kan användas i informationsskärmar och andra applikationer.

## Funktioner

- ? Skrapar matsedel från Skara kommuns webbplats
- ? Genererar tre RSS-feeds:
  - **Dagens** (`matsedel-dagens.xml`) - Dagens matsedel
  - **Veckovy** (`matsedel-vecka.xml`) - Hela veckans matsedel
  - **Alla dagar** (`matsedel-alla-dagar.xml`) - Kommande 30 dagar
- ? Automatisk uppdatering via GitHub Actions varje dag
- ? Automatisk hämtning av nästa månads matsedel i slutet av månaden
- ? Inbyggd HTML-viewer för digital signage
- ? Perfekt för informationsskärmar/digital signage

## RSS-Feeds

Efter körning finns följande feeds tillgängliga:

- `MatsedelRss/output/matsedel-dagens.xml` - Dagens matsedel
- `MatsedelRss/output/matsedel-vecka.xml` - Hela veckans matsedel grupperad per vecka
- `MatsedelRss/output/matsedel-alla-dagar.xml` - Alla kommande dagar (max 30)

## Användning

### Lokalt

```bash
# Navigera till projektkatalogen
cd MatsedelRss

# Återställ dependencies
dotnet restore

# Kör programmet
dotnet run
```

RSS-filerna genereras i `MatsedelRss/output/` katalogen.

### HTML Viewer

Projektet inkluderar en färdig HTML-viewer för digital signage:

1. Kör programmet för att generera RSS-feeds
2. Öppna `MatsedelRss/viewer.html` i en webbläsare
3. För fullskärmsläge (digital signage), tryck F11

Viewer:n uppdateras automatiskt var 30:e minut och visar:
- **Dagens Mat** - Stor, tydlig vy av dagens lunch
- **Hela Veckan** - Översikt över veckans alla luncher

### GitHub Actions

Projektet är konfigurerat att automatiskt uppdatera RSS-feeds:

- **Daglig uppdatering**: Varje dag kl 06:00 UTC (07:00/08:00 svensk tid)
- **Manuell körning**: Kan triggas manuellt via GitHub Actions-fliken
- **Vid push**: Uppdateras automatiskt vid push till main-branchen
- **Automatisk månadsövergång**: Hämtar nästa månads matsedel 7 dagar innan månadsskifte

RSS-feeds committas automatiskt tillbaka till repositoryt och kan nås via:
```
https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml
https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-vecka.xml
https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-alla-dagar.xml
```

## Användning i Informationsskärm

### Alternativ 1: HTML Viewer (Rekommenderat)

1. **Konfigurera viewer för GitHub Pages:**
   - Redigera `MatsedelRss/viewer.html`
   - Uppdatera RSS_FEEDS URL:er till dina GitHub Raw-länkar
   
2. **Aktivera GitHub Pages:**
   - Gå till Settings ? Pages i ditt repository
   - Välj "Deploy from a branch"
   - Välj `main` branch och `/MatsedelRss` folder
   - Öppna `https://[username].github.io/[repo]/viewer.html`

3. **För lokal display:**
   - Öppna `viewer.html` direkt från filsystemet
   - Tryck F11 för fullskärmsläge
   - Viewer:n uppdateras automatiskt

### Alternativ 2: RSS-Reader för Digital Signage

De flesta digital signage-lösningar (t.ex. ScreenCloud, Yodeck, OptiSigns) har inbyggt stöd för RSS-feeds.

1. Använd URL:en till RSS-feeden från GitHub
2. Konfigurera updateringfrekvens (rekommenderat: daglig uppdatering)
3. Välj mellan veckoöversikt eller dagens mat

### Alternativ 3: Egen HTML/JavaScript-implementation

```html
<!DOCTYPE html>
<html>
<head>
    <title>Dagens Lunch</title>
    <style>
        body { font-family: Arial, sans-serif; padding: 20px; }
        h1 { color: #333; }
        .menu { font-size: 24px; background: #f0f0f0; padding: 20px; border-radius: 10px; }
    </style>
</head>
<body>
    <h1>Dagens Lunch</h1>
    <div id="menu" class="menu">Laddar matsedel...</div>
    
    <script>
        async function loadMenu() {
            const response = await fetch('https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml');
            const text = await response.text();
            const parser = new DOMParser();
            const xml = parser.parseFromString(text, 'text/xml');
            const description = xml.querySelector('description').textContent;
            document.getElementById('menu').innerHTML = description;
        }
        
        loadMenu();
        setInterval(loadMenu, 3600000); // Uppdatera varje timme
    </script>
</body>
</html>
```

## Konfiguration

### Ändra URL för matsedel

Om Skara kommun ändrar URL-strukturen, uppdatera `BaseUrl` och URL-genereringen i `MatsedelScraper` klassen i `Program.cs`.

### Ändra uppdateringsschema

Redigera `.github/workflows/update-rss.yml` och ändra cron-schemat:

```yaml
schedule:
  - cron: '0 6 * * *'  # Ändra tiden här (UTC)
```

### Ändra månadsövergångstid

I `Program.cs`, ändra antalet dagar för när nästa månad ska hämtas:

```csharp
if (daysLeftInMonth <= 7)  // Ändra från 7 till önskat antal dagar
```

## Teknisk Stack

- **.NET 9.0** - Runtime
- **HtmlAgilityPack** - HTML-parsning och web scraping
- **System.ServiceModel.Syndication** - RSS-generering
- **GitHub Actions** - Automatisering
- **Vanilla JavaScript** - HTML viewer

## Projektstruktur

```
matsedel-rss/
??? MatsedelRss/
?   ??? Program.cs              # Huvudprogram med scraper och RSS-generering
?   ??? MatsedelRss.csproj      # Projektfil
?   ??? viewer.html             # HTML-viewer för digital signage
?   ??? output/                 # Genererade RSS-feeds
?       ??? matsedel-dagens.xml
?       ??? matsedel-vecka.xml
?       ??? matsedel-alla-dagar.xml
??? .github/
?   ??? workflows/
?       ??? update-rss.yml      # GitHub Actions workflow
??? README.md
??? TESTING.md
??? .gitignore
```

## Felsökning

### Problem med att hitta matsedeln

Om programmet inte hittar matsedeln:
1. Besök https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/
2. Hitta aktuell månads sida
3. Kontrollera URL-strukturen
4. Uppdatera URL-genereringen i `Program.cs`

### Problem med parsning

Om HTML-strukturen har ändrats på webbplatsen:
1. Öppna matsedelssidan i webbläsaren
2. Inspektera HTML-strukturen (F12)
3. Uppdatera parsning-logiken i metoderna:
   - `ParseTable()`
   - `ParseDateHeaders()`

### Viewer visar inte innehåll

- Kontrollera att RSS-feeds har genererats i `output/`-mappen
- Kontrollera att URL:erna i `viewer.html` är korrekta
- Öppna webbläsarens konsol (F12) för felmeddelanden
- För GitHub Pages: Kontrollera att Pages är aktiverat och korrekt konfigurerat

## Förbättringsmöjligheter

- [ ] Stöd för flera skolor/enheter
- [ ] Filtrera på allergener
- [ ] JSON API utöver RSS
- [ ] E-postnotiser för nya menyer
- [ ] Historisk data och statistik
- [ ] PWA-stöd för offline-visning
- [ ] Stöd för fler kommuner

## Licens

MIT License

## Utvecklare

Skapad för att enkelt visa matsedel från Skara kommun på informationsskärmar.
