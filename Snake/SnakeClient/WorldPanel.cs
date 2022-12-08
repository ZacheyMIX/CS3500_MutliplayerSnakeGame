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
using GameModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Maui.Graphics;

namespace SnakeGame;
public class WorldPanel : IDrawable
{
    public delegate void ObjectDrawer(object o, ICanvas canvas);
    private IImage wall;
    private IImage background;
    private IImage explode1, explode2, explode3, explode4, explode5, explode6, explode7, explode8;
    private IImage explode9, explode10, explode11, explode12, explode13, explode14, explode15;
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

    private ClientWorld world;
    public WorldPanel()
    {
    }

    /// <summary>
    /// SetWorld method taken from GameLab solution.
    /// Acquires a pointer to the client's main World object.
    /// </summary>
    /// <param name="w"> World pointer for our world field </param>
    public void SetWorld(ClientWorld w)
    {
        world = w;
    }

    private void InitializeDrawing()
    {
        wall = loadImage("WallSprite.png");
        background = loadImage("Background.png");
        loadGif();
        initializedForDrawing = true;
    }

    private void loadGif()
    {
        explode1 = loadImage("Explode1.gif");
        explode2 = loadImage("Explode2.gif");
        explode3 = loadImage("Explode3.gif");
        explode4 = loadImage("Explode4.gif");
        explode5 = loadImage("Explode5.gif");
        explode6 = loadImage("Explode6.gif");
        explode7 = loadImage("Explode7.gif");
        explode8 = loadImage("Explode8.gif");
        explode9 = loadImage("Explode9.gif");
        explode10 = loadImage("Explode10.gif");
        explode11 = loadImage("Explode11.gif");
        explode12 = loadImage("Explode12.gif");
        explode13 = loadImage("Explode13.gif");
        explode14 = loadImage("Explode14.gif");
        explode15 = loadImage("Explode15.gif");
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
        if (s.body[i].GetX() >= world.WorldSize / 2.5 && s.body[i - 1].GetX() <= -world.WorldSize / 2.5)
        {
            return true;
        }
        //Snake enters left border
        if (s.body[i].GetX() <= -world.WorldSize / 2.5 && s.body[i - 1].GetX() >= world.WorldSize / 2.5)
        {
            return true;
        }
        //Snake enters upper border
        if (s.body[i].GetY() >= world.WorldSize / 2.5 && s.body[i - 1].GetY() <= -world.WorldSize / 2.5)
        {
            return true;
        }
        //Snake enters lower border
        if (s.body[i].GetY() <= -world.WorldSize/ 2.5 && s.body[i - 1].GetY() >= world.WorldSize / 2.5)
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
        float sizeX = parse(w.p1.GetX());
        float sizeY = parse(w.p1.GetY());

        //Draws walls with the same Y coord until it reaches the X other direction
        while (sizeX >= w.p2.GetX() && sizeY == w.p2.GetY())
        {
            canvas.DrawImage(wall, sizeX - 25, sizeY - 25, 50, 50);
            sizeX -= 50;
        }
        //Reset sizeX
        sizeX = parse(w.p1.GetX());

        //Draws walls with the same Y coord until it reaches the X coord other direction
        while (sizeX <= w.p2.GetX() && sizeY == w.p2.GetY())
        {
            canvas.DrawImage(wall, sizeX - 25, sizeY - 25 , 50, 50);
            sizeX += 50;
        }
        //Reset sizeX
        sizeX = parse(w.p1.GetX());

        //Draws walls with the same X coord until it reaches the Y coord
        while (sizeX == w.p2.GetX() && sizeY >= w.p2.GetY())
        {
            canvas.DrawImage(wall, parse(sizeX) - 25, parse(sizeY) - 25 , 50, 50);
            sizeY -= 50;
        }
        //Reset sizeY
        sizeY = parse(w.p1.GetY());

        //Draws walls with the same X coord until it reaches the Y coord other direction
        while (sizeX == w.p2.GetX() && sizeY <= w.p2.GetY())
        {
            canvas.DrawImage(wall, sizeX - 25, parse(sizeY) - 25, 50, 50);
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

    /// <summary>
    /// Drawer for Dead Snake Objects i.e. Death animations
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void DeadSnakeDrawer(object o, ICanvas canvas)
    {
        Snake dead = o as Snake;
        float locX = parse(dead.body[dead.body.Count - 1].GetX());
        float locY = parse(dead.body[dead.body.Count - 1].GetX());
        if (dead.explode.currentFrame == 28 )
            canvas.DrawImage(explode15, locX - 25, parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 26 || dead.explode.currentFrame == 27)
            canvas.DrawImage(explode14, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 24 || dead.explode.currentFrame == 25)
            canvas.DrawImage(explode13, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 22 || dead.explode.currentFrame == 23)
            canvas.DrawImage(explode12, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 20 || dead.explode.currentFrame == 21)
            canvas.DrawImage(explode11, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 18 || dead.explode.currentFrame == 19)
            canvas.DrawImage(explode10, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 16 || dead.explode.currentFrame == 17)
            canvas.DrawImage(explode9, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 14 || dead.explode.currentFrame == 15)
            canvas.DrawImage(explode8, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 12 || dead.explode.currentFrame == 13)
            canvas.DrawImage(explode7, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 10 || dead.explode.currentFrame == 11)
            canvas.DrawImage(explode6, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 8 || dead.explode.currentFrame == 9)
            canvas.DrawImage(explode5, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 6 || dead.explode.currentFrame == 7)
            canvas.DrawImage(explode4, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 4 || dead.explode.currentFrame == 5)
            canvas.DrawImage(explode3, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 2 || dead.explode.currentFrame == 3)
            canvas.DrawImage(explode2, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        else if (dead.explode.currentFrame == 0 || dead.explode.currentFrame == 1)
            canvas.DrawImage(explode1, parse(dead.body[dead.body.Count - 1].GetX() - 25), parse(dead.body[dead.body.Count - 1].GetY() - 25), 50, 50);
        dead.explode.runThroughFrames();
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

            else if (world.DeadSnakes.ContainsKey(world.ID))
            {
                int bodyListLen = world.DeadSnakes[world.ID].body.Count;
                playerX = parse(world.DeadSnakes[world.ID].body[bodyListLen - 1].GetX());
                playerY = parse(world.DeadSnakes[world.ID].body[bodyListLen - 1].GetY());
            }

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

            // draw dead snakes (i.e. explosions)
            foreach (var p in world.DeadSnakes.Values)
            {
                DeadSnakeDrawer(p, canvas);
            }
            
        }
    }
}