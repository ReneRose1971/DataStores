namespace TestHelper.DataStores.Fixtures;

/// <summary>
/// Shared Fixture für temporäre Test-Verzeichnisse.
/// Erstellt ein isoliertes Temp-Verzeichnis pro Testklasse und bereinigt es nach Tests.
/// </summary>
/// <remarks>
/// Verwendung mit xUnit IClassFixture für gemeinsames Setup pro Testklasse.
/// </remarks>
public class TempDirectoryFixture : IDisposable
{
    /// <summary>
    /// Root-Pfad des temporären Test-Verzeichnisses.
    /// Eindeutig pro Testklassen-Ausführung.
    /// </summary>
    public string TestRoot { get; }

    /// <summary>
    /// Erstellt ein neues temporäres Test-Verzeichnis unter dem angegebenen Unterordner.
    /// </summary>
    /// <param name="subFolder">Unterordner-Name für bessere Organisation (z.B. "LiteDb", "Json").</param>
    public TempDirectoryFixture(string subFolder)
    {
        TestRoot = Path.Combine(
            Path.GetTempPath(),
            "DataStores.Tests",
            subFolder,
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(TestRoot);
    }

    /// <summary>
    /// Bereinigt das temporäre Verzeichnis nach Testabschluss.
    /// Best-effort: Fehler beim Löschen werden ignoriert.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(TestRoot))
        {
            try
            {
                Directory.Delete(TestRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup - ignore errors
            }
        }
    }

    /// <summary>
    /// Erstellt einen vollständigen Dateipfad relativ zum Test-Root-Verzeichnis.
    /// </summary>
    /// <param name="relativePath">Relativer Pfad zur Datei (z.B. "test.db" oder "data/file.json").</param>
    /// <returns>Vollständiger absoluter Pfad.</returns>
    public string GetFilePath(string relativePath)
    {
        return Path.Combine(TestRoot, relativePath);
    }
}
