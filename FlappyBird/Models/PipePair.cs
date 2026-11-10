using System.Drawing;

namespace FlappyBird.Models;

public sealed class PipePair(float x, float gapTop)
{
    public const float Width = 72;
    public const float Gap = 180;
    public float X { get; private set; } = x;
    public float GapTop { get; private set; } = gapTop;
    public bool Passed { get; private set; }
    public RectangleF TopBounds => new(X, 0, Width, GapTop);
    public RectangleF BottomBounds => new(X, GapTop + Gap, Width, GameEngine.GroundY - GapTop - Gap);

    public void Move(float distance) => X -= distance;
    public void MarkPassed() => Passed = true;
    public void Recycle(float x, float gapTop)
    {
        X = x;
        GapTop = gapTop;
        Passed = false;
    }
}
