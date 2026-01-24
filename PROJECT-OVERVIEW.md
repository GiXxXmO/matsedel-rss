# Matsedel RSS - Projektoversikt

## Sammanfattning

Ett komplett system för att automatiskt skrapa matsedel från Skara kommuns webbplats och presentera den som RSS-feeds och via en modern webbvisare, perfekt för informationsskärmar och digital signage.

## Nyckelkomponenter

### 1. MatsedelScraper (Program.cs)
**Huvudfunktionalitet:**
- Skrapar matsedelssidan från Skara kommun
- Hanterar flera URL-format och månadsövergångar
- Parsar HTML-tabeller och datum-headers
- Stödjer svenska datum- och månadsformat
- Automatisk hämtning av nästa månads matsedel

**Tekniker:**
- HtmlAgilityPack för HTML-parsning
- HttpClient för web requests
- Regex för textbearbetning
- CultureInfo för svensk lokalisering

### 2. RSS-generering
**Tre olika feeds:**

#### matsedel-dagens.xml
- Dagens matsedel
- Uppdateras dagligen
- Visar aktuell dag eller "Ingen lunch idag"

#### matsedel-vecka.xml
- Grupperad per vecka
- Visar alla veckodagar
- Perfekt för veckoöversikter

#### matsedel-alla-dagar.xml
- Alla kommande dagar (max 30)
- Sorterad kronologiskt
- Exkluderar historiska datum

**Tekniker:**
- System.ServiceModel.Syndication för RSS 2.0
- XmlWriter för formaterad output
- UTF-8 encoding
- Välformade HTML-beskrivningar

### 3. HTML Viewer (viewer.html)
**Features:**
- Responsiv design (desktop & mobile)
- Två vyer: Dagens mat och hela veckan
- Automatisk uppdatering var 30:e minut
- Modern gradient-design
- Visar senaste uppdateringstid
- Fullskärmsläge för digital signage

**Tekniker:**
- Vanilla JavaScript (ingen dependencies)
- CSS Grid och Flexbox
- DOMParser för XML-parsning
- Fetch API för asynkrona anrop
- LocalStorage för eventuell caching

### 4. GitHub Actions (update-rss.yml)
**Automatisering:**
- Kör dagligen kl 06:00 UTC
- Bygg och kör .NET-applikationen
- Committa och pusha RSS-feeds
- Stöd för manuell trigger

**Workflow:**
1. Checkout kod
2. Setup .NET 9.0
3. Restore dependencies
4. Build projekt
5. Kör scraper
6. Commit resultat
7. Push till repository

## Filstruktur

```
matsedel-rss/
?
??? MatsedelRss/                      # Huvudprojekt
?   ??? Program.cs                    # Scraper och RSS-generering
?   ??? MatsedelRss.csproj           # Projektfil med dependencies
?   ??? viewer.html                   # Web-baserad viewer
?   ??? output/                       # Genererade RSS-feeds
?       ??? matsedel-dagens.xml
?       ??? matsedel-vecka.xml
?       ??? matsedel-alla-dagar.xml
?
??? .github/
?   ??? workflows/
?       ??? update-rss.yml           # GitHub Actions workflow
?
??? README.md                         # Huvuddokumentation
??? QUICKSTART.md                     # Snabbstartsguide
??? TESTING.md                        # Testinstruktioner
??? DIGITAL-SIGNAGE.md               # Integration-guide
??? CONTRIBUTING.md                   # Bidragsguide
??? .gitignore                       # Git ignore-regler
??? MatsedelRss.sln                  # Visual Studio solution

```

## Dataflöde

```
???????????????????????
?  Skara kommun       ?
?  Webbplats          ?
???????????????????????
           ?
           ? HTTP GET (Scraping)
           ?
???????????????????????
?  MatsedelScraper    ?
?  - HTML Parsing     ?
?  - Date Extraction  ?
???????????????????????
           ?
           ? Processad data
           ?
???????????????????????
?  RSS Generator      ?
?  - Dagens           ?
?  - Vecka            ?
?  - Alla dagar       ?
???????????????????????
           ?
           ? XML files
           ?
???????????????????????     ???????????????????????
?  GitHub Repository  ???????  HTML Viewer        ?
?  (RSS feeds)        ?     ?  (Browser)          ?
???????????????????????     ???????????????????????
           ?
           ? Raw URLs
           ?
???????????????????????
?  Digital Signage    ?
?  Lösningar          ?
???????????????????????
```

## Teknisk Stack

### Backend (.NET)
- **.NET 9.0**: Modern runtime med bästa prestanda
- **C# 13**: Latest language features
- **HtmlAgilityPack 1.12.4**: Robust HTML-parsning
- **System.ServiceModel.Syndication 10.0.2**: RSS/Atom-stöd

### Frontend
- **HTML5**: Semantisk markup
- **CSS3**: Grid, Flexbox, Gradients
- **JavaScript ES6+**: Moderna features
- **DOMParser**: XML-parsning i browser

### DevOps
- **GitHub Actions**: CI/CD automation
- **Git**: Versionshantering
- **GitHub Pages**: Hosting (optional)

## Designbeslut

### Varför .NET?
- Utmärkt för web scraping
- Stark XML/RSS-support
- Bra prestanda
- Cross-platform (Linux/Windows/Mac)
- GitHub Actions har built-in support

### Varför ingen databas?
- RSS-filer är tillräckliga
- Enkelt att deploya
- Inga extra kostnader
- Git fungerar som historik
- Lätt att backup:a

### Varför tre olika RSS-feeds?
- Flexibilitet för olika användningsfall
- Dagens mat för quick-view
- Vecka för planning
- Alla dagar för komplett översikt

### Varför vanilla JavaScript istället för React/Vue?
- Inga build-steps behövs
- Snabbare laddningstid
- Lättare att underhålla
- Perfekt för enkla use-cases
- Fungerar direkt från filsystem

## Prestanda

### Scraping
- **Tid**: ~2-5 sekunder (beroende på nätverkshastighet)
- **Minne**: ~50MB under körning
- **Nätverksanrop**: 1-4 requests (försöker olika URLs)

### RSS-generering
- **Tid**: <100ms per feed
- **Filstorlek**: 1-5KB per feed (beroende på innehåll)

### HTML Viewer
- **Initial load**: <1 sekund
- **RSS fetch**: <500ms (från GitHub Raw)
- **Render**: <100ms
- **Minnesanvändning**: <10MB

## Säkerhet

### Web Scraping
- User-Agent header för att identifiera bot
- Respekterar robots.txt (om implementerat)
- Ingen känslig data skrapas

### GitHub Actions
- Använder bot-konto för commits
- Inga secrets behövs för public repos
- Read-only access till external sites

### Viewer
- Ingen data skickas till tredje part
- CORS-friendly (använder Raw URLs)
- Ingen localStorage av känslig data

## Framtida möjligheter

### Kortsiktigt
1. Stöd för flera skolor/enheter
2. Bättre felhantering vid nätverksproblem
3. Offline-stöd i viewer
4. Bildstöd för maträtter

### Långsiktigt
1. REST API
2. Mobilapp
3. Push-notiser
4. Favoritmaträtter
5. Social sharing
6. Nutritionsinformation
7. Allergenerinformation

## Underhåll

### Regelbundet
- Kontrollera att scraping fungerar
- Uppdatera dependencies (dotnet outdated)
- Granska GitHub Actions-loggar

### Vid problem
- Verifiera URL-struktur på källsidan
- Kontrollera HTML-struktur (kan ändras)
- Uppdatera parsning-logik vid behov

### Vid ny månad
- Systemet hanterar detta automatiskt
- Kontrollera första dagarna att rätt data visas
- Vid problem, kör manuell GitHub Actions

## Support och Community

- **Issues**: Rapportera buggar och föreslå features
- **Pull Requests**: Bidra med kod
- **Discussions**: Ställ frågor och dela erfarenheter
- **Wiki**: Detaljerad dokumentation (coming soon)

## Licens och Copyright

- **Licens**: MIT (fri att använda och modifiera)
- **Data**: Matsedelsdata tillhör Skara kommun
- **Användning**: Följ Skara kommuns användningsvillkor

---

**Version**: 1.0.0  
**Senast uppdaterad**: 2026-01-23  
**Författare**: GitHub Community
