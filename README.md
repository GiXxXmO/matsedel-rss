# Matsedel RSS - Skara Kommun

En RSS-l�sare som automatiskt skrapar matsedeln fr�n Skara kommuns webbplats och genererar RSS-feeds som kan anv�ndas i informationssk�rmar och andra applikationer.

## Funktioner

- ? Skrapar matsedel fr�n Skara kommuns webbplats
- ? Genererar tre RSS-feeds:
  - **Dagens** (`matsedel-dagens.xml`) - Dagens matsedel
  - **Veckovy** (`matsedel-vecka.xml`) - Hela veckans matsedel
  - **Alla dagar** (`matsedel-alla-dagar.xml`) - Kommande 30 dagar
- ? Automatisk uppdatering via GitHub Actions varje dag
- ? Automatisk h�mtning av n�sta m�nads matsedel i slutet av m�naden
- ? Inbyggd HTML-viewer f�r digital signage
- ? Perfekt f�r informationssk�rmar/digital signage

## RSS-Feeds

Efter k�rning finns f�ljande feeds tillg�ngliga:

- `MatsedelRss/output/matsedel-dagens.xml` - Dagens matsedel
- `MatsedelRss/output/matsedel-vecka.xml` - Hela veckans matsedel grupperad per vecka
- `MatsedelRss/output/matsedel-alla-dagar.xml` - Alla kommande dagar (max 30)

## Kom ig�ng

### �ppna i Visual Studio

Om du vill utveckla och �ndra projektet i Visual Studio, se den kompletta guiden: **[Visual Studio Setup Guide](VS-SETUP.md)**

Snabbstart:
1. �ppna Visual Studio 2022
2. Klona repository: `https://github.com/GiXxXmO/matsedel-rss.git`
3. �ppna `MatsedelRss.sln`
4. B�rja koda!

### Anv�ndning

### Lokalt

```bash
# Navigera till projektkatalogen
cd MatsedelRss

# �terst�ll dependencies
dotnet restore

# K�r programmet
dotnet run
```

RSS-filerna genereras i `MatsedelRss/output/` katalogen.

### HTML Viewer

Projektet inkluderar en f�rdig HTML-viewer f�r digital signage:

1. K�r programmet f�r att generera RSS-feeds
2. �ppna `MatsedelRss/viewer.html` i en webbl�sare
3. F�r fullsk�rmsl�ge (digital signage), tryck F11

Viewer:n uppdateras automatiskt var 30:e minut och visar:
- **Dagens Mat** - Stor, tydlig vy av dagens lunch
- **Hela Veckan** - �versikt �ver veckans alla luncher

### GitHub Actions

Projektet �r konfigurerat att automatiskt uppdatera RSS-feeds:

- **Daglig uppdatering**: Varje dag kl 06:00 UTC (07:00/08:00 svensk tid)
- **Manuell k�rning**: Kan triggas manuellt via GitHub Actions-fliken
- **Vid push**: Uppdateras automatiskt vid push till main-branchen
- **Automatisk m�nads�verg�ng**: H�mtar n�sta m�nads matsedel 7 dagar innan m�nadsskifte

RSS-feeds committas automatiskt tillbaka till repositoryt och kan n�s via:
```
https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml
https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-vecka.xml
https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-alla-dagar.xml
```

## Anv�ndning i Informationssk�rm

### Alternativ 1: HTML Viewer (Rekommenderat)

1. **Konfigurera viewer f�r GitHub Pages:**
   - Redigera `MatsedelRss/viewer.html`
   - Uppdatera RSS_FEEDS URL:er till dina GitHub Raw-l�nkar
   
2. **Aktivera GitHub Pages:**
   - G� till Settings ? Pages i ditt repository
   - V�lj "Deploy from a branch"
   - V�lj `main` branch och `/MatsedelRss` folder
   - �ppna `https://[username].github.io/[repo]/viewer.html`

3. **F�r lokal display:**
   - �ppna `viewer.html` direkt fr�n filsystemet
   - Tryck F11 f�r fullsk�rmsl�ge
   - Viewer:n uppdateras automatiskt

### Alternativ 2: RSS-Reader f�r Digital Signage

De flesta digital signage-l�sningar (t.ex. ScreenCloud, Yodeck, OptiSigns) har inbyggt st�d f�r RSS-feeds.

1. Anv�nd URL:en till RSS-feeden fr�n GitHub
2. Konfigurera updateringfrekvens (rekommenderat: daglig uppdatering)
3. V�lj mellan vecko�versikt eller dagens mat

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

### �ndra URL f�r matsedel

Om Skara kommun �ndrar URL-strukturen, uppdatera `BaseUrl` och URL-genereringen i `MatsedelScraper` klassen i `Program.cs`.

### �ndra uppdateringsschema

Redigera `.github/workflows/update-rss.yml` och �ndra cron-schemat:

```yaml
schedule:
  - cron: '0 6 * * *'  # �ndra tiden h�r (UTC)
```

### �ndra m�nads�verg�ngstid

I `Program.cs`, �ndra antalet dagar f�r n�r n�sta m�nad ska h�mtas:

```csharp
if (daysLeftInMonth <= 7)  // �ndra fr�n 7 till �nskat antal dagar
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
?   ??? viewer.html             # HTML-viewer f�r digital signage
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

## Fels�kning

### Problem med att hitta matsedeln

Om programmet inte hittar matsedeln:
1. Bes�k https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/
2. Hitta aktuell m�nads sida
3. Kontrollera URL-strukturen
4. Uppdatera URL-genereringen i `Program.cs`

### Problem med parsning

Om HTML-strukturen har �ndrats p� webbplatsen:
1. �ppna matsedelssidan i webbl�saren
2. Inspektera HTML-strukturen (F12)
3. Uppdatera parsning-logiken i metoderna:
   - `ParseTable()`
   - `ParseDateHeaders()`

### Viewer visar inte inneh�ll

- Kontrollera att RSS-feeds har genererats i `output/`-mappen
- Kontrollera att URL:erna i `viewer.html` �r korrekta
- �ppna webbl�sarens konsol (F12) f�r felmeddelanden
- F�r GitHub Pages: Kontrollera att Pages �r aktiverat och korrekt konfigurerat

## F�rb�ttringsm�jligheter

- [ ] St�d f�r flera skolor/enheter
- [ ] Filtrera p� allergener
- [ ] JSON API ut�ver RSS
- [ ] E-postnotiser f�r nya menyer
- [ ] Historisk data och statistik
- [ ] PWA-st�d f�r offline-visning
- [ ] St�d f�r fler kommuner

## Licens

MIT License

## Utvecklare

Skapad f�r att enkelt visa matsedel fr�n Skara kommun p� informationssk�rmar.
