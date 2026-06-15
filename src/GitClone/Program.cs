using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace GitClone;

internal static class Program
{
    /// <summary>Human-readable result of the native-library setup, shown in the log at startup.</summary>
    public static string NativeStatus { get; private set; } = "Native: (nicht initialisiert)";

    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string path);

    [STAThread]
    private static void Main()
    {
        LoadNativeGit();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Ui.MainForm());
    }

    /// <summary>
    /// The native git2 library is embedded as a resource. Extract it, preload it into the process by
    /// its exact name (so LibGit2Sharp's DllImport resolves to the already-loaded module), and also
    /// set NativeLibraryPath as a fallback.
    /// </summary>
    private static void LoadNativeGit()
    {
        var sb = new StringBuilder();
        sb.Append($"Native-Setup: 64bit-Prozess={Environment.Is64BitProcess}. ");
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            string? resName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.StartsWith("git2-", StringComparison.OrdinalIgnoreCase)
                                     && n.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            if (resName is null)
            {
                sb.Append("FEHLER: keine eingebettete git2-DLL. Ressourcen: "
                          + string.Join(", ", asm.GetManifestResourceNames()));
                NativeStatus = sb.ToString();
                return;
            }

            string dir = Path.Combine(Path.GetTempPath(), "GitClone-native");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, resName);
            using (var stream = asm.GetManifestResourceStream(resName)!)
            using (var file = File.Create(dest))
                stream.CopyTo(file);

            LibGit2Sharp.GlobalSettings.NativeLibraryPath = dir;

            IntPtr handle = LoadLibrary(dest);
            int err = Marshal.GetLastWin32Error();
            sb.Append($"git2='{resName}', {new FileInfo(dest).Length} Bytes. " +
                      (handle == IntPtr.Zero
                          ? $"LoadLibrary FEHLGESCHLAGEN (Win32-Fehler {err})."
                          : "LoadLibrary OK, NativeLibraryPath gesetzt."));
        }
        catch (Exception ex)
        {
            sb.Append("FEHLER beim Einrichten: " + ex.Message);
        }
        NativeStatus = sb.ToString();
    }
}
