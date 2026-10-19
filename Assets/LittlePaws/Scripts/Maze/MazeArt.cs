using UnityEngine;

namespace LittlePaws.Maze
{
    // Small, code-native artwork for the maze UI. The caller owns both sprite
    // and texture and should Destroy them when the maze screen is disposed.
    public static class MazeArt
    {
        private static readonly Color Ink = Hex(0x4B3541);
        private static Color Hex(uint rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
        }

        private sealed class Canvas
        {
            public readonly int Size;
            private readonly Color[] pixels;
            public Canvas(int size) { Size = size; pixels = new Color[size * size]; }

            private void Blend(int x, int y, Color c, float coverage)
            {
                if (x < 0 || y < 0 || x >= Size || y >= Size) return;
                int i = x + y * Size;
                float a = Mathf.Clamp01(c.a * coverage);
                Color old = pixels[i];
                float outA = a + old.a * (1f - a);
                if (outA < 0.0001f) return;
                pixels[i] = new Color((c.r * a + old.r * old.a * (1f - a)) / outA,
                    (c.g * a + old.g * old.a * (1f - a)) / outA,
                    (c.b * a + old.b * old.a * (1f - a)) / outA, outA);
            }

            public void Fill(Color c)
            {
                for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            }

            // Drawing coordinates are normalized to a 192 x 192 artboard.
            public void Ellipse(float cx, float cy, float rx, float ry, Color c)
            {
                float scale = Size / 192f;
                cx *= scale; cy *= scale; rx *= scale; ry *= scale;
                for (int y = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1)); y < Mathf.Min(Size, Mathf.CeilToInt(cy + ry + 1)); y++)
                for (int x = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1)); x < Mathf.Min(Size, Mathf.CeilToInt(cx + rx + 1)); x++)
                {
                    float dx = (x + .5f - cx) / rx, dy = (y + .5f - cy) / ry;
                    float distance = (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
                    Blend(x, y, c, Mathf.Clamp01(.5f - distance));
                }
            }

            public void Line(float ax, float ay, float bx, float by, float width, Color c)
            {
                Vector2 a = new Vector2(ax, ay) * (Size / 192f);
                Vector2 b = new Vector2(bx, by) * (Size / 192f);
                float radius = width * Size / 384f;
                Vector2 d = b - a;
                for (int y = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1)); y < Mathf.Min(Size, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1)); y++)
                for (int x = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1)); x < Mathf.Min(Size, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1)); x++)
                {
                    Vector2 p = new Vector2(x + .5f, y + .5f);
                    float t = d.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude) : 0;
                    Blend(x, y, c, Mathf.Clamp01(radius + .5f - Vector2.Distance(p, a + d * t)));
                }
            }

            public void Polygon(Vector2[] points, Color c)
            {
                float scale = Size / 192f;
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        Vector2 p = new Vector2(x + .25f + sx * .5f, y + .25f + sy * .5f) / scale;
                        bool inside = false;
                        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                        {
                            Vector2 a = points[i], b = points[j];
                            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                                inside = !inside;
                        }
                        if (inside) hits++;
                    }
                    if (hits > 0) Blend(x, y, c, hits * .25f);
                }
            }

            public void RoundRect(float left, float bottom, float right, float top, float radius, Color c)
            {
                float scale = Size / 192f;
                Vector2 center = new Vector2((left + right) / 2, (bottom + top) / 2) * scale;
                Vector2 half = new Vector2((right - left) / 2, (top - bottom) / 2) * scale;
                radius *= scale;
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    Vector2 q = new Vector2(Mathf.Abs(x + .5f - center.x), Mathf.Abs(y + .5f - center.y)) - half + Vector2.one * radius;
                    float distance = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - radius;
                    Blend(x, y, c, Mathf.Clamp01(.5f - distance));
                }
            }

            public Sprite Finish(string name, Vector4 border = default(Vector4))
            {
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                texture.name = name + " Texture";
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
                sprite.name = name;
                return sprite;
            }
        }

        public static Sprite Pet(bool puppy)
        {
            var p = new Canvas(192);
            Color fur = puppy ? Hex(0xDCA86E) : Hex(0xF5CD91);
            Color shade = puppy ? Hex(0xB77D50) : Hex(0xDEA565);
            Color cream = Hex(0xFFF1D6);
            Color ear = Hex(0xE7A5A0);
            p.Ellipse(96, 19, 57, 10, Hex(0x314849, .17f));
            // The little body and feet remain visible below the oversized head.
            p.Ellipse(96, 55, 35, 35, shade);
            p.Ellipse(96, 57, 29, 30, fur);
            p.Ellipse(96, 52, 19, 22, cream);
            p.Ellipse(71, 27, 16, 11, fur); p.Ellipse(121, 27, 16, 11, fur);
            p.Line(67, 24, 67, 28, 2, shade); p.Line(74, 24, 74, 28, 2, shade);
            p.Line(117, 24, 117, 28, 2, shade); p.Line(124, 24, 124, 28, 2, shade);
            if (puppy)
            {
                p.Ellipse(41, 113, 22, 44, shade);
                p.Ellipse(151, 113, 22, 44, shade);
                p.Ellipse(38, 112, 14, 32, Hex(0xC88D60));
                p.Ellipse(154, 112, 14, 32, Hex(0xC88D60));
            }
            else
            {
                p.Polygon(new[] {new Vector2(42, 120), new Vector2(43, 179), new Vector2(85, 147)}, shade);
                p.Polygon(new[] {new Vector2(107, 147), new Vector2(149, 179), new Vector2(150, 120)}, shade);
                p.Polygon(new[] {new Vector2(50, 132), new Vector2(51, 164), new Vector2(75, 144)}, ear);
                p.Polygon(new[] {new Vector2(117, 144), new Vector2(141, 164), new Vector2(142, 132)}, ear);
            }
            p.Ellipse(96, 108, 59, 54, shade);
            p.Ellipse(96, 111, 56, 51, fur);
            if (!puppy)
            {
                p.Line(96, 156, 96, 144, 6, shade);
                p.Line(79, 153, 83, 144, 5, shade);
                p.Line(113, 153, 109, 144, 5, shade);
                p.Line(44, 110, 53, 107, 5, shade);
                p.Line(148, 110, 139, 107, 5, shade);
            }
            else p.Ellipse(125, 126, 17, 23, Hex(0xC28D59));
            p.Ellipse(96, 86, 29, 21, cream);
            p.Ellipse(59, 97, 11, 6, Hex(0xEAA69C, .63f));
            p.Ellipse(133, 97, 11, 6, Hex(0xEAA69C, .63f));
            p.Ellipse(75, 115, 8, 11, Ink); p.Ellipse(117, 115, 8, 11, Ink);
            p.Ellipse(77, 119, 2.6f, 3, Color.white); p.Ellipse(119, 119, 2.6f, 3, Color.white);
            p.Ellipse(73, 111, 1.3f, 1.4f, Hex(0xA58E72)); p.Ellipse(115, 111, 1.3f, 1.4f, Hex(0xA58E72));
            p.Ellipse(96, 95, puppy ? 8 : 6, 4.5f, puppy ? Ink : Hex(0xBA7780));
            p.Line(96, 92, 96, 85, 2.8f, Ink);
            p.Line(96, 85, 90, 82, 2.5f, Ink); p.Line(96, 85, 102, 82, 2.5f, Ink);
            if (!puppy)
            {
                p.Line(63, 90, 37, 94, 1.8f, Ink); p.Line(63, 84, 38, 81, 1.8f, Ink);
                p.Line(129, 90, 155, 94, 1.8f, Ink); p.Line(129, 84, 154, 81, 1.8f, Ink);
            }
            p.RoundRect(75, 62, 117, 68, 3, Hex(0x619B8A));
            p.Ellipse(96, 62, 5, 6, Hex(0xF6CC6B));
            return p.Finish(puppy ? "Maze Puppy" : "Maze Kitten");
        }

        public static Sprite Rounded()
        {
            var p = new Canvas(32);
            p.RoundRect(0, 0, 192, 192, 56, Color.white);
            return p.Finish("Maze Rounded Panel", new Vector4(10, 10, 10, 10));
        }

        public static Sprite Star()
        {
            var p = new Canvas(64);
            p.Ellipse(96, 33, 57, 12, Hex(0xA97E2E, .2f));
            var vertices = new Vector2[10];
            for (int i = 0; i < vertices.Length; i++)
            {
                float angle = Mathf.PI / 2 + i * Mathf.PI / 5;
                float radius = i % 2 == 0 ? 78 : 36;
                vertices[i] = new Vector2(96 + Mathf.Cos(angle) * radius, 101 + Mathf.Sin(angle) * radius);
            }
            p.Polygon(vertices, Hex(0xDCA547));
            for (int i = 0; i < vertices.Length; i++) vertices[i] = (vertices[i] - new Vector2(96, 101)) * .85f + new Vector2(96, 105);
            p.Polygon(vertices, Hex(0xFFE099));
            p.Line(79, 139, 70, 119, 6, Hex(0xFFF5D5));
            p.Ellipse(82, 101, 4, 6, Ink); p.Ellipse(110, 101, 4, 6, Ink);
            p.Line(90, 86, 96, 83, 3, Ink); p.Line(96, 83, 102, 86, 3, Ink);
            return p.Finish("Maze Goal Star");
        }

        public static Sprite Mud()
        {
            var p = new Canvas(64);
            Color edge = Hex(0x725343), fill = Hex(0x94705A);
            p.Ellipse(96, 84, 81, 48, edge); p.Ellipse(60, 113, 43, 34, edge); p.Ellipse(131, 109, 40, 39, edge);
            p.Ellipse(96, 88, 76, 43, fill); p.Ellipse(60, 115, 37, 28, fill); p.Ellipse(131, 111, 34, 33, fill);
            p.Line(45, 114, 66, 122, 5, Hex(0xB79374)); p.Line(66, 122, 84, 121, 5, Hex(0xB79374));
            p.Line(110, 79, 133, 81, 4, Hex(0xB79374));
            p.Ellipse(121, 115, 8, 4, Hex(0x735342)); p.Ellipse(119, 117, 5, 2, Hex(0xC3A082));
            p.Ellipse(57, 80, 6, 3, Hex(0x795945)); p.Ellipse(147, 55, 10, 5, edge);
            return p.Finish("Maze Mud Puddle");
        }

        public static Sprite MapArrow()
        {
            var p=new Canvas(32);
            p.Polygon(new[]{new Vector2(96,182),new Vector2(20,18),new Vector2(96,54),new Vector2(172,18)},Color.white);
            return p.Finish("Mini map facing arrow");
        }

        public static Sprite Paw()
        {
            var p = new Canvas(64);
            p.Ellipse(96, 70, 42, 34, Color.white);
            p.Ellipse(45, 112, 16, 23, Color.white); p.Ellipse(77, 139, 17, 24, Color.white);
            p.Ellipse(116, 139, 17, 24, Color.white); p.Ellipse(149, 112, 16, 23, Color.white);
            return p.Finish("Maze Paw");
        }

        public static Sprite Wall()
        {
            var p = new Canvas(64);
            p.Fill(Hex(0x3A7562));
            p.RoundRect(6, 5, 186, 187, 28, Hex(0x315D50));
            p.RoundRect(6, 17, 186, 187, 28, Hex(0x4F8A6B));
            p.Ellipse(49, 125, 32, 31, Hex(0x659B73)); p.Ellipse(106, 152, 35, 23, Hex(0x6CA377));
            p.Ellipse(150, 104, 28, 33, Hex(0x5D976F)); p.Ellipse(90, 65, 39, 26, Hex(0x4A8163));
            p.Line(40, 143, 51, 148, 5, Hex(0x86B38A)); p.Line(103, 164, 117, 162, 4, Hex(0x86B38A));
            return p.Finish("Maze Hedge Wall");
        }

        public static Sprite Floor()
        {
            var p = new Canvas(64);
            p.Fill(Hex(0xEFE3CA));
            p.Line(0, 1, 192, 1, 2, Hex(0xE0D1B7)); p.Line(1, 0, 1, 192, 2, Hex(0xE0D1B7));
            for (int i = 0; i < 14; i++)
            {
                int x = 12 + (i * 47 + 19) % 165, y = 12 + (i * 71 + 31) % 165;
                p.Ellipse(x, y, 2 + i % 3, 1.6f, Hex(0xBCAE95, .23f));
            }
            return p.Finish("Maze Sand Path");
        }
    }
}
