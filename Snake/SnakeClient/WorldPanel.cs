﻿using System.Collections.Generic;
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

namespace SnakeGame;
public class WorldPanel : IDrawable
{
    public delegate void ObjectDrawer(object o, ICanvas canvas);
    private IImage wall;
    private IImage background;
    //private IImage explode;
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
        //explode = loadImage("Explode.png");
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

    private void ColorID(int num, ICanvas canvas)
    {
        while(num > 7)
        {
            num -= 8;
        }
        if (num == 0)
            canvas.FillColor = Colors.Red;
        else if (num == 1)
            canvas.FillColor = Colors.Orange;
        else if (num == 2)
            canvas.FillColor = Colors.Green;
        else if (num == 3)
            canvas.FillColor = Colors.Blue;
        else if (num == 4)
            canvas.FillColor = Colors.Black;
        else if (num == 5)
            canvas.FillColor = Colors.White;
        else if (num == 6)
            canvas.FillColor = Colors.Purple;
        else
            canvas.FillColor = Colors.Yellow;
    }

    private void SnakeDrawer(object o, ICanvas canvas)
    {
        Snake s = o as Snake;
        canvas.DrawString(s.name + ": " + s.score, parse(s.body[1].GetX()), parse(s.body[1].GetY()), HorizontalAlignment.Center);
        ColorID(s.ID, canvas);
        foreach (Vector2D body in s.body)
        {
            canvas.FillRectangle(parse(body.GetX()), parse(body.GetY()), 10, 10);
        }
        
    }

    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall w = o as Wall;
        canvas.DrawImage(wall, parse(w.p1.GetX()), parse(w.p1.GetY()), 50, 50);
        canvas.DrawImage(wall, parse(w.p2.GetX()), parse(w.p2.GetY()), 50, 50);
    }

    private void PowerupDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 16;
        canvas.FillColor = Colors.Orange;

        // Ellipses are drawn starting from the top-left corner.
        // So if we want the circle centered on the powerup's location, we have to offset it
        // by half its size to the left (-width/2) and up (-height/2)
        canvas.FillEllipse(-(width / 2), -(width / 2), width, width);
    }

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

        
        canvas.DrawImage(background, 0, 0, viewSize, viewSize);

        lock (world)
        {
            foreach (var p in world.Snakes.Values)
            {
                float playerX = parse(p.body[1].GetX());
                float playerY = parse(p.body[1].GetY());

                canvas.Translate(-playerX + (viewSize / 2), -playerY + (viewSize / 2));
                SnakeDrawer(p, canvas);
            }
            foreach (var p in world.Walls.Values)
            {
                WallDrawer(p, canvas);
            }
            foreach (var p in world.Powerups.Values)
            {
                DrawObjectWithTransform(canvas, p, p.loc.GetX(), p.loc.GetY(), 0, PowerupDrawer);
            }
        }
    }
}