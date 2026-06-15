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

- Windows (x64). Kein installiertes Git nötig (LibGit2Sharp ist eingebettet).
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

## Release-Build (single-file .exe)

```bash
dotnet publish src/GitClone -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Im Repo passiert das automatisch: ein `v*`-Tag löst den Release-Workflow aus, der
die `.exe` baut und an ein GitHub-Release hängt.

## Versionierung

SemVer, Tags `vX.Y.Z`. Version steht in `src/GitClone/GitClone.csproj`.
Releases werden nie gelöscht.
