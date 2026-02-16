# Elf Forest Saga (C# / Windows)

Dwuwymiarowa gra platformowa inspirowana stylem Mario i klimatem fantasy z początku lat 2000, z nowocześniejszym detalem animacji.

## Fabuła
Aerin to elf o długich blond włosach i bardziej ludzkich rysach twarzy inspirowanych klasycznymi bohaterami high fantasy. Gdy Serce Lasu Aelorii gaśnie, na krainę rusza armia orków, goblinów, zombie i starych czarodziei. Aerin przemierza 21 rozdziałów-przygód, skacząc po ruinach, drzewach i mostach, by ocalić las.

## Cechy gry
- 21 poziomów kampanii (ostatni to arena tylko ze smokiem).
- Przewijany ekran (kamera podąża za graczem jak w klasycznych platformówkach).
- Styl wizualny fantasy z warstwowym, „malowanym” tłem (parallax, góry, las).
- Możliwość użycia modeli 3D/renderów postaci i tła (import z menu):
  - obsługiwane pliki postaci: `.obj`, `.fbx`, `.gltf`, `.glb`, `.blend` oraz obrazki `.png/.jpg`,
  - gra tworzy/czyta podgląd sprite (z pliku podglądu obok modelu lub z automatycznego placeholdera),
  - mechanizm cache ogranicza przeładowania, aby nie spowalniać gry po zmianach.
- Szczegółowy bohater z twarzą/włosami/strojem w stylu fantasy (render 3D jako sprite).
- Sylwetki przeciwników rysowane bardziej organicznie (bez prostych kwadratowych bloków), z cieniowaniem i animacją kończyn.
- 2 bronie do wyboru:
  - Łuk
  - Ogień z dłoni
  - przełączanie klawiszem `Z`
- Przeciwnicy (orki, gobliny, zombie, starzy czarodzieje) z animowanymi kończynami, realistyczniejszymi twarzami i aktywnymi atakami:
  - gobliny mają topory, ohydne twarze, zmarszczki i strzelają z łuku,
  - czarodzieje mają długie siwe włosy, laski w dłoni i są bardziej mobilni (także na platformach),
  - ruch ust i detale twarzy (zmarszczki/oczy) w renderze proceduralnym fallback.
- Co 5. poziom pojawiają się atakujące orły.
- Pickupy na poziomach:
  - fiolka z zielonym eliksirem = zdrowie,
  - czerwona fiolka laboratoryjna = szybszy/mocniejszy atak.
- Zdrowie startowe elfa: `100`.
- Menu pod `Esc`:
  - wybór poziomu,
  - regulacja jasności,
  - restart poziomu,
  - wyjście z gry,
  - import własnej muzyki (WAV),
  - import tła globalnego i tła dla wybranego poziomu,
  - import modelu/renderu elfa,
  - import modelu/renderu dla: orków, goblinów, zombie, czarodziejów i smoka,
  - cofanie zmian osobno dla każdego elementu oraz „Cofnij wszystko”.
- Finałowy boss: 21. poziom to samotny pojedynek ze smokiem o bardziej wężowym, mobilnym ciele (łapy, skrzydła, kolce, zianie ogniem).
- Film końcowy z podsumowaniem gry (pokonani wrogowie, obrażenia, los smoka).

## Muzyka (Twoje nagranie z linku YouTube)
Repozytorium nie zawiera bezpośrednio pliku z YouTube. Aby użyć wskazanego utworu:
1. Przygotuj lokalny plik `.wav` z muzyką (np. `custom-theme.wav`).
2. Umieść go w folderze:
   `ElfForestSaga.Game/Assets/music/custom-theme.wav`
3. Podczas uruchomienia gra automatycznie wykryje i zapętli ten plik.

Jeśli plik nie istnieje, gra odtwarza awaryjną melodię proceduralną.

## Podmiana grafiki/muzyki/modeli przez menu
1. W trakcie gry naciśnij `Esc`.
2. Wybierz poziom (jeśli chcesz podmienić tło tylko na konkretnym poziomie).
3. Kliknij odpowiednie przyciski importu (muzyka, tło globalne, tło poziomu, elf, orki/gobliny/zombie/czarodzieje/smok).
4. Gra zapisze pliki do katalogu `Assets/` i natychmiast je załaduje (bez restartu aplikacji).
5. W menu możesz cofnąć każdą zmianę osobno lub przyciskiem „Cofnij wszystko”.

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
- `ElfForestSaga.Game/LevelFactory.cs` - generowanie 21 poziomów (finałowy boss level).
- `ElfForestSaga.Game/GameObjects.cs` - modele obiektów gry.
- `ElfForestSaga.Game/SoundtrackPlayer.cs` - obsługa muzyki (lokalny WAV + fallback).
- `ElfForestSaga.Game/EndingCinematicForm.cs` - film końcowy z podsumowaniem kampanii.
