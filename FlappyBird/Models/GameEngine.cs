namespace FlappyBird.Models;

public enum GameState { Ready, Playing, Paused, GameOver }

public sealed class GameEngine
{
    public const int Width = 900;
    public const int Height = 560;
    public const int GroundY = 500;
    public const int PipeSpacing = 315;
    private readonly Random random;
    private readonly List<PipePair> pipes = [];
    public Bird Bird { get; private set; } = new();
    public IReadOnlyList<PipePair> Pipes => pipes.AsReadOnly();
    public GameState State { get; private set; }
    public int Score { get; private set; }
    public float Speed => Math.Min(170 + Score * 5, 255);
    public event Action<int>? GameEnded;

    public GameEngine(int? seed = null)
    {
        random = seed.HasValue ? new Random(seed.Value) : new Random();
        Reset();
    }

    public void Reset()
    {
        Bird = new Bird();
        Score = 0;
        State = GameState.Ready;
        pipes.Clear();
        for (int i = 0; i < 3; i++) pipes.Add(new PipePair(580 + i * PipeSpacing, NextGap()));
    }

    private float NextGap() => random.Next(95, 236);

    public void Flap()
    {
        if (State == GameState.Ready) State = GameState.Playing;
        if (State == GameState.Playing) Bird.Flap();
    }

    public void TogglePause()
    {
        if (State == GameState.Playing) State = GameState.Paused;
        else if (State == GameState.Paused) State = GameState.Playing;
    }

    public void Update(float seconds)
    {
        if (State != GameState.Playing || !float.IsFinite(seconds) || seconds <= 0) return;
        // Küçük fizik adımları hızlı hareketlerde engellerin içinden geçmeyi önler.
        float remaining = Math.Min(seconds, 0.1f);
        while (remaining > 0 && State == GameState.Playing)
        {
            float step = Math.Min(remaining, 1f / 120);
            Step(step);
            remaining -= step;
        }
    }

    private void Step(float seconds)
    {
        Bird.Update(seconds);
        foreach (var pipe in pipes) pipe.Move(Speed * seconds);
        if (Bird.Bounds.Bottom >= GroundY || pipes.Any(p =>
            Bird.Bounds.IntersectsWith(p.TopBounds) || Bird.Bounds.IntersectsWith(p.BottomBounds)))
        {
            State = GameState.GameOver;
            GameEnded?.Invoke(Score);
            return;
        }
        foreach (var pipe in pipes)
        {
            if (!pipe.Passed && pipe.X + PipePair.Width < Bird.Bounds.Left)
            {
                pipe.MarkPassed();
                Score++;
            }
            if (pipe.X + PipePair.Width < 0)
                pipe.Recycle(pipes.Max(p => p.X) + PipeSpacing, NextGap());
        }
    }
}
