using FlappyBird.Models;
using FlappyBird.Services;

int count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    count++;
}

var game = new GameEngine(219);
Check(game.State == GameState.Ready && game.Pipes.Count >= 2, "Initial state and obstacle count");
float initialY = game.Bird.Y;
game.Update(1);
Check(game.Bird.Y == initialY, "Ready state does not advance");
game.Flap(); game.Update(0.05f);
Check(game.Bird.Y < initialY, "Flap moves bird up");
game.TogglePause();
float pausedY = game.Bird.Y, pausedX = game.Pipes[0].X;
game.Update(0.1f); game.Flap();
Check(game.Bird.Y == pausedY && game.Pipes[0].X == pausedX, "Pause freezes physics");
game.TogglePause();
int ended = 0;
game.GameEnded += _ => ended++;
for (int i = 0; i < 400; i++) game.Update(1f / 60);
Check(game.State == GameState.GameOver && ended == 1, "Ground collision ends exactly once");
float stoppedX = game.Pipes[0].X;
game.Update(0.1f);
Check(game.Pipes[0].X == stoppedX, "Game over freezes obstacles");
game.Reset();
Check(game.Score == 0 && game.State == GameState.Ready && game.Bird.Y == initialY, "Restart resets session");
game.Pipes[0].Recycle(Bird.X - 10, 40);
game.Flap(); game.Update(1f / 120);
Check(game.State == GameState.GameOver, "Bottom pipe collision");
game.Reset();
game.Pipes[0].Recycle(Bird.X - 10, 310);
game.Flap(); game.Update(1f / 120);
Check(game.State == GameState.GameOver, "Top pipe collision");
game.Reset();
game.Pipes[0].Recycle(Bird.X - Bird.Radius - PipePair.Width - 1, 160);
game.Flap(); game.Update(1f / 120);
Check(game.Score == 1 && game.Speed == 175, "Passing scores and increases speed");
game.Update(1f / 120);
Check(game.Score == 1, "Each obstacle scores once");
game.Pipes[0].Recycle(-100, 160);
float lastX = game.Pipes.Max(p => p.X);
game.Update(1f / 120);
Check(game.Pipes[0].X > lastX && !game.Pipes[0].Passed, "Offscreen pipe recycles after last pipe");
var bird = new Bird();
for (int i = 0; i < 200; i++) { bird.Flap(); bird.Update(1f / 120); }
Check(bird.Y >= Bird.Radius, "Ceiling clamps position");
game.Reset(); game.Flap();
float beforeInvalid = game.Bird.Y;
game.Update(float.NaN); game.Update(-1);
Check(game.Bird.Y == beforeInvalid, "Invalid elapsed times ignored");

string folder = Path.Combine(Path.GetTempPath(), "FlappyBirdChecks-" + Guid.NewGuid());
try
{
    string path = Path.Combine(folder, "best.txt");
    var store = new ScoreStore(path);
    Check(store.Load() == 0, "Missing score defaults to zero");
    store.Save(12);
    Check(new ScoreStore(path).Load() == 12 && store.Error == null, "Score persists across instances");
    File.WriteAllText(path, "corrupt");
    Check(store.Load() == 0, "Corrupt score handled");
    File.WriteAllText(path, "-5");
    Check(store.Load() == 0, "Negative score rejected");
    var blocked = new ScoreStore(Path.Combine(path, "invalid.txt"));
    blocked.Save(4);
    Check(blocked.Error != null, "Write failure is reported without crashing");
}
finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
Console.WriteLine($"All {count} checks passed.");
