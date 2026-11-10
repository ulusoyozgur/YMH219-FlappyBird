using System.Diagnostics;
using System.Drawing.Drawing2D;
using FlappyBird.Models;
using FlappyBird.Services;

namespace FlappyBird;

public sealed class GameForm : Form
{
    private readonly GameEngine game = new();
    private readonly ScoreStore scores = new();
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch clock = new();
    private readonly HashSet<Keys> heldKeys = [];
    private readonly GameCanvas canvas;
    private readonly Label status = new();
    private readonly Button pause = new();
    private int best;

    public GameForm()
    {
        Text = "Flappy Bird • YMH219";
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 740);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(17, 34, 49);
        ForeColor = Color.White;
        KeyPreview = true;
        Font = new Font("Segoe UI", 10);
        best = scores.Load();
        var title = new Label { Text = "FLAPPY BIRD", Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = true, Location = new Point(26, 12) };
        var subtitle = new Label { Text = "Küçük kanatlar. Büyük bir yolculuk.", AutoSize = true,
            ForeColor = Color.FromArgb(168, 195, 209), Location = new Point(30, 62) };
        var badge = new Label { Text = "YMH219  /  C# + WINDOWS FORMS", AutoSize = true,
            ForeColor = Color.FromArgb(102, 218, 194), Location = new Point(594, 38) };
        canvas = new GameCanvas(game, () => best) { Location = new Point(0, 100), Size = new Size(900, 560), TabStop = false };
        var restart = MakeButton("Yeni oyun  ·  R", 540, 680, 160);
        restart.Click += (_, _) => Restart();
        pause = MakeButton("Duraklat  ·  P", 714, 680, 160);
        pause.Click += (_, _) => { game.TogglePause(); RefreshGame(); };
        status.SetBounds(28, 677, 495, 55);
        status.ForeColor = Color.FromArgb(192, 210, 222);
        Controls.AddRange([title, subtitle, badge, canvas, restart, pause, status]);
        KeyDown += HandleKeyDown;
        KeyUp += (_, e) => heldKeys.Remove(e.KeyCode);
        Deactivate += (_, _) =>
        {
            heldKeys.Clear();
            if (game.State == GameState.Playing) game.TogglePause();
            RefreshGame();
        };
        game.GameEnded += score =>
        {
            if (score > best) { best = score; scores.Save(best); }
        };
        timer.Tick += (_, _) =>
        {
            float elapsed = (float)clock.Elapsed.TotalSeconds;
            clock.Restart();
            game.Update(elapsed);
            RefreshGame();
        };
        Shown += (_, _) => { clock.Start(); timer.Start(); };
        RefreshGame();
    }

    private Button MakeButton(string text, int x, int y, int width)
    {
        var button = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 38),
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(36, 65, 80), ForeColor = Color.White };
        button.FlatAppearance.BorderColor = Color.FromArgb(78, 116, 132);
        return button;
    }

    private void Restart()
    {
        game.Reset();
        clock.Restart();
        RefreshGame();
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is not (Keys.Space or Keys.Up or Keys.W or Keys.R or Keys.P or Keys.Escape)) return;
        e.Handled = true;
        e.SuppressKeyPress = true;
        if (!heldKeys.Add(e.KeyCode)) return; // Basılı tutma otomatik kanat çırpmasın.
        if (e.KeyCode == Keys.R) Restart();
        else if (e.KeyCode is Keys.P or Keys.Escape) game.TogglePause();
        else game.Flap();
        RefreshGame();
    }

    private void RefreshGame()
    {
        pause.Enabled = game.State is GameState.Playing or GameState.Paused;
        pause.Text = game.State == GameState.Paused ? "Devam et  ·  P" : "Duraklat  ·  P";
        status.Text = scores.Error ?? "BOŞLUK / ↑ / W   Kanat çırp\nP   Duraklat     •     R   Yeni oyun";
        canvas.Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class GameCanvas(GameEngine game, Func<int> best) : Control
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.ScaleTransform(Width / 900f, Height / 560f);
        using var sky = new LinearGradientBrush(new Rectangle(0, 0, 900, 500),
            Color.FromArgb(173, 224, 230), Color.FromArgb(232, 246, 224), 90f);
        g.FillRectangle(sky, 0, 0, 900, 500);
        FillEllipse(g, Color.FromArgb(255, 245, 192), 724, 70, 90, 90);
        Cloud(g, 75, 115); Cloud(g, 365, 75); Cloud(g, 650, 205);
        FillEllipse(g, Color.FromArgb(172, 211, 181), -110, 378, 560, 260);
        FillEllipse(g, Color.FromArgb(165, 205, 179), 320, 400, 700, 240);
        foreach (var pipe in game.Pipes)
        {
            DrawPipe(g, pipe.TopBounds, true);
            DrawPipe(g, pipe.BottomBounds, false);
        }
        FillRect(g, Color.FromArgb(65, 117, 91), 0, 500, 900, 7);
        FillRect(g, Color.FromArgb(204, 221, 165), 0, 507, 900, 53);
        for (int x = 0; x < 900; x += 28) FillRect(g, Color.FromArgb(180, 201, 141), x, 511, 15, 5);
        DrawBird(g);
        Card(g, 24, 22, 142, 60, Color.FromArgb(235, 255, 255, 255));
        DrawText(g, "SKOR", 36, 29, 10, Color.FromArgb(63, 103, 116));
        DrawText(g, game.Score.ToString("00"), 92, 28, 24, Color.FromArgb(23, 54, 68), true);
        Card(g, 708, 22, 168, 60, Color.FromArgb(235, 255, 255, 255));
        DrawText(g, "EN İYİ", 722, 29, 10, Color.FromArgb(63, 103, 116));
        DrawText(g, Math.Max(best(), game.Score).ToString("00"), 803, 28, 24, Color.FromArgb(23, 54, 68), true);
        DrawText(g, $"HIZ  {game.Speed:0} px/sn", 26, 531, 9, Color.FromArgb(53, 86, 67));
        DrawText(g, game.State switch { GameState.Ready => "HAZIR", GameState.Playing => "UÇUŞTA", GameState.Paused => "DURAKLATILDI", _ => "OYUN BİTTİ" },
            732, 531, 9, Color.FromArgb(53, 86, 67), true);
        if (game.State != GameState.Playing) DrawOverlay(g);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    private void DrawOverlay(Graphics g)
    {
        Card(g, 267, 153, 366, 240, Color.FromArgb(245, 17, 34, 49));
        var title = game.State switch { GameState.Ready => "Uçmaya hazır mısın?", GameState.Paused => "Kısa bir mola", _ => "GAME OVER" };
        CenterText(g, title, 185, 25, Color.White, true);
        CenterText(g, game.State switch
        {
            GameState.Ready => "Boruların arasından geç, puanları topla.",
            GameState.Paused => "Yolculuğun seni bekliyor.",
            _ => $"Skor: {game.Score}     •     En iyi: {best()}"
        }, 241, 12, Color.FromArgb(187, 211, 218));
        Card(g, 304, 289, 292, 48, Color.FromArgb(103, 223, 190));
        CenterText(g, game.State switch { GameState.Ready => "BOŞLUK ile başla", GameState.Paused => "P ile devam et", _ => "R ile yeniden başlat" },
            300, 13, Color.FromArgb(17, 49, 51), true);
        CenterText(g, "Her basışta bir kanat çırpışı", 354, 10, Color.FromArgb(152, 184, 197));
    }

    private void DrawBird(Graphics g)
    {
        float x = Bird.X, y = game.Bird.Y;
        FillEllipse(g, Color.FromArgb(111, 78, 39), x - 19, y - 19, 38, 38);
        FillEllipse(g, Color.FromArgb(255, 204, 74), x - 17, y - 17, 34, 34);
        FillEllipse(g, Color.FromArgb(244, 164, 56), x - 22, y - 1, 24, 15);
        FillEllipse(g, Color.White, x + 3, y - 13, 15, 17);
        FillEllipse(g, Color.FromArgb(24, 43, 52), x + 11, y - 9, 5, 7);
        using var beak = new SolidBrush(Color.FromArgb(235, 109, 56));
        g.FillPolygon(beak, [new PointF(x + 14, y + 1), new PointF(x + 29, y + 6), new PointF(x + 14, y + 10)]);
    }

    private static void DrawPipe(Graphics g, RectangleF rect, bool top)
    {
        using var gradient = new LinearGradientBrush(rect, Color.FromArgb(55, 143, 124), Color.FromArgb(30, 99, 91), 0f);
        g.FillRectangle(gradient, rect);
        FillRect(g, Color.FromArgb(105, 193, 157), rect.X + 6, rect.Y, 9, rect.Height);
        float rimY = top ? rect.Bottom - 22 : rect.Y;
        // Kenarlık da çarpışma dikdörtgeninin içinde kalır.
        FillRect(g, Color.FromArgb(25, 81, 77), rect.X, rimY, rect.Width, 22);
        FillRect(g, Color.FromArgb(72, 165, 138), rect.X + 3, rimY + 3, rect.Width - 6, 16);
    }

    private static void Cloud(Graphics g, float x, float y)
    {
        var color = Color.FromArgb(155, 255, 255, 255);
        FillEllipse(g, color, x, y, 98, 24);
        FillEllipse(g, color, x + 18, y - 20, 44, 42);
        FillEllipse(g, color, x + 47, y - 10, 38, 32);
    }
    private static void FillRect(Graphics g, Color color, float x, float y, float w, float h)
    { using var brush = new SolidBrush(color); g.FillRectangle(brush, x, y, w, h); }
    private static void FillEllipse(Graphics g, Color color, float x, float y, float w, float h)
    { using var brush = new SolidBrush(color); g.FillEllipse(brush, x, y, w, h); }
    private static void Card(Graphics g, int x, int y, int w, int h, Color color)
    {
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(new Rectangle(x, y, w, h), new Size(16, 16));
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }
    private static void DrawText(Graphics g, string text, float x, float y, float size, Color color, bool bold = false)
    {
        using var font = new Font("Segoe UI", size * 96f / 72f, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, x, y);
    }
    private static void CenterText(Graphics g, string text, float y, float size, Color color, bool bold = false)
    {
        using var font = new Font("Segoe UI", size * 96f / 72f, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = StringAlignment.Center };
        g.DrawString(text, font, brush, new RectangleF(270, y, 360, 40), format);
    }
}



