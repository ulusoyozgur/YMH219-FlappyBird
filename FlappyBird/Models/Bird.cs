using System.Drawing;

namespace FlappyBird.Models;

public sealed class Bird
{
    public const float X = 190;
    public const float Radius = 17;
    public float Y { get; private set; } = 280;
    public float Velocity { get; private set; }
    public RectangleF Bounds => new(X - Radius, Y - Radius, Radius * 2, Radius * 2);

    public void Flap() => Velocity = -330;

    public void Update(float seconds)
    {
        Velocity += 920 * seconds;
        Y += Velocity * seconds;
        if (Y < Radius) { Y = Radius; Velocity = 0; }
    }
}
