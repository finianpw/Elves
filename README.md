# Elf Forest Saga (C# / Windows)

Dwuwymiarowa gra platformowa inspirowana stylem Mario i klimatem fantasy z początku lat 2000.

## Fabuła
Aerin to elf o długich blond włosach i rysach twarzy inspirowanych klasycznymi bohaterami high fantasy. Gdy Serce Lasu Aelorii gaśnie, na krainę rusza armia orków, goblinów, zombie i starych czarodziei. Aerin przemierza 20 rozdziałów-przygód, skacząc po kamiennych ruinach, drzewach i mostach, by ocalić las.

## Cechy gry
- 20 poziomów kampanii.
- Przewijany ekran (kamera podąża za graczem jak w klasycznych platformówkach).
- Styl wizualny inspirowany platformówkami 2D z początku lat 2000:
  - cieniowane tła,
  - kamienne platformy,
  - warstwowe dekoracje leśne,
  - bardziej szczegółowy sprite bohatera (twarz, włosy, strój).
- 2 bronie do wyboru:
  - Łuk (`1`)
  - Ogień z dłoni (`2`)
- Różni przeciwnicy: orki, gobliny, zombie, starzy czarodzieje.
- Pickupy na każdym poziomie:
  - dodatkowe zdrowie,
  - zwiększona szybkość strzału.
- Zdrowie startowe elfa: `100`.
- Menu pod `Esc`:
  - wybór poziomu,
  - regulacja jasności,
  - restart poziomu,
  - wyjście z gry.
- Muzyka tła (autorska pętla melodyczna generowana programowo).
- Film końcowy po ukończeniu całej kampanii.

## Sterowanie
- `A` / `D` lub `←` / `→`: ruch
- `Space`, `W`, `↑`: skok
- `F` lub `Ctrl`: atak
- `1` / `2`: zmiana broni
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
- `ElfForestSaga.Game/GameForm.cs` - główna pętla gry, logika, renderowanie, HUD, kamera, system jasności.
- `ElfForestSaga.Game/PauseMenuForm.cs` - menu ESC: poziomy, jasność, restart, wyjście.
- `ElfForestSaga.Game/LevelFactory.cs` - generowanie 20 poziomów.
- `ElfForestSaga.Game/GameObjects.cs` - modele obiektów gry.
- `ElfForestSaga.Game/SoundtrackPlayer.cs` - muzyka tła.
- `ElfForestSaga.Game/EndingCinematicForm.cs` - film końcowy.
