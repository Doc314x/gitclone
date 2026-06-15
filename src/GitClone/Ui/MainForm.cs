using System.Diagnostics;
using GitClone.Models;
using GitClone.Services;

namespace GitClone.Ui;

public partial class MainForm : Form
{
    private readonly DeviceFlowAuthenticator _auth = new();
    private string? _token;
    private IGitHubService? _github;

    private readonly TextBox _log = new() { Multiline = true, Dock = DockStyle.Bottom, Height = 140, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly Button _loginButton = new() { Text = "Login bei GitHub", Dock = DockStyle.Top, Height = 36 };
    private readonly TextBox _targetFolder = new() { Dock = DockStyle.Top };

    private readonly ListView _repoList = new() { View = View.Details, CheckBoxes = true, FullRowSelect = true, Dock = DockStyle.Fill };
    private readonly ListView _archiveList = new() { View = View.Details, FullRowSelect = true, Dock = DockStyle.Fill };
    private readonly CheckBox _deleteAfterBackup = new();

    public MainForm()
    {
        Text = "GitClone — Lokales GitHub-Repo-Archiv";
        Width = 950;
        Height = 680;

        _loginButton.Click += async (_, _) => await LoginAsync();

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildBackupTab());
        tabs.TabPages.Add(BuildRestoreTab());

        Controls.Add(tabs);
        Controls.Add(_log);
        Controls.Add(BuildFolderBar());
        Controls.Add(_loginButton);
    }

    private Control BuildFolderBar()
    {
        // A 3-column grid (label | stretching textbox | button) lays out cleanly and shows the path.
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(6, 5, 6, 5)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        var caption = new Label { Text = "Zielordner:", AutoSize = true, Anchor = AnchorStyles.Left };

        _targetFolder.Dock = DockStyle.None;
        _targetFolder.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _targetFolder.Margin = new Padding(6, 3, 6, 3);

        var browse = new Button { Text = "Durchsuchen…", Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 2) };
        browse.Click += (_, _) => PickTargetFolder();

        table.Controls.Add(caption, 0, 0);
        table.Controls.Add(_targetFolder, 1, 0);
        table.Controls.Add(browse, 2, 0);
        return table;
    }

    private void PickTargetFolder()
    {
        try
        {
            Log("Öffne Ordner-Auswahl…");
            using var dialog = new FolderBrowserDialog
            {
                Description = "Zielordner für Backups wählen",
                ShowNewFolderButton = true
            };
            if (!string.IsNullOrWhiteSpace(_targetFolder.Text) && Directory.Exists(_targetFolder.Text))
                dialog.SelectedPath = _targetFolder.Text;

            var result = dialog.ShowDialog(this);
            Log($"Ordner-Auswahl: {result}" + (result == DialogResult.OK ? $" → {dialog.SelectedPath}" : ""));
            if (result == DialogResult.OK)
                _targetFolder.Text = dialog.SelectedPath;
        }
        catch (Exception ex)
        {
            Log("Ordner-Auswahl fehlgeschlagen: " + ex.GetType().Name + " — " + ex.Message);
        }
    }

    private TabPage BuildBackupTab()
    {
        var page = new TabPage("Backup");
        _repoList.Columns.Add("Repo", 320);
        _repoList.Columns.Add("Privat", 60);
        _repoList.Columns.Add("Letzter Push", 160);
        _repoList.Columns.Add("Größe (KB)", 100);

        var refresh = new Button { Text = "Repos laden", Dock = DockStyle.Top, Height = 30 };
        refresh.Click += async (_, _) => await LoadReposAsync();

        var backup = new Button { Text = "Sichern", Dock = DockStyle.Bottom, Height = 36 };
        backup.Click += async (_, _) => await BackupSelectedAsync(_deleteAfterBackup.Checked);

        // Delete is opt-in and off by default: backing up without deleting is the safe default.
        _deleteAfterBackup.Text = "Nach erfolgreichem Backup auf GitHub löschen (mit Sicherheitsabfrage)";
        _deleteAfterBackup.Dock = DockStyle.Bottom;
        _deleteAfterBackup.Height = 26;
        _deleteAfterBackup.Padding = new Padding(6, 0, 0, 0);

        page.Controls.Add(_repoList);
        page.Controls.Add(backup);
        page.Controls.Add(_deleteAfterBackup);
        page.Controls.Add(refresh);
        return page;
    }

    private TabPage BuildRestoreTab()
    {
        var page = new TabPage("Restore");
        _archiveList.Columns.Add("Repo", 320);
        _archiveList.Columns.Add("Privat", 60);
        _archiveList.Columns.Add("Gesichert am", 180);

        var scan = new Button { Text = "Archiv-Ordner scannen", Dock = DockStyle.Top, Height = 30 };
        scan.Click += (_, _) => ScanArchives();

        var restore = new Button { Text = "Ausgewähltes Repo zu GitHub wiederherstellen", Dock = DockStyle.Bottom, Height = 34 };
        restore.Click += async (_, _) => await RestoreSelectedAsync();

        page.Controls.Add(_archiveList);
        page.Controls.Add(restore);
        page.Controls.Add(scan);
        return page;
    }

    private void Log(string message) =>
        _log.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");

    private async Task LoginAsync()
    {
        try
        {
            _loginButton.Enabled = false;
            Log("Fordere Geräte-Code an…");
            var code = await _auth.RequestCodeAsync();
            Clipboard.SetText(code.UserCode);
            Process.Start(new ProcessStartInfo(code.VerificationUri) { UseShellExecute = true });
            Log($"Code {code.UserCode} (in Zwischenablage) im Browser eingeben. Warte auf Bestätigung…");

            _token = await _auth.WaitForTokenAsync(code);
            _github = new GitHubService(_token);
            _loginButton.Text = "Angemeldet ✓";
            Log("Login erfolgreich.");
        }
        catch (Exception ex)
        {
            Log("Login fehlgeschlagen: " + ex.Message);
            _loginButton.Enabled = true;
        }
    }

    private async Task LoadReposAsync()
    {
        if (_github is null) { Log("Bitte zuerst einloggen."); return; }
        Log("Lade Repos…");
        var repos = await _github.ListReposAsync();
        _repoList.Items.Clear();
        foreach (var r in repos)
        {
            var item = new ListViewItem(r.FullName) { Tag = r };
            item.SubItems.Add(r.Private ? "ja" : "nein");
            item.SubItems.Add(r.PushedAt?.ToString("yyyy-MM-dd") ?? "—");
            item.SubItems.Add(r.SizeKb.ToString());
            _repoList.Items.Add(item);
        }
        Log($"{repos.Count} Repos geladen.");
    }

    private async Task BackupSelectedAsync(bool deleteAfter)
    {
        if (_github is null || _token is null) { Log("Bitte zuerst einloggen."); return; }
        string target = _targetFolder.Text.Trim();
        if (string.IsNullOrEmpty(target)) { Log("Bitte einen Zielordner angeben."); return; }

        var archiver = new GitArchiver();
        foreach (ListViewItem item in _repoList.CheckedItems)
        {
            var info = (RepoInfo)item.Tag!;
            try
            {
                Log($"Sichere {info.FullName}…");
                string zip = await Task.Run(() => archiver.Archive(info, target, _token));
                var verify = await Task.Run(() => archiver.Verify(zip));
                if (!verify.Ok) { Log($"{info.FullName}: {verify.Message} — NICHT gelöscht."); continue; }
                Log($"{info.FullName}: {verify.Message}");

                if (deleteAfter && ConfirmDelete(info))
                {
                    await _github.DeleteRepoAsync(info.Owner, info.Name);
                    Log($"{info.FullName} auf GitHub gelöscht.");
                }
            }
            catch (Exception ex)
            {
                Log($"{info.FullName}: Fehler — {ex.Message}");
            }
        }
        Log("Backup-Lauf fertig.");
    }

    private bool ConfirmDelete(RepoInfo info)
    {
        using var dialog = new Form { Text = "Löschen bestätigen", Width = 420, Height = 170, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent };
        var label = new Label { Text = $"Backup verifiziert. Zum Löschen auf GitHub den Repo-Namen tippen:\n{info.Name}", Dock = DockStyle.Top, Height = 60 };
        var input = new TextBox { Dock = DockStyle.Top };
        var ok = new Button { Text = "Endgültig löschen", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };
        dialog.Controls.Add(input);
        dialog.Controls.Add(label);
        dialog.Controls.Add(ok);
        dialog.AcceptButton = ok;
        return dialog.ShowDialog(this) == DialogResult.OK && input.Text == info.Name;
    }

    private void ScanArchives()
    {
        string target = _targetFolder.Text.Trim();
        if (string.IsNullOrEmpty(target)) { Log("Bitte einen Zielordner angeben."); return; }

        var entries = new ArchiveIndex().List(target);
        _archiveList.Items.Clear();
        foreach (var e in entries)
        {
            var item = new ListViewItem($"{e.Metadata.Owner}/{e.Metadata.Name}") { Tag = e };
            item.SubItems.Add(e.Metadata.Private ? "ja" : "nein");
            item.SubItems.Add(e.Metadata.ArchivedAt.ToString("yyyy-MM-dd HH:mm"));
            _archiveList.Items.Add(item);
        }
        Log($"{entries.Count} Archive gefunden.");
    }

    private async Task RestoreSelectedAsync()
    {
        if (_github is null || _token is null) { Log("Bitte zuerst einloggen."); return; }
        if (_archiveList.SelectedItems.Count == 0) { Log("Bitte ein Archiv auswählen."); return; }

        var entry = (ArchiveEntry)_archiveList.SelectedItems[0].Tag!;
        try
        {
            Log($"Archiv {entry.Metadata.Name}: {entry.Metadata.RefCount} Refs / {entry.Metadata.CommitCount} Commits.");
            if (entry.Metadata.RefCount == 0)
            {
                Log("Archiv ist leer — Wiederherstellung abgebrochen. Bitte das Backup erneut erstellen.");
                return;
            }
            Log($"Lege {entry.Metadata.Name} auf GitHub neu an…");
            var created = await _github.CreateRepoAsync(entry.Metadata);
            Log($"Pushe Mirror nach {created.CloneUrl}…");
            await Task.Run(() => new GitRestorer().PushArchiveTo(entry.ZipPath, created.CloneUrl, _token));
            Log($"{entry.Metadata.Name} wiederhergestellt.");
        }
        catch (Exception ex)
        {
            Log($"Restore fehlgeschlagen: {ex.Message}");
        }
    }
}
