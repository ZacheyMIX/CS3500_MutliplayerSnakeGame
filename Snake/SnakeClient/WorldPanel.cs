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
  private IImage loadImage( string name )
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream( assembly.GetManifestResourceStream( $"{path}.{name}" ) );
    }
#endif

    World world;
    public WorldPanel()
    {
    }

    private void InitializeDrawing()
    {
        wall = loadImage( "WallSprite.png" );
        background = loadImage( "Background.png" );
        initializedForDrawing = true;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // TODO: write code to retrieve this frame's world from our active GameController instance's GetWorld method.
        // Assign this to WorldPanel's world object and then use that to draw our world.
        // Also note that we need to check that world is initialized correctly with both the worldsize and our client's ID.

        
        if ( !initializedForDrawing )
            InitializeDrawing();

        canvas.ResetState();
        canvas.DrawImage(background, 0, 0, 2000, 2000);
        canvas.FillColor = Colors.Red;


        // All references to control.modelWorld should be changed to WorldPanel's world data member.
        lock (control.modelWorld)
        {
            foreach (int p in control.modelWorld.Snakes.Keys)
            {
                canvas.FillColor = Colors.Red;
                foreach (Vector2D body in control.modelWorld.Snakes[p].body)
                {
                    canvas.FillRoundedRectangle(0, 0, 25, 25, 10);
                }
            }
            foreach(int p in control.modelWorld.Walls.Keys)
            {
                canvas.DrawImage(wall, 0, 0, 50, 50);
            }
        } 
    }
}
