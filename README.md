# Elf Forest Saga (C# / Windows)

Dwuwymiarowa gra platformowa inspirowana klasycznym stylem Mario, osadzona w lesie fantasy.

## Fabuła
Aerin, elf o długich blond włosach, żyje w pradawnym lesie Aeloria. Gdy Serce Lasu zaczyna gasnąć, na krainę spada inwazja orków, goblinów, zombie i starych czarodziei. Bohater wyrusza w podróż przez 20 poziomów, skacząc po drzewach i mostach oraz walcząc dwiema broniami: łukiem i ogniem z dłoni.

Każdy poziom to kolejny rozdział opowieści. Pokonanie wszystkich przeciwników i dotarcie do końca mapy odblokowuje finałowy film końcowy, który domyka historię niczym ostatnie strony książki.

## Cechy gry
- 20 poziomów kampanii.
- Przewijany ekran (kamera podąża za graczem).
- 2 bronie do wyboru:
  - Łuk (`1`)
  - Ogień z dłoni (`2`)
- Różni przeciwnicy: orki, gobliny, zombie, starzy czarodzieje.
- Pickupy na każdym poziomie:
  - dodatkowe zdrowie,
  - zwiększona szybkość strzału.
- Zdrowie startowe elfa: `100`.
- Muzyka tła (autorska pętla melodyczna generowana programowo).
- Film końcowy po ukończeniu całej kampanii.

## Sterowanie
- `A` / `D` lub `←` / `→`: ruch
- `Space`, `W`, `↑`: skok
- `F` lub `Ctrl`: atak
- `1` / `2`: zmiana broni

## Uruchomienie na Windows
1. Zainstaluj .NET SDK 8.0.
2. Otwórz terminal w katalogu repozytorium.
3. Wykonaj:
   ```bash
   dotnet build ElfForestSaga.sln
   dotnet run --project ElfForestSaga.Game/ElfForestSaga.Game.csproj
   ```

## Struktura projektu
- `ElfForestSaga.Game/GameForm.cs` - główna pętla gry, logika, renderowanie, HUD, kamera.
- `ElfForestSaga.Game/LevelFactory.cs` - generowanie 20 poziomów.
- `ElfForestSaga.Game/GameObjects.cs` - modele obiektów gry.
- `ElfForestSaga.Game/SoundtrackPlayer.cs` - muzyka tła.
- `ElfForestSaga.Game/EndingCinematicForm.cs` - film końcowy.
