# Snabbstart - Matsedel RSS

## F�r utvecklare som vill k�ra lokalt

### Med Visual Studio (Rekommenderat f�r Windows-utvecklare)

Se den kompletta guiden: **[Visual Studio Setup Guide](VS-SETUP.md)**

Snabbstart:
1. �ppna Visual Studio 2022
2. Klona repository: `https://github.com/GiXxXmO/matsedel-rss.git`
3. �ppna `MatsedelRss.sln`
4. Tryck `Ctrl + F5` f�r att k�ra

### Med kommandoraden

### 1. Klona eller ladda ner projektet
```bash
git clone https://github.com/[username]/matsedel-rss.git
cd matsedel-rss
```

### 2. K�r applikationen
```bash
dotnet run --project MatsedelRss/MatsedelRss.csproj
```

### 3. Visa resultatet
�ppna `MatsedelRss/viewer.html` i din webbl�sare f�r att se dagens och veckans matsedel.

---

## F�r att s�tta upp p� GitHub med automatisk uppdatering

### 1. Forka eller pusha projektet till GitHub

### 2. Aktivera GitHub Actions
- G� till "Actions"-fliken i ditt repository
- Klicka "I understand my workflows, go ahead and enable them"

### 3. (Valfritt) Aktivera GitHub Pages f�r viewer
- G� till Settings ? Pages
- Under "Source", v�lj "Deploy from a branch"
- V�lj `main` branch och `/MatsedelRss` folder
- Klicka "Save"
- Din viewer kommer finnas p�: `https://[username].github.io/[repo]/viewer.html`

### 4. Konfigurera viewer f�r GitHub Pages
Om du aktiverade GitHub Pages, uppdatera `MatsedelRss/viewer.html`:

```javascript
const RSS_FEEDS = {
    today: 'https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml',
    week: 'https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-alla-dagar.xml'
};
```

Ers�tt `[username]` och `[repo]` med dina faktiska v�rden.

### 5. F�rsta k�rningen
K�r GitHub Actions manuellt f�r f�rsta g�ngen:
- G� till Actions-fliken
- V�lj "Uppdatera Matsedel RSS"
- Klicka "Run workflow"
- Efter det kommer den k�ra automatiskt varje dag kl 06:00 UTC

---

## F�r att anv�nda i Digital Signage / Informationssk�rm

### Metod 1: Lokal HTML-viewer
1. K�r programmet en g�ng: `dotnet run --project MatsedelRss/MatsedelRss.csproj`
2. �ppna `MatsedelRss/viewer.html` i Chrome eller Edge
3. Tryck F11 f�r fullsk�rmsl�ge
4. S�tt upp browsern att starta automatiskt vid systemstart

### Metod 2: GitHub Pages (Rekommenderat f�r n�tverk)
1. F�lj stegen ovan f�r att aktivera GitHub Pages
2. �ppna viewer-URL:en p� informationssk�rmen
3. Sidan uppdateras automatiskt varje dag via GitHub Actions

### Metod 3: RSS-reader i befintlig Digital Signage-l�sning
Anv�nd f�ljande RSS-URLs i din befintliga l�sning:
- Dagens: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml`
- Vecka: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-vecka.xml`
- Alla dagar: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-alla-dagar.xml`

---

## Fels�kning

### "Kunde inte h�mta matsedel"
- Kontrollera att du har internetanslutning
- Verifiera att Skara kommuns webbplats �r uppe
- Kolla att URL:en i `Program.cs` st�mmer med aktuell matsedelssida

### Viewer visar inte data
- Kontrollera att RSS-filerna finns i `output/`-mappen
- F�r GitHub Pages: V�nta n�gra minuter efter f�rsta deploy
- �ppna webbl�sarens konsol (F12) f�r detaljerade felmeddelanden

### GitHub Actions fungerar inte
- Kontrollera att Actions �r aktiverade i repository settings
- Verifiera att workflow-filen finns i `.github/workflows/`
- Kolla Actions-loggen f�r detaljerade felmeddelanden

---

## Support

F�r problem eller fr�gor, �ppna ett issue p� GitHub.
