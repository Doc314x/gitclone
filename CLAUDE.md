# CLAUDE.md — GitClone

## Was das ist
WinForms-Tool (.NET 8) zum lokalen Mirror-Backup von GitHub-Repos, Löschen nach
verifiziertem Backup und Restore zurück nach GitHub. Git via LibGit2Sharp (kein
externes git.exe), GitHub-API via Octokit, Login via OAuth Device Flow
(Token nur im Speicher, nie gespeichert).

## Struktur
- `src/GitClone/Services/` — Kernlogik: GitArchiver (Backup+Verify), GitRestorer,
  ArchiveIndex, GitHubService (Octokit), DeviceFlowAuthenticator, GitStats.
- `src/GitClone/Models/` — RepoInfo, ArchiveMetadata, RepoStats, VerificationResult.
- `src/GitClone/Ui/MainForm.cs` — gesamte GUI.
- `tests/GitClone.Tests/` — xUnit. Git-Tests laufen hermetisch gegen lokale
  Repos (siehe `GitTestRepo`), nicht gegen GitHub.

## Build & Tests
Gebaut/getestet wird ausschließlich auf GitHub Actions (kein lokales SDK):
- `.github/workflows/ci.yml` — build+test bei Push/PR auf `main`.
- `.github/workflows/release.yml` — single-file `.exe` bei `v*`-Tag.
Lokal (falls SDK vorhanden): `dotnet test` läuft komplett offline, kein Token nötig.

## Konventionen
- SemVer; Version in `src/GitClone/GitClone.csproj`. Tag `vX.Y.Z` löst Release aus.
- Bei jeder Verhaltensänderung README.md und diese Datei im selben Commit nachführen.
- Archivformat: `Owner__Name.zip` mit `repo.git/` (bare Mirror) + `metadata.json`.

## Bekannte LibGit2Sharp-Fallstricke
- **Push expandiert keine Wildcard-Refspecs** — Refs einzeln aufzählen
  (`GitRestorer`). Fetch dagegen kann Wildcards.
- Ein bare Clone befüllt `refs/heads/*` nicht verlässlich; Tests fetchen daher
  von einem Arbeits-Repo als Quelle.
