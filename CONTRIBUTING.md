# Bidra till Matsedel RSS

Tack för ditt intresse att bidra till projektet! Här är några riktlinjer.

## Hur kan jag bidra?

### Rapportera buggar
Öppna ett issue med:
- Beskrivning av problemet
- Steg för att återskapa
- Förväntad vs faktisk funktion
- Screenshots om möjligt
- Miljöinformation (OS, .NET-version, etc.)

### Föreslå nya funktioner
Öppna ett issue med:
- Beskrivning av funktionen
- Motivering (varför behövs den?)
- Exempel på användning
- Eventuella alternativ du övervägt

### Bidra med kod

1. **Forka projektet**
2. **Skapa en branch** för din feature/bugfix
   ```bash
   git checkout -b feature/min-nya-funktion
   ```
3. **Gör dina ändringar**
   - Följ befintlig kodstil
   - Lägg till kommentarer för komplexa delar
   - Testa dina ändringar
4. **Committa**
   ```bash
   git commit -m "Lägg till: beskrivning av ändring"
   ```
5. **Pusha till din fork**
   ```bash
   git push origin feature/min-nya-funktion
   ```
6. **Öppna en Pull Request**

## Kodstandard

### C# (.NET)
- Använd PascalCase för klasser och metoder
- Använd camelCase för privata fält och lokala variabler
- Använd async/await för asynkrona operationer
- Hantera exceptions på lämpligt sätt
- Kommentera komplexa logik

### HTML/CSS/JavaScript
- Använd semantisk HTML
- CSS med logisk gruppering
- Modern JavaScript (ES6+)
- Kommentarer för icke-självklara delar

## Testning

Innan du submittar en PR:
1. Bygg projektet: `dotnet build`
2. Kör programmet: `dotnet run`
3. Verifiera att RSS-feeds genereras korrekt
4. Testa viewer.html i flera webbläsare
5. Kontrollera att GitHub Actions workflow fungerar

## Utvecklingsmiljö

Rekommenderad setup:
- **IDE**: Visual Studio 2022 eller VS Code
- **.NET SDK**: 9.0 eller senare
- **Git**: Latest version
- **Webbläsare**: Chrome/Edge för testning

## Projektstruktur att följa

```
MatsedelRss/
??? Program.cs          # Huvudlogik
??? output/             # Genererade RSS-feeds
??? viewer.html         # Frontend för visning
```

Lägg till nya features i separata klasser om de är större än 100 rader.

## Commit-meddelanden

Använd tydliga commit-meddelanden:
- `Lägg till: [funktion]` - Ny funktionalitet
- `Fixa: [problem]` - Buggfix
- `Uppdatera: [komponent]` - Förbättring av befintlig funktion
- `Dokumentation: [ändring]` - Dokumentationsuppdatering

## Pull Request Process

1. Uppdatera README.md om funktionen påverkar användning
2. Uppdatera QUICKSTART.md om setup-processen ändras
3. Lägg till/uppdatera kommentarer i koden
4. Beskriv dina ändringar tydligt i PR-beskrivningen
5. Länka till relaterade issues

## Idéer för bidrag

### Funktioner som skulle vara bra att ha:
- [ ] Stöd för fler kommuner (konfigurerbart)
- [ ] Allergenerinformation
- [ ] Nutritionsinformation
- [ ] Veckomeny i PDF-format
- [ ] E-postnotiser
- [ ] Slack/Discord-integration
- [ ] REST API
- [ ] Mobilapp
- [ ] PWA-stöd
- [ ] Offline-läge
- [ ] Favoritmaträtter
- [ ] Recept/Instruktioner
- [ ] Bildstöd för maträtter

### Förbättringar:
- [ ] Bättre HTML-parsning (hantera fler format)
- [ ] Mer robust felhantering
- [ ] Caching för snabbare laddning
- [ ] Enhetstester
- [ ] Integrationstester
- [ ] CI/CD-förbättringar
- [ ] Docker-support
- [ ] Logging/telemetri
- [ ] Prestanda-optimeringar

### Dokumentation:
- [ ] Video-tutorial
- [ ] Fler exempel på digital signage-integration
- [ ] Översättningar till andra språk
- [ ] API-dokumentation
- [ ] Architecture Decision Records (ADRs)

## Frågor?

Om du har frågor, öppna ett issue med label "question" eller kontakta projektägaren.

## Licens

Genom att bidra till projektet godkänner du att dina bidrag licensieras under MIT License.
