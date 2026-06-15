using System.Reflection;

namespace GitClone;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        LoadNativeGit();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Ui.MainForm());
    }

    /// <summary>
    /// The native git2 library is embedded as a resource in this single-file exe. LibGit2Sharp loads
    /// its native dependency from disk, so extract it to a temp folder and point LibGit2Sharp there.
    /// </summary>
    private static void LoadNativeGit()
    {
        var asm = Assembly.GetExecutingAssembly();
        string? resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.StartsWith("git2-", StringComparison.OrdinalIgnoreCase)
                                 && n.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
        if (resName is null)
        {
            MessageBox.Show(
                "Native Git-Bibliothek wurde nicht in die EXE eingebettet (Build-Problem).",
                "GitClone", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string dir = Path.Combine(Path.GetTempPath(), "GitClone-native");
        Directory.CreateDirectory(dir);
        string dest = Path.Combine(dir, resName);
        if (!File.Exists(dest))
        {
            using var stream = asm.GetManifestResourceStream(resName)!;
            using var file = File.Create(dest);
            stream.CopyTo(file);
        }

        LibGit2Sharp.GlobalSettings.NativeLibraryPath = dir;
    }
}
