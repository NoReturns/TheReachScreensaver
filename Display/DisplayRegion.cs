namespace TheReachScreensaver.Display;

internal sealed class DisplayRegion
{
    public required PixelRect Bounds { get; init; }
    public required string Name { get; init; }
    public float DpiX { get; init; } = 96f;
    public float DpiY { get; init; } = 96f;
    public bool IsPrimary { get; init; }
}
