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
using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Graphics;

namespace SnakeGame;
public class WorldPanel : IDrawable
{
    public delegate void ObjectDrawer(object o, ICanvas canvas);
    private IImage wall;
    private IImage background;
    private IImage explode;
    private int viewSize = 900;

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

    private World world;
    public WorldPanel()
    {
    }

    /// <summary>
    /// SetWorld method taken from GameLab solution.
    /// Acquires a pointer to the client's main World object.
    /// </summary>
    /// <param name="w"> World pointer for our world field </param>
    public void SetWorld(World w)
    {
        world = w;
    }

    private void InitializeDrawing()
    {
        wall = loadImage("WallSprite.png");
        background = loadImage("Background.png");
        explode = loadImage("Explode.gif");
        initializedForDrawing = true;
    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// Method that determines the snakes color based on its ID
    /// </summary>
    /// <param name="num"></param>
    /// <param name="canvas"></param>
    private void ColorID(int num, ICanvas canvas)
    {
        while(num > 7)
        {
            num -= 8;
        }
        if (num == 0)
            canvas.StrokeColor = Colors.Red;
        else if (num == 1)
            canvas.StrokeColor = Colors.Orange;
        else if (num == 2)
            canvas.StrokeColor = Colors.Green;
        else if (num == 3)
            canvas.StrokeColor = Colors.Blue;
        else if (num == 4)
            canvas.StrokeColor = Colors.Black;
        else if (num == 5)
            canvas.StrokeColor = Colors.White;
        else if (num == 6)
            canvas.StrokeColor = Colors.Purple;
        else
            canvas.StrokeColor = Colors.Yellow;
    }

    private void SnakeDrawer(object o, ICanvas canvas)
    {
        Snake s = o as Snake;
        int count = s.body.Count - 1;

        //Sets stroke and color based on snake ID
        canvas.StrokeSize = 10;
        ColorID(s.ID, canvas);
        //Draws the body connecting to the next body until it reaches the tail
        for (int i = count; i > 0; i--)
        {
            //Check for when the snake is crossing a border
            if (borderSwitch(s, canvas, i))
                continue;
            else
            {
                canvas.DrawCircle(parse(s.body[i].GetX()), parse(s.body[i].GetY()), .3f);
                canvas.DrawLine(parse(s.body[i].GetX()), parse(s.body[i].GetY()), parse(s.body[i - 1].GetX()), parse(s.body[i - 1].GetY()));
                canvas.DrawCircle(parse(s.body[i - 1].GetX()), parse(s.body[i - 1].GetY()), .3f);
            }
            
        }

        //Creates the ID and Score for the snake head
        canvas.DrawString(s.name + ": " + s.score,
            parse(s.body[count].GetX()),
            parse(s.body[count].GetY()-10),
            HorizontalAlignment.Center);
    }

    /// <summary>
    /// Method for handling when the snake teleports from 1 border to the other
    /// </summary>
    /// <param name="s"></param>
    /// <param name="canvas"></param>
    /// <param name="i"></param>
    /// <returns></returns>
    private bool borderSwitch(Snake s, ICanvas canvas, int i)
    {
        //Snake enters right border
        if (s.body[i].GetX() >= 975 && s.body[i - 1].GetX() <= -975)
        {
            return true;
        }
        //Snake enters left border
        if (s.body[i].GetX() <= -975 && s.body[i - 1].GetX() >= 975)
        {
            return true;
        }
        //Snake enters upper border
        if (s.body[i].GetY() >= 975 && s.body[i - 1].GetY() <= -975)
        {
            return true;
        }
        //Snake enters lower border
        if (s.body[i].GetY() <= -975 && s.body[i - 1].GetY() >= 975)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Drawer for Wall objects
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall w = o as Wall;
        double sizeX = w.p1.GetX();
        double sizeY = w.p1.GetY();

        //Draws walls with the same Y coord until it reaches the X other direction
        while (sizeX >= w.p2.GetX() && sizeY == w.p2.GetY())
        {
            canvas.DrawImage(wall, parse(sizeX) - 25, parse(sizeY) - 25, 50, 50);
            sizeX -= 50;
        }
        //Reset sizeX
        sizeX = w.p1.GetX();

        //Draws walls with the same Y coord until it reaches the X coord other direction
        while (sizeX <= w.p2.GetX() && sizeY == w.p2.GetY())
        {
            canvas.DrawImage(wall, parse(sizeX) - 25, parse(sizeY) - 25 , 50, 50);
            sizeX += 50;
        }
        //Reset sizeX
        sizeX = w.p1.GetX();

        //Draws walls with the same X coord until it reaches the Y coord
        while (sizeX == w.p2.GetX() && sizeY >= w.p2.GetY())
        {
            canvas.DrawImage(wall, parse(sizeX) - 25, parse(sizeY) - 25 , 50, 50);
            sizeY -= 50;
        }
        //Reset sizeY
        sizeY = w.p1.GetY();

        //Draws walls with the same X coord until it reaches the Y coord other direction
        while (sizeX == w.p2.GetX() && sizeY <= w.p2.GetY())
        {
            canvas.DrawImage(wall, parse(sizeX) - 25, parse(sizeY) - 25, 50, 50);
            sizeY += 50;
        }

    }

    /// <summary>
    /// Drawer for PowerUp Objects
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void PowerupDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 16;
        canvas.FillColor = Colors.Orange;
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);
    }

    // CHANGED TO AN EVENT HANDLER
    /*
    /// <summary>
    /// Drawer for Dead Snake Objects i.e. Death animations
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void DeadSnakeDrawer(object o, ICanvas canvas)
    {
        Snake dead = o as Snake;
        //canvas.FillColor = Colors.Red;
        //canvas.FillCircle(parse(dead.body[dead.body.Count - 1].GetX()), parse(dead.body[dead.body.Count - 1].GetY()), 20);
        //canvas.FillColor = Colors.Orange;
        //canvas.FillCircle(parse(dead.body[dead.body.Count - 1].GetX()), parse(dead.body[dead.body.Count - 1].GetY()), 15);
        //canvas.FillColor = Colors.Yellow;
        //canvas.FillCircle(parse(dead.body[dead.body.Count - 1].GetX()), parse(dead.body[dead.body.Count - 1].GetY()), 10);
        //canvas.FillColor = Colors.White;
        //canvas.FillCircle(parse(dead.body[dead.body.Count - 1].GetX()), parse(dead.body[dead.body.Count - 1].GetY()), 5);
        canvas.DrawImage(explode, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
    }
    */

    /// <summary>
    /// Given coordinates of a dead snake's head,
    /// method adds the explosion animation.
    /// </summary>
    /// <param name="coordinates"> Vector2D representation of a dead snake's head </param>
    public void KillSnake(Vector2D coordinates)
    {
        // TODO: implement this the rest of the way
        return;
    }

    /// <summary>
    /// Method for parsing doubles to floats
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    private float parse(double num)
    {
        return float.Parse(num.ToString());
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (world.ID == -1 || world.WorldSize == -1)    // do not draw if our information isn't set yet
            return;


        if (!initializedForDrawing)
            InitializeDrawing();

        canvas.ResetState();

        lock (world)
        {

            //center canvas on player snake
            float playerX = 0;
            float playerY = 0;
            if (world.Snakes.ContainsKey(world.ID))
            {
                int bodyListLen = world.Snakes[world.ID].body.Count;
                playerX = parse(world.Snakes[world.ID].body[bodyListLen - 1].GetX());
                playerY = parse(world.Snakes[world.ID].body[bodyListLen - 1].GetY());
            }

            // CHANGED TO AN EVENT HANDLER
            /*
            else if (world.DeadSnakes.ContainsKey(world.ID))
            {
                int bodyListLen = world.DeadSnakes[world.ID].body.Count;
                playerX = parse(world.DeadSnakes[world.ID].body[bodyListLen - 1].GetX());
                playerY = parse(world.DeadSnakes[world.ID].body[bodyListLen - 1].GetY());
            }
            */

            canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));

            //Draws background according to world size
            canvas.DrawImage(background, -world.WorldSize / 2, -world.WorldSize / 2, world.WorldSize, world.WorldSize);


            // draw snakes
            foreach (var p in world.Snakes.Values)
            {
                if (p.alive)
                    SnakeDrawer(p, canvas);
            }

            //draw walls
            foreach (var p in world.Walls.Values)
            {
                WallDrawer(p, canvas);
            }

            // draw powerups
            foreach (var p in world.Powerups.Values)
            {
                DrawObjectWithTransform(canvas, p, p.loc.GetX(), p.loc.GetY(), 0, PowerupDrawer);
            }

            // ChANGED TO AN EVENT HANDLER
            /*
            // draw dead snakes (i.e. explosions)
            foreach (var p in world.DeadSnakes.Values)
            {
                DeadSnakeDrawer(p, canvas);
            }
            */
        }
    }
}