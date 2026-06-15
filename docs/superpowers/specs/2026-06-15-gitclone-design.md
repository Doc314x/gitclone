# GitClone — Lokales GitHub-Repo-Archiv

**Datum:** 2026-06-15
**Status:** Freigegeben (Design)
**Repo:** Doc314x/gitclone (privat)

## Ziel

Ein Windows-GUI-Tool, das ungenutzte GitHub-Repos **vollständig** lokal sichert
(kompletter Git-Verlauf, alle Branches & Tags), die Repos anschließend auf GitHub
löscht und sie bei Bedarf aus dem lokalen Archiv **wieder nach GitHub** herstellt.
Zweck: den GitHub-Account auf das Wesentliche reduzieren, ohne Daten zu verlieren.

## Nicht-Ziele (YAGNI)

- Kein Sichern von Issues, Pull Requests oder Wiki (nur Git-Daten + Repo-Metadaten
  für die Wiederherstellung).
- Keine plattformübergreifende GUI (Windows-only).
- Kein automatisches/zeitgesteuertes Backup — der Anwender wählt manuell aus.
- Kein Mitsichern von Release-Assets (kann später als Erweiterung kommen).

## Tech-Stack

- **WinForms-App, .NET 8, C#.**
- Build als **self-contained, single-file `win-x64` .exe** — kein installiertes
  .NET-Runtime und kein `git.exe` auf dem Zielrechner nötig.
- **Git-Engine:** LibGit2Sharp (native Bibliothek, in die .exe gebündelt) —
  vollständiger Mirror ohne externes Git.
- **GitHub-API:** Octokit.NET (offizieller .NET-Client).
- **Auth:** OAuth **Device Flow**. Das ausgestellte Token wird **nicht
  gespeichert** — es lebt nur im Arbeitsspeicher der laufenden Sitzung. Bei jedem
  Start meldet man sich per Knopf neu an (Browser + 2FA). Bewusste Entscheidung:
  das Tool wird selten benutzt, kein dauerhaftes Token auf der Platte.

### Einmalige Voraussetzung (erledigt)

Eine bei GitHub registrierte **OAuth-App** liefert eine `Client-ID`, die fest in
die .exe kompiliert wird. Benötigte Scopes: `repo` (privat lesen, anlegen,
Mirror-Push) und `delete_repo` (Löschen) — werden beim Device-Flow-Request
angefragt, nicht in der App konfiguriert. Das Device-Flow-Token dient beim
Restore-Push gleichzeitig als HTTPS-Passwort gegenüber GitHub.

- **OAuth-App:** „GitClone" (registriert, *Enable Device Flow* aktiv)
- **Client ID:** `Ov23lisIxJSnEoMAdIzj` (öffentlich, kein Client Secret nötig —
  public client)

## Komponenten

Jede Unit hat eine klar abgegrenzte Aufgabe und ist einzeln testbar:

| Unit | Aufgabe | Abhängigkeiten |
|---|---|---|
| `DeviceFlowAuthenticator` | OAuth-Device-Flow durchführen, Token (nur im Speicher) zurückgeben | Octokit.Oauth |
| `GitHubService` | Octokit-Wrapper: `ListRepos`, `GetMetadata`, `CreateRepo`, `DeleteRepo` | Octokit, Token |
| `GitArchiver` | Mirror-Clone → `metadata.json` schreiben → in `.zip` packen → verifizieren | LibGit2Sharp, Token |
| `GitRestorer` | `.zip` entpacken → Repo auf GitHub anlegen → alle Refs zurückpushen | LibGit2Sharp, GitHubService |
| `ArchiveIndex` | Zielordner scannen, vorhandene Backups aus `.zip`/Metadaten auflisten | Dateisystem |
| `MainForm` (UI) | Login-Status, Backup-Tab, Restore-Tab, Fortschritts-/Log-Panel | alle obigen Services |

Die UI kennt nur die Service-Schnittstellen; die Services kennen die UI nicht.

## Archiv-Format

Pro Repo eine Datei im Zielordner: **`<owner>__<repo>.zip`**, enthält:

- `repo.git/` — bare Mirror (kompletter Verlauf, alle Branches & Tags).
  Erzeugt durch Clone mit Refspec `+refs/heads/*:refs/heads/*` plus Tags
  (vollständige Wiederherstellbarkeit von Code + Historie).
- `metadata.json` — für Auflistung und Wiederherstellung:

```json
{
  "owner": "Doc314x",
  "name": "beispiel-repo",
  "description": "…",
  "private": true,
  "defaultBranch": "main",
  "topics": ["…"],
  "sourceUrl": "https://github.com/Doc314x/beispiel-repo.git",
  "archivedAt": "2026-06-15T12:00:00Z",
  "refCount": 7,
  "commitCount": 142
}
```

`refCount`/`commitCount` werden beim Backup ermittelt und bei der Verifikation
erneut aus dem entpackten Mirror geprüft.

## Abläufe

### Backup

1. Login (Device Flow, Token ggf. aus Cache).
2. `ListRepos` → Tabelle mit Name, privat?, letzter Push, Größe + Checkboxen.
3. Anwender wählt Zielordner und Repos.
4. Je ausgewähltem Repo:
   a. Mirror-Clone via LibGit2Sharp in ein temporäres bare Repo.
   b. `metadata.json` schreiben (inkl. ref-/commitCount).
   c. In `<owner>__<repo>.zip` packen.
   d. **Verifikation:** Zip erneut öffnen, Mirror laden, Refs/Commits zählen und
      mit den Erwartungswerten abgleichen.
5. Optional „**Sichern & Löschen**": nur wenn Verifikation bestanden →
   Sicherheitsabfrage (Anwender muss Repo-Namen exakt eintippen) → `DeleteRepo`.

### Restore

1. `.zip` aus dem Archiv wählen.
2. `metadata.json` lesen.
3. Repo auf GitHub neu anlegen (`CreateRepo`: Name, Beschreibung, privat,
   Default-Branch).
4. Zip in temporäres bare Repo entpacken.
5. Alle Refs (Branches + Tags) zum neuen Remote pushen.
6. Aufräumen (Temp löschen).

## Fehlerbehandlung & Sicherheit

- Auf GitHub wird **nur** gelöscht, wenn (a) die Verifikation bestanden ist **und**
  (b) der Anwender den Repo-Namen exakt eingetippt hat. Sonst niemals.
- Ein fehlgeschlagenes oder nicht verifiziertes Backup blockiert das Löschen.
- Netzwerk-/API-/Git-Fehler werden abgefangen und im Log-Panel angezeigt; der
  betroffene Vorgang gilt als fehlgeschlagen und wird nicht als erfolgreich markiert.
- Restore warnt und bricht ab, falls der Repo-Name auf GitHub bereits existiert.
- Temporäre Arbeitsverzeichnisse werden nach jedem Vorgang aufgeräumt.

## Tests

- **Unit-Tests:** `metadata.json` Serialisierung/Deserialisierung; `ArchiveIndex`
  (Zielordner mit gemischten Dateien korrekt einlesen); Verifikationslogik
  (manipuliertes/kaputtes Zip wird erkannt).
- **Integrationstest (optional, per Token-Secret gated):** gegen ein
  Wegwerf-Test-Repo — Backup → Zip prüfen → Restore in ein neues Repo → Refs
  vergleichen.

## Versionierung & CI (GitHub-Standards)

- **SemVer**, Tags `vX.Y.Z`. Releases werden nie gelöscht.
- **GH-Actions-Workflow:**
  - CI-Build bei Push/PR (`dotnet build` + `dotnet test`).
  - Release bei Tag `v*`: `dotnet publish` (self-contained, single-file,
    `win-x64`) → `.exe` als Release-Asset hochladen + GitHub-Release anlegen.
- **README.md** und **CLAUDE.md** von Anfang an vorhanden; Doku wird im selben
  Commit wie Code-Änderungen nachgeführt.

## Offene Punkte / spätere Erweiterungen

- Mitsichern von Release-Assets (Dateien, die nicht im Git-Verlauf liegen).
- Optionale Verschlüsselung des Archivs.
- „Ungenutzt"-Vorfilter nach letztem Push-Datum als Komfortfunktion.
