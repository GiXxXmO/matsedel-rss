# Test RSS-feeds lokalt

För att testa projektet lokalt:

## 1. Bygg projektet
```bash
dotnet build MatsedelRss/MatsedelRss.csproj
```

## 2. Kör programmet
```bash
dotnet run --project MatsedelRss/MatsedelRss.csproj
```

## 3. Kontrollera output
RSS-filerna skapas i:
- `MatsedelRss/output/matsedel-vecka.xml`
- `MatsedelRss/output/matsedel-dagens.xml`

## 4. Testa RSS-feeds
Öppna XML-filerna i en webbläsare eller RSS-läsare för att validera innehållet.

## GitHub Actions
När du pushar till GitHub kommer filerna automatiskt uppdateras dagligen via GitHub Actions.

## Felsökning

### Problem med att hitta matsedeln
Om programmet inte hittar matsedeln, kan URL-strukturen ha ändrats. Kontrollera:
1. Besök https://www.skara.se/forskolaskolaochutbildning/matiskolaochforskola/matsedelforskolaochskola/
2. Hitta aktuell månads sida
3. Uppdatera URL-genereringen i `Program.cs`

### Problem med parsning
Om HTML-strukturen har ändrats på webbplatsen behöver parsning-logiken uppdateras i metoderna:
- `ParseTable()`
- `ParseDateHeaders()`
