# Snabbstart - Matsedel RSS

## För utvecklare som vill köra lokalt

### 1. Klona eller ladda ner projektet
```bash
git clone https://github.com/[username]/matsedel-rss.git
cd matsedel-rss
```

### 2. Kör applikationen
```bash
dotnet run --project MatsedelRss/MatsedelRss.csproj
```

### 3. Visa resultatet
Öppna `MatsedelRss/viewer.html` i din webbläsare för att se dagens och veckans matsedel.

---

## För att sätta upp på GitHub med automatisk uppdatering

### 1. Forka eller pusha projektet till GitHub

### 2. Aktivera GitHub Actions
- Gå till "Actions"-fliken i ditt repository
- Klicka "I understand my workflows, go ahead and enable them"

### 3. (Valfritt) Aktivera GitHub Pages för viewer
- Gå till Settings ? Pages
- Under "Source", välj "Deploy from a branch"
- Välj `main` branch och `/MatsedelRss` folder
- Klicka "Save"
- Din viewer kommer finnas på: `https://[username].github.io/[repo]/viewer.html`

### 4. Konfigurera viewer för GitHub Pages
Om du aktiverade GitHub Pages, uppdatera `MatsedelRss/viewer.html`:

```javascript
const RSS_FEEDS = {
    today: 'https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml',
    week: 'https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-alla-dagar.xml'
};
```

Ersätt `[username]` och `[repo]` med dina faktiska värden.

### 5. Första körningen
Kör GitHub Actions manuellt för första gången:
- Gå till Actions-fliken
- Välj "Uppdatera Matsedel RSS"
- Klicka "Run workflow"
- Efter det kommer den köra automatiskt varje dag kl 06:00 UTC

---

## För att använda i Digital Signage / Informationsskärm

### Metod 1: Lokal HTML-viewer
1. Kör programmet en gång: `dotnet run --project MatsedelRss/MatsedelRss.csproj`
2. Öppna `MatsedelRss/viewer.html` i Chrome eller Edge
3. Tryck F11 för fullskärmsläge
4. Sätt upp browsern att starta automatiskt vid systemstart

### Metod 2: GitHub Pages (Rekommenderat för nätverk)
1. Följ stegen ovan för att aktivera GitHub Pages
2. Öppna viewer-URL:en på informationsskärmen
3. Sidan uppdateras automatiskt varje dag via GitHub Actions

### Metod 3: RSS-reader i befintlig Digital Signage-lösning
Använd följande RSS-URLs i din befintliga lösning:
- Dagens: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml`
- Vecka: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-vecka.xml`
- Alla dagar: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-alla-dagar.xml`

---

## Felsökning

### "Kunde inte hämta matsedel"
- Kontrollera att du har internetanslutning
- Verifiera att Skara kommuns webbplats är uppe
- Kolla att URL:en i `Program.cs` stämmer med aktuell matsedelssida

### Viewer visar inte data
- Kontrollera att RSS-filerna finns i `output/`-mappen
- För GitHub Pages: Vänta några minuter efter första deploy
- Öppna webbläsarens konsol (F12) för detaljerade felmeddelanden

### GitHub Actions fungerar inte
- Kontrollera att Actions är aktiverade i repository settings
- Verifiera att workflow-filen finns i `.github/workflows/`
- Kolla Actions-loggen för detaljerade felmeddelanden

---

## Support

För problem eller frågor, öppna ett issue på GitHub.
