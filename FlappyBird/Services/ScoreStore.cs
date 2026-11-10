namespace FlappyBird.Services;

public sealed class ScoreStore
{
    private readonly string path;
    public string? Error { get; private set; }
    public ScoreStore(string? path = null) => this.path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YMH219-FlappyBird", "best-score.txt");

    public int Load()
    {
        try
        {
            Error = null;
            return File.Exists(path) && int.TryParse(File.ReadAllText(path), out int score) && score >= 0 ? score : 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Error = "Rekor dosyası okunamadı; bu oturumda devam edebilirsin."; return 0; }
    }

    public void Save(int score)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path + ".tmp", score.ToString());
            File.Move(path + ".tmp", path, true);
            Error = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Error = "Rekor kaydedilemedi; bu oturumda korunuyor."; }
    }
}
