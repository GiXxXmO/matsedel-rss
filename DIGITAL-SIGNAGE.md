# Integration med Digital Signage-lösningar

Denna guide visar hur du integrerar Matsedel RSS med populära digital signage-plattformar.

## ScreenCloud

1. Logga in på ScreenCloud
2. Gå till "Apps" ? "RSS Feed"
3. Lägg till RSS Feed-appen till din skärm
4. Konfigurera:
   - **Feed URL**: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml`
   - **Update Frequency**: 1 hour eller Daily
   - **Display Duration**: 30-60 sekunder
5. Anpassa layout och färger efter behov

## Yodeck

1. Logga in på Yodeck
2. Gå till "Media" ? "Add Widget"
3. Välj "RSS Feed"
4. Konfigurera:
   - **URL**: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-alla-dagar.xml`
   - **Refresh Rate**: 3600 seconds (1 timme)
   - **Template**: Välj en passande mall eller skapa egen
5. Lägg till i din playlist

## OptiSigns

1. Logga in på OptiSigns
2. Gå till "Content" ? "Add Content"
3. Välj "RSS Feed"
4. Konfigurera:
   - **Feed URL**: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-vecka.xml`
   - **Refresh Interval**: 60 minutes
5. Dra och släpp på din layout

## Rise Vision

1. Logga in på Rise Vision
2. Gå till "Presentations"
3. Lägg till "RSS Widget"
4. Konfigurera:
   - **URL**: `https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml`
   - **Update Interval**: 1 hour
5. Publicera presentationen

## Xibo

1. Logga in på Xibo CMS
2. Gå till "Library" ? "Add Media"
3. Välj "Web Page"
4. URL: `https://[username].github.io/[repo]/viewer.html`
5. Refresh: 3600 seconds
6. Lägg till i din layout

## Google Slides (Enkel lösning)

För en mycket enkel lösning kan du använda Google Slides:

1. Skapa en ny Google Slides-presentation
2. Lägg till en textbox för matsedeln
3. Använd Google Apps Script för att hämta RSS-feed:

```javascript
function updateMenu() {
  var url = 'https://raw.githubusercontent.com/[username]/[repo]/main/MatsedelRss/output/matsedel-dagens.xml';
  var xml = UrlFetchApp.fetch(url).getContentText();
  var document = XmlService.parse(xml);
  var root = document.getRootElement();
  var description = root.getChild('channel').getChild('item').getChild('description').getText();
  
  // Uppdatera slide
  var presentation = SlidesApp.getActivePresentation();
  var slide = presentation.getSlides()[0];
  var textBox = slide.getShapes()[0];
  textBox.getText().setText(description);
}
```

4. Sätt upp en trigger att köra varje timme
5. Publicera som webbsida eller visa direkt

## Raspberry Pi med Chromium (DIY)

Om du vill bygga din egen lösning med en Raspberry Pi:

### Installation

```bash
# Installera Chromium
sudo apt-get update
sudo apt-get install chromium-browser unclutter

# Skapa autostart-script
nano ~/.config/lxsession/LXDE-pi/autostart
```

### Lägg till i autostart:

```bash
@xset s off
@xset -dpms
@xset s noblank
@chromium-browser --noerrdialogs --disable-infobars --kiosk https://[username].github.io/[repo]/viewer.html
```

### Eller använd lokal fil:

```bash
# Klona repo
cd ~
git clone https://github.com/[username]/[repo].git

# Lägg till i autostart
@chromium-browser --noerrdialogs --disable-infobars --kiosk file:///home/pi/[repo]/MatsedelRss/viewer.html

# Sätt upp cron för att uppdatera
crontab -e
# Lägg till:
0 7 * * * cd ~/[repo] && git pull
```

## Smart TV med webbläsare

De flesta moderna smart-TV:ar har en inbyggd webbläsare:

1. Öppna webbläsaren på TV:n
2. Navigera till: `https://[username].github.io/[repo]/viewer.html`
3. Gå till fullskärmsläge (vanligtvis via meny eller F11)
4. Ställ in TV:n att starta med webbläsaren öppen

## Tips för bästa resultat

### Uppdateringsfrekvens
- **Daglig uppdatering räcker**: Matsedeln ändras normalt en gång per dag
- **GitHub Actions kör kl 06:00 UTC**: Dina feeds uppdateras automatiskt
- **Viewer uppdaterar var 30:e minut**: Automatisk reload av innehåll

### Display-inställningar
- **Upplösning**: 1920x1080 (Full HD) rekommenderas
- **Orientation**: Landscape för veckoöversikt, Portrait för dagens mat
- **Brightness**: Justera efter omgivande ljus
- **Sleep Mode**: Inaktivera för 24/7-drift

### Nätverkskrav
- **Bandbredd**: Minimal (endast XML-filer, några KB)
- **Stabilitet**: Behöver stabil internetanslutning för uppdateringar
- **Firewall**: Tillåt utgående trafik till GitHub och Raw-innehåll

### Backup-lösning
Om GitHub är nere:
- Viewer:n visar senast laddade innehåll
- RSS-filerna finns lokalt i `output/`-mappen
- Kan servas från egen webbserver vid behov

## Anpassning

### Ändra färger och stil
Redigera `viewer.html` och uppdatera CSS:

```css
/* Ändra huvudfärger */
body {
    background: linear-gradient(135deg, #YOUR_COLOR_1 0%, #YOUR_COLOR_2 100%);
}

/* Ändra accentfärg */
.day-card {
    border-left: 5px solid #YOUR_ACCENT_COLOR;
}
```

### Lägga till logotyp
I `viewer.html`, lägg till ovanför `<h1>`:

```html
<img src="path/to/logo.png" alt="Logo" style="max-width: 200px; margin: 0 auto; display: block;">
```

### Ändra typsnitt
```css
body {
    font-family: 'Din egen fontfamilj', Arial, sans-serif;
}
```

## Support och frågor

För specifika integrationsfrågor, öppna ett issue på GitHub.
