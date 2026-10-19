using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace DNS_changer.UI
{
    internal enum Glyph
    {
        None,
        Gear,
        Refresh,
        Reset,
        Plus,
        Pencil,
        Trash,
        ChevronUp,
        ChevronDown,
        Bolt,
        Check,
        Globe,
        Shield,
        Offline,
    }

    /// <summary>Small vector icons drawn with GDI+, so no icon font or image assets are needed.</summary>
    internal static class Glyphs
    {
        public static void Draw(Graphics g, Glyph glyph, RectangleF box, Color color)
        {
            if (glyph == Glyph.None)
                return;

            var state = g.Save();
            Gfx.Smooth(g);
            float w = box.Width, h = box.Height;
            float stroke = Math.Max(1.4f, w * 0.095f);
            Func<float, float, PointF> p = (x, y) => new PointF(box.X + x * w, box.Y + y * h);
            var center = p(.5f, .5f);

            using (var pen = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            using (var brush = new SolidBrush(color))
            {
                switch (glyph)
                {
                    case Glyph.Gear:
                    {
                        float r = w * .26f, ri = w * .09f;
                        g.DrawEllipse(pen, center.X - r, center.Y - r, r * 2, r * 2);
                        g.DrawEllipse(pen, center.X - ri, center.Y - ri, ri * 2, ri * 2);
                        using (var tooth = new Pen(color, stroke * 1.9f))
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                double a = i * Math.PI / 4;
                                float cx = (float)Math.Cos(a), cy = (float)Math.Sin(a);
                                g.DrawLine(tooth, center.X + cx * r, center.Y + cy * r,
                                    center.X + cx * w * .45f, center.Y + cy * w * .45f);
                            }
                        }
                        break;
                    }
                    case Glyph.Refresh:
                        DrawRefresh(g, pen, brush, center, w);
                        break;
                    case Glyph.Reset:
                    {
                        var mirror = g.Save();
                        g.TranslateTransform(center.X * 2, 0);
                        g.ScaleTransform(-1, 1);
                        DrawRefresh(g, pen, brush, center, w);
                        g.Restore(mirror);
                        break;
                    }
                    case Glyph.Plus:
                        g.DrawLine(pen, p(.5f, .2f), p(.5f, .8f));
                        g.DrawLine(pen, p(.2f, .5f), p(.8f, .5f));
                        break;
                    case Glyph.Pencil:
                        g.DrawLines(pen, new[] { p(.2f, .8f), p(.24f, .62f), p(.64f, .22f), p(.78f, .36f), p(.38f, .76f), p(.2f, .8f) });
                        g.DrawLine(pen, p(.55f, .31f), p(.69f, .45f));
                        break;
                    case Glyph.Trash:
                        g.DrawLine(pen, p(.18f, .27f), p(.82f, .27f));
                        g.DrawLines(pen, new[] { p(.39f, .27f), p(.41f, .16f), p(.59f, .16f), p(.61f, .27f) });
                        g.DrawLines(pen, new[] { p(.27f, .27f), p(.31f, .84f), p(.69f, .84f), p(.73f, .27f) });
                        g.DrawLine(pen, p(.44f, .42f), p(.44f, .69f));
                        g.DrawLine(pen, p(.56f, .42f), p(.56f, .69f));
                        break;
                    case Glyph.ChevronUp:
                        g.DrawLines(pen, new[] { p(.24f, .63f), p(.5f, .37f), p(.76f, .63f) });
                        break;
                    case Glyph.ChevronDown:
                        g.DrawLines(pen, new[] { p(.24f, .37f), p(.5f, .63f), p(.76f, .37f) });
                        break;
                    case Glyph.Bolt:
                        g.FillPolygon(brush, new[] { p(.58f, .06f), p(.2f, .56f), p(.47f, .56f), p(.4f, .94f), p(.8f, .42f), p(.53f, .42f) });
                        break;
                    case Glyph.Check:
                        g.DrawLines(pen, new[] { p(.2f, .52f), p(.41f, .72f), p(.8f, .3f) });
                        break;
                    case Glyph.Globe:
                    {
                        float r = w * .38f;
                        g.DrawEllipse(pen, center.X - r, center.Y - r, r * 2, r * 2);
                        g.DrawEllipse(pen, center.X - r * .45f, center.Y - r, r * .9f, r * 2);
                        g.DrawLine(pen, center.X - r, center.Y, center.X + r, center.Y);
                        break;
                    }
                    case Glyph.Shield:
                        g.DrawLines(pen, new[] { p(.5f, .1f), p(.82f, .22f), p(.8f, .52f), p(.5f, .9f), p(.2f, .52f), p(.18f, .22f), p(.5f, .1f) });
                        g.DrawLines(pen, new[] { p(.36f, .5f), p(.47f, .61f), p(.66f, .4f) });
                        break;
                    case Glyph.Offline:
                        g.DrawLine(pen, p(.25f, .25f), p(.75f, .75f));
                        g.DrawLine(pen, p(.75f, .25f), p(.25f, .75f));
                        break;
                }
            }
            g.Restore(state);
        }

        static void DrawRefresh(Graphics g, Pen pen, Brush brush, PointF c, float w)
        {
            float r = w * .3f;
            const float start = -20f, sweep = 280f;
            g.DrawArc(pen, c.X - r, c.Y - r, r * 2, r * 2, start, sweep);

            double end = (start + sweep) * Math.PI / 180;
            var tip = new PointF(c.X + r * (float)Math.Cos(end), c.Y + r * (float)Math.Sin(end));
            // Clockwise tangent at the arc end.
            float dx = -(float)Math.Sin(end), dy = (float)Math.Cos(end);
            float size = w * .26f;
            var front = new PointF(tip.X + dx * size * .55f, tip.Y + dy * size * .55f);
            var back = new PointF(tip.X - dx * size * .45f, tip.Y - dy * size * .45f);
            float px = -dy, py = dx;
            g.FillPolygon(brush, new[]
            {
                front,
                new PointF(back.X + px * size * .6f, back.Y + py * size * .6f),
                new PointF(back.X - px * size * .6f, back.Y - py * size * .6f),
            });
        }
    }
}
