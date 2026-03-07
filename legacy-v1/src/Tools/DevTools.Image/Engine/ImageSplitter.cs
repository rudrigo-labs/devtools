using DevTools.Image.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DevTools.Image.Engine;

internal sealed class ImageSplitter
{
    public byte AlphaThreshold { get; set; } = 10;

    public List<ImageRegion> FindConnectedComponents(Image<Rgba32> image, CancellationToken ct)
    {
        var visited = new bool[image.Width, image.Height];
        var components = new List<ImageRegion>();

        for (int y = 0; y < image.Height; y++)
        {
            ct.ThrowIfCancellationRequested();

            for (int x = 0; x < image.Width; x++)
            {
                if (visited[x, y]) continue;

                var pixel = image[x, y];
                if (pixel.A <= AlphaThreshold)
                {
                    visited[x, y] = true;
                    continue;
                }

                var rect = FloodFill(image, visited, x, y, ct);
                components.Add(rect);
            }
        }

        return components;
    }

    private ImageRegion FloodFill(Image<Rgba32> image, bool[,] visited, int startX, int startY, CancellationToken ct)
    {
        var minX = startX;
        var maxX = startX;
        var minY = startY;
        var maxY = startY;

        var queue = new Queue<PixelPoint>();
        queue.Enqueue(new PixelPoint(startX, startY));
        visited[startX, startY] = true;

        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var p = queue.Dequeue();

            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;

            var neighbors = new[]
            {
                new PixelPoint(p.X + 1, p.Y),
                new PixelPoint(p.X - 1, p.Y),
                new PixelPoint(p.X, p.Y + 1),
                new PixelPoint(p.X, p.Y - 1),
                new PixelPoint(p.X + 1, p.Y + 1),
                new PixelPoint(p.X + 1, p.Y - 1),
                new PixelPoint(p.X - 1, p.Y + 1),
                new PixelPoint(p.X - 1, p.Y - 1)
            };

            foreach (var n in neighbors)
            {
                if (n.X >= 0 && n.X < image.Width && n.Y >= 0 && n.Y < image.Height)
                {
                    if (!visited[n.X, n.Y])
                    {
                        var pixel = image[n.X, n.Y];
                        if (pixel.A > AlphaThreshold)
                        {
                            visited[n.X, n.Y] = true;
                            queue.Enqueue(n);
                        }
                        else
                        {
                            visited[n.X, n.Y] = true;
                        }
                    }
                }
            }
        }

        return new ImageRegion(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private readonly record struct PixelPoint(int X, int Y);
}
