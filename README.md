# Elf Forest Saga (C# / Windows)

Dwuwymiarowa gra platformowa inspirowana stylem Mario i klimatem fantasy z początku lat 2000, z nowocześniejszym detalem animacji.

## Fabuła
Aerin to elf o długich blond włosach i bardziej ludzkich rysach twarzy inspirowanych klasycznymi bohaterami high fantasy. Gdy Serce Lasu Aelorii gaśnie, na krainę rusza armia orków, goblinów, zombie i starych czarodziei. Aerin przemierza 20 rozdziałów-przygód, skacząc po ruinach, drzewach i mostach, by ocalić las.

## Cechy gry
- 20 poziomów kampanii.
- Przewijany ekran (kamera podąża za graczem jak w klasycznych platformówkach).
- Styl wizualny fantasy z warstwowym, „malowanym” tłem (parallax, góry, las).
- Możliwość użycia dokładnie Twojego tła i dokładnego renderu elfa (plików graficznych):
  - `ElfForestSaga.Game/Assets/visuals/background-main.png`
  - `ElfForestSaga.Game/Assets/visuals/elf-3d.png`
  - jeśli pliki istnieją, gra renderuje je bezpośrednio zamiast fallbackowego rysowania kodem.
- Szczegółowy bohater z twarzą/włosami/strojem w stylu fantasy (render 3D jako sprite).
- Sylwetki przeciwników rysowane bardziej organicznie (bez prostych kwadratowych bloków), z cieniowaniem i animacją kończyn.
- 2 bronie do wyboru:
  - Łuk
  - Ogień z dłoni
  - przełączanie klawiszem `Z`
- Przeciwnicy (orki, gobliny, zombie, starzy czarodzieje) z animowanymi kończynami i aktywnymi atakami:
  - ataki kontaktowe,
  - czarodzieje dodatkowo strzelają pociskami.
- Pickupy na poziomach:
  - dodatkowe zdrowie,
  - zwiększona szybkość strzału.
- Zdrowie startowe elfa: `100`.
- Menu pod `Esc`:
  - wybór poziomu,
  - regulacja jasności,
  - restart poziomu,
  - wyjście z gry,
  - import własnej muzyki (WAV),
  - import własnego tła (PNG/JPG),
  - import własnego renderu/modelu elfa (PNG).
- Finałowy boss: Smok Strażnik na końcu 20. poziomu.
- Film końcowy z podsumowaniem gry (pokonani wrogowie, obrażenia, los smoka).

## Muzyka (Twoje nagranie z linku YouTube)
Repozytorium nie zawiera bezpośrednio pliku z YouTube. Aby użyć wskazanego utworu:
1. Przygotuj lokalny plik `.wav` z muzyką (np. `custom-theme.wav`).
2. Umieść go w folderze:
   `ElfForestSaga.Game/Assets/music/custom-theme.wav`
3. Podczas uruchomienia gra automatycznie wykryje i zapętli ten plik.

Jeśli plik nie istnieje, gra odtwarza awaryjną melodię proceduralną.

## Podmiana grafiki i muzyki przez menu
1. W trakcie gry naciśnij `Esc`.
2. Kliknij:
   - `Wgraj muzykę (.wav)`
   - `Wgraj tło (.png/.jpg)`
   - `Wgraj model elfa (.png)`
3. Gra zapisze pliki do katalogu `Assets/` i natychmiast je załaduje (bez restartu aplikacji).

## Sterowanie
- `A` / `D` lub `←` / `→`: ruch
- `Space`, `W`, `↑`: skok
- `F` lub `Ctrl`: atak
- `Z`: zmiana broni
- `Esc`: menu gry

## Uruchomienie na Windows
1. Zainstaluj .NET SDK 8.0.
2. Otwórz terminal w katalogu repozytorium.
3. Wykonaj:
   ```bash
   dotnet build ElfForestSaga.sln
   dotnet run --project ElfForestSaga.Game/ElfForestSaga.Game.csproj
   ```

## Struktura projektu
- `ElfForestSaga.Game/GameForm.cs` - pętla gry, render, animacje przeciwników, system ataków, HUD.
- `ElfForestSaga.Game/PauseMenuForm.cs` - menu ESC: poziomy, jasność, restart, wyjście.
- `ElfForestSaga.Game/LevelFactory.cs` - generowanie 20 poziomów.
- `ElfForestSaga.Game/GameObjects.cs` - modele obiektów gry.
- `ElfForestSaga.Game/SoundtrackPlayer.cs` - obsługa muzyki (lokalny WAV + fallback).
- `ElfForestSaga.Game/EndingCinematicForm.cs` - film końcowy z podsumowaniem kampanii.
