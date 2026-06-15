# CLAUDE.md — GitClone

## Was das ist
WinForms-Tool (**net48**, nutzt die in Windows eingebaute .NET-Framework-Laufzeit —
bewusst NICHT net8 self-contained, das wurde 165 MB groß) zum lokalen Mirror-Backup
von GitHub-Repos, Löschen nach
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
- `.github/workflows/release.yml` — baut net48 + Costura und hängt die einzelne
  `GitClone.exe` bei `v*`-Tag ans Release.
Lokal (falls SDK vorhanden): `dotnet test` läuft komplett offline, kein Token nötig.

## net48-Besonderheiten
- LangVersion `latest` + **PolySharp** polyfillt records/init/required für net48.
- WinForms/Drawing/Compression via `<Reference>` (in-box), nicht via UseWindowsForms.
- `System.Text.Json` als NuGet (net48 hat es nicht eingebaut).
- Program.cs nutzt `Application.EnableVisualStyles()` statt
  `ApplicationConfiguration.Initialize()` (nur .NET 6+).
- `TextBox.PlaceholderText` existiert in net48 NICHT.
- **Costura.Fody** + `PlatformTarget x64` bündeln alles in eine `GitClone.exe`.
  Die native git2-DLL wird als EmbeddedResource eingebettet und in Program.cs
  beim Start nach %TEMP% entpackt + per LoadLibrary geladen.
- **Native-Version-Falle:** `LibGit2Sharp.NativeBinaries` MUSS exakt die Version
  sein, die LibGit2Sharp via DllImport erwartet — für 0.30.0 ist das **2.0.322**
  (`git2-a418d9d.dll`). 2.0.323 (`git2-3f4182d.dll`) führt zu DllNotFound. Bei
  LibGit2Sharp-Upgrade die passende NativeBinaries-Version aus dem nuspec prüfen.

## OAuth-Scopes
`repo` + `delete_repo` + **`workflow`**. Ohne `workflow` lehnt GitHub Pushes ab,
die `.github/workflows/*` enthalten — das brach den Restore solcher Repos.

## Konventionen
- SemVer; Version in `src/GitClone/GitClone.csproj`. Tag `vX.Y.Z` löst Release aus.
- Bei jeder Verhaltensänderung README.md und diese Datei im selben Commit nachführen.
- Archivformat: `Owner__Name.zip` mit `repo.git/` (bare Mirror) + `metadata.json`.

## Bekannte LibGit2Sharp-Fallstricke
- **Push expandiert keine Wildcard-Refspecs** — Refs einzeln aufzählen
  (`GitRestorer`). Fetch dagegen kann Wildcards.
- Ein bare Clone befüllt `refs/heads/*` nicht verlässlich; Tests fetchen daher
  von einem Arbeits-Repo als Quelle.
