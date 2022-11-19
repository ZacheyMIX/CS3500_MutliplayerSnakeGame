using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using GC;
using ClientModel;

namespace SnakeGame;
public class WorldPanel : IDrawable
{
    private IImage wall;
    private IImage background;

    private bool initializedForDrawing = false;

#if MACCATALYST
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#else
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#endif

    private GameController control = new GameController();
    private World world;
    public WorldPanel()
    {
    }

    private void InitializeDrawing()
    {
        wall = loadImage("WallSprite.png");
        background = loadImage("Background.png");
        initializedForDrawing = true;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {

        if (!initializedForDrawing)
            InitializeDrawing();

        canvas.ResetState();
        canvas.DrawImage(background, 0, 0, 2000, 2000);
        canvas.FillColor = Colors.Red;


        world = control.modelWorld;

        lock (world)
        {
            foreach (int p in world.Snakes.Keys)
            {
                canvas.FillColor = Colors.Red;
                foreach (Vector2D body in world.Snakes[p].body)
                {
                    canvas.FillRoundedRectangle(0, 0, 25, 25, 10);
                }
            }
            foreach (int p in world.Walls.Keys)
            {
                canvas.DrawImage(wall, 0, 0, 50, 50);
            }
        }
    }
}