# Highway Pursuit (C# / Windows)

Arcade'owa gra wyścigowa w klimacie klasycznego **Highway Pursuit**.

## Założenia rozgrywki
- Droga biegnie pionowo w górę ekranu (efekt pseudo-3D).
- Gracz prowadzi **zielony samochód**, który stopniowo się rozpędza.
- Na trasie pojawiają się auta ruchu:
  - **niebieskie**,
  - **czarne**,
  - **pomarańczowe** (strzelają pociskami do tyłu).
- Trzeba omijać auta i pociski jak najdłużej.
- Tło zmienia się płynnie wraz z pokonanymi kilometrami.

## Funkcje
- Menu startowe z wyborem stylu auta gracza (Sport / Muscle / Futuristic).
- Przycisk **Start** oraz **Wyjście z gry**.
- Sterowanie strzałkami (`←`, `→`, `↑`, `↓`).
- Pauza pod klawiszem **P**.
- `Esc` wraca z wyścigu do menu.
- HUD:
  - lewy górny róg: licznik kilometrów,
  - prawy górny róg: prędkość,
  - maksymalna prędkość: **250 km/h**.

## Uruchomienie na Windows
1. Zainstaluj .NET SDK 8.0.
2. Otwórz terminal w katalogu repozytorium.
3. Wykonaj:
   ```bash
   dotnet build ElfForestSaga.sln
   dotnet run --project ElfForestSaga.Game/ElfForestSaga.Game.csproj
   ```

## Główne pliki
- `ElfForestSaga.Game/MainMenuForm.cs` - menu główne.
- `ElfForestSaga.Game/RacingGameForm.cs` - pętla wyścigu, pseudo-3D, przeciwnicy, strzelanie, HUD.
- `ElfForestSaga.Game/Program.cs` - punkt wejścia uruchamiający menu.
