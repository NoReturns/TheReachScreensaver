namespace TheReachScreensaver.Display;

internal readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public float Aspect => Height == 0 ? 1f : Width / (float)Height;
}
