# GitClone

Windows-Tool, das ausgewählte GitHub-Repos **vollständig** lokal als `.zip`-Archiv
sichert (kompletter Git-Verlauf, alle Branches & Tags), sie nach verifiziertem
Backup auf GitHub löscht und bei Bedarf aus dem Archiv **zurück nach GitHub**
wiederherstellt.

## Funktionsweise

- **Login:** OAuth Device Flow — Knopf drücken, im Browser mit 2FA bestätigen.
  Kein Token wird gespeichert.
- **Backup:** Repo-Liste laden, ankreuzen, Zielordner wählen, „Sichern".
  Jedes Repo wird als `Owner__Name.zip` (bare Mirror + `metadata.json`) abgelegt
  und sofort verifiziert.
- **Löschen:** „Sichern & auf GitHub löschen" entfernt das Repo erst nach
  bestandener Verifikation und Eingabe des Repo-Namens.
- **Restore:** Archiv-Ordner scannen, Repo auswählen, es wird auf GitHub neu
  angelegt und der komplette Mirror zurückgepusht.

## Voraussetzungen

- Windows (x64) mit dem in Windows eingebauten **.NET Framework 4.8** (Standard
  auf Win10/11/Server). Keine zusätzliche Runtime, kein installiertes Git nötig
  (LibGit2Sharp wird mitgeliefert).
- Eine registrierte GitHub-OAuth-App mit aktivem Device Flow; die Client-ID ist
  in `src/GitClone/AppConfig.cs` hinterlegt.

## Build & Tests

Gebaut und getestet wird auf GitHub Actions (siehe `.github/workflows/ci.yml`);
lokal ist kein .NET-SDK nötig. Wer lokal bauen möchte:

```bash
dotnet build
dotnet test
dotnet run --project src/GitClone
```

## Auslieferung

Zielframework ist **net48** — die App nutzt die eingebaute 4.8-Laufzeit, daher
eine kleine `GitClone.exe` + ein paar DLLs (~6 MB) statt einer fetten
self-contained Runtime. Ein `v*`-Tag löst den Release-Workflow aus, der
`bin/Release/net48/` als `GitClone-net48.zip` an ein GitHub-Release hängt.

> Die Dateien im ZIP müssen zusammenbleiben. Ein optionales Single-File-Bundle
> (Costura) ist als spätere Variante möglich.

## Versionierung

SemVer, Tags `vX.Y.Z`. Version steht in `src/GitClone/GitClone.csproj`.
Releases werden nie gelöscht.
