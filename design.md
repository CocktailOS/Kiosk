# Design — CocktailOS

## Genre

Modern-minimal. CocktailOS ist eine Kiosk-Workbench: Rezept zuerst, Administration ruhig im Hintergrund, keine dekorativen Oberflächen ohne Bediennutzen.

## Macrostructure family

- App-Seiten: Workbench mit einer kompakten Kopfzeile, klarer Arbeitsfläche und einer dauerhaften Navigationsleiste für Verwaltung.
- Dialoge: Eine Aufgabe pro Fläche, mit einer eindeutigen Hauptaktion und großen Touch-Zielen.

## Theme

- Primary: dunkles Lila
- Surfaces: neutrale, leicht warme Grauwerte
- Information: Lila ist Auswahl und Aktion; Warnung und Fehler bleiben semantisch eigenständig.
- Light/Dark: gleiche Hierarchie, keine Transparenz als Trennung von Flächen.

## Typography and spacing

- Body: Inter/System UI, klare Gewichte statt Display-Schrift.
- 4-Pixel-Rhythmus; Standardabstände 8, 12, 16 und 24 Pixel.
- Klickziele mindestens 44 Pixel.

## Motion

- Nur Farbe und Opazität für Statuswechsel, 160 ms.
- Reduzierte Bewegung: keine zusätzlichen Übergänge.

## Interaction stance

- Eine Hauptaktion pro Dialog.
- Sichtbare Tastaturfokusse und stabile Flächen ohne Layoutsprünge.
- Kein dekorativer Glanz, keine automatischen Animationen.

## Display target

Die primäre Kioskfläche ist 1024 × 600 Pixel. Startseite: sechs Cocktails in einer lesbaren 3×2-Fläche. Bei kleinerer Breite reflowt die Bedienung ohne horizontales Überlaufen.
