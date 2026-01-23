# Bidra till Matsedel RSS

Tack f�r ditt intresse att bidra till projektet! H�r �r n�gra riktlinjer.

## Hur kan jag bidra?

### Rapportera buggar
�ppna ett issue med:
- Beskrivning av problemet
- Steg f�r att �terskapa
- F�rv�ntad vs faktisk funktion
- Screenshots om m�jligt
- Milj�information (OS, .NET-version, etc.)

### F�resl� nya funktioner
�ppna ett issue med:
- Beskrivning av funktionen
- Motivering (varf�r beh�vs den?)
- Exempel p� anv�ndning
- Eventuella alternativ du �verv�gt

### Bidra med kod

1. **Forka projektet**
2. **Skapa en branch** f�r din feature/bugfix
   ```bash
   git checkout -b feature/min-nya-funktion
   ```
3. **G�r dina �ndringar**
   - F�lj befintlig kodstil
   - L�gg till kommentarer f�r komplexa delar
   - Testa dina �ndringar
4. **Committa**
   ```bash
   git commit -m "L�gg till: beskrivning av �ndring"
   ```
5. **Pusha till din fork**
   ```bash
   git push origin feature/min-nya-funktion
   ```
6. **�ppna en Pull Request**

## Kodstandard

### C# (.NET)
- Anv�nd PascalCase f�r klasser och metoder
- Anv�nd camelCase f�r privata f�lt och lokala variabler
- Anv�nd async/await f�r asynkrona operationer
- Hantera exceptions p� l�mpligt s�tt
- Kommentera komplexa logik

### HTML/CSS/JavaScript
- Anv�nd semantisk HTML
- CSS med logisk gruppering
- Modern JavaScript (ES6+)
- Kommentarer f�r icke-sj�lvklara delar

## Testning

Innan du submittar en PR:
1. Bygg projektet: `dotnet build`
2. K�r programmet: `dotnet run`
3. Verifiera att RSS-feeds genereras korrekt
4. Testa viewer.html i flera webbl�sare
5. Kontrollera att GitHub Actions workflow fungerar

## Utvecklingsmilj�

Rekommenderad setup:
- **IDE**: Visual Studio 2022 eller VS Code
- **.NET SDK**: 9.0 eller senare
- **Git**: Latest version
- **Webbl�sare**: Chrome/Edge f�r testning

**Ny till Visual Studio?** Se v�r kompletta guide: **[Visual Studio Setup Guide](VS-SETUP.md)**

## Projektstruktur att f�lja

```
MatsedelRss/
??? Program.cs          # Huvudlogik
??? output/             # Genererade RSS-feeds
??? viewer.html         # Frontend f�r visning
```

L�gg till nya features i separata klasser om de �r st�rre �n 100 rader.

## Commit-meddelanden

Anv�nd tydliga commit-meddelanden:
- `L�gg till: [funktion]` - Ny funktionalitet
- `Fixa: [problem]` - Buggfix
- `Uppdatera: [komponent]` - F�rb�ttring av befintlig funktion
- `Dokumentation: [�ndring]` - Dokumentationsuppdatering

## Pull Request Process

1. Uppdatera README.md om funktionen p�verkar anv�ndning
2. Uppdatera QUICKSTART.md om setup-processen �ndras
3. L�gg till/uppdatera kommentarer i koden
4. Beskriv dina �ndringar tydligt i PR-beskrivningen
5. L�nka till relaterade issues

## Id�er f�r bidrag

### Funktioner som skulle vara bra att ha:
- [ ] St�d f�r fler kommuner (konfigurerbart)
- [ ] Allergenerinformation
- [ ] Nutritionsinformation
- [ ] Veckomeny i PDF-format
- [ ] E-postnotiser
- [ ] Slack/Discord-integration
- [ ] REST API
- [ ] Mobilapp
- [ ] PWA-st�d
- [ ] Offline-l�ge
- [ ] Favoritmatr�tter
- [ ] Recept/Instruktioner
- [ ] Bildst�d f�r matr�tter

### F�rb�ttringar:
- [ ] B�ttre HTML-parsning (hantera fler format)
- [ ] Mer robust felhantering
- [ ] Caching f�r snabbare laddning
- [ ] Enhetstester
- [ ] Integrationstester
- [ ] CI/CD-f�rb�ttringar
- [ ] Docker-support
- [ ] Logging/telemetri
- [ ] Prestanda-optimeringar

### Dokumentation:
- [ ] Video-tutorial
- [ ] Fler exempel p� digital signage-integration
- [ ] �vers�ttningar till andra spr�k
- [ ] API-dokumentation
- [ ] Architecture Decision Records (ADRs)

## Fr�gor?

Om du har fr�gor, �ppna ett issue med label "question" eller kontakta projekt�garen.

## Licens

Genom att bidra till projektet godk�nner du att dina bidrag licensieras under MIT License.
