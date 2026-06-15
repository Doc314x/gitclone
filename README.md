# GitClone

Windows-Tool, das ausgewählte GitHub-Repos **vollständig** lokal als `.zip`-Archiv
sichert (kompletter Git-Verlauf, alle Branches & Tags), sie nach verifiziertem
Backup auf GitHub löscht und bei Bedarf aus dem Archiv **zurück nach GitHub**
wiederherstellt.

## Funktionsweise

- **Login:** OAuth Device Flow — Knopf drücken, im Browser mit 2FA bestätigen.
  Kein Token wird gespeichert. Scopes: `repo`, `delete_repo`, `workflow`
  (letzteres nötig, um Repos mit `.github/workflows/*` wiederherzustellen).
- **Backup:** Repo-Liste laden, ankreuzen, Zielordner wählen, „Sichern".
  Jedes Repo wird als `Owner__Name.zip` (bare Mirror + `metadata.json`) abgelegt
  und sofort verifiziert.
- **Löschen (optional):** Checkbox „Nach erfolgreichem Backup auf GitHub löschen"
  ist standardmäßig aus. Ist sie an, wird das Repo erst nach bestandener
  Verifikation **und** Eingabe des Repo-Namens entfernt.
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

Zielframework ist **net48** — die App nutzt die eingebaute 4.8-Laufzeit statt
einer fetten self-contained Runtime. **Costura.Fody** bündelt alle Abhängigkeiten
(inkl. der nativen Git-DLL) in **eine einzelne `GitClone.exe`**. Ein `v*`-Tag löst
den Release-Workflow aus, der diese `GitClone.exe` an ein GitHub-Release hängt.

## Versionierung

SemVer, Tags `vX.Y.Z`. Version steht in `src/GitClone/GitClone.csproj`.
Releases werden nie gelöscht.
