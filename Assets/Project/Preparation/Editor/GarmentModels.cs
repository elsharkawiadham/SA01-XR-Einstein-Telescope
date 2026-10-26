using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Original cleanroom clothing meshes. Front faces -Z; dimensions are metres.
/// The meshes illustrate the client's clothing reference. They are not cloth simulations.</summary>
public static class GarmentModels
{
    const string Folder = "Assets/Project/Preparation/Art/";
    const int Sides = 28;
    static List<Vector3> vertices;
    static List<Vector2> coordinates;
    static List<int>[] triangles;
    static int surface;

    static void Begin()
    {
        vertices = new List<Vector3>();
        coordinates = new List<Vector2>();
        triangles = new[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
        surface = 0;
    }

    static int Vertex(Vector3 p, Vector2 uv)
    {
        vertices.Add(p); coordinates.Add(uv); return vertices.Count - 1;
    }

    static void Tri(int a, int b, int c) { triangles[surface].Add(a); triangles[surface].Add(b); triangles[surface].Add(c); }
    static void Quad(int a, int b, int c, int d) { Tri(a, b, c); Tri(c, b, d); }

    // Elliptical sewn sections; small folds change the silhouette as well as the normals.
    static void Section(Vector3 a, Vector3 b, float width, float depth, float taper = 1,
        int rings = 8, float folds = .018f, bool cap = true, int sides = Sides)
    {
        var axis = (b - a).normalized;
        var x = Vector3.Cross(axis, Mathf.Abs(axis.y) > .8f ? Vector3.forward : Vector3.up).normalized;
        var z = Vector3.Cross(x, axis);
        int start = vertices.Count;
        for (int j = 0; j <= rings; j++)
        {
            float f = j / (float)rings;
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                float ripple = 1 + folds * Mathf.Sin(f * 23 + angle * 5) * Mathf.Sin(f * Mathf.PI);
                var p = Vector3.Lerp(a, b, f) + (x * Mathf.Cos(angle) * width + z * Mathf.Sin(angle) * depth)
                    * Mathf.Lerp(1, taper, f) * ripple;
                Vertex(p, new Vector2(i / (float)sides, f));
            }
        }
        for (int j = 0; j < rings; j++)
            for (int i = 0; i < sides; i++)
            {
                int aa = start + j * (sides + 1) + i;
                Quad(aa, aa + sides + 1, aa + 1, aa + sides + 2);
            }
        if (!cap) return;
        int first = Vertex(a, Vector2.zero), last = Vertex(b, Vector2.one);
        for (int i = 0; i < sides; i++)
        {
            Tri(first, start + i, start + i + 1);
            Tri(last, start + rings * (sides + 1) + i + 1, start + rings * (sides + 1) + i);
        }
    }

    static void Cord(IList<Vector3> path, float radius, int material = 1, bool loop = false)
    {
        // Shared rings prevent alternating intersections and sawtooth edges at every segment.
        if (path.Count < 2) return;
        int previous = surface; surface = material;
        int start = vertices.Count, sides = 8, count = path.Count;
        var firstTangent = (path[1] - path[0]).normalized;
        Vector3 x = Vector3.Cross(firstTangent, Mathf.Abs(firstTangent.y) > .8f ? Vector3.forward : Vector3.up).normalized;
        for (int j = 0; j < count; j++)
        {
            Vector3 before = path[j == 0 ? (loop ? count - 1 : 0) : j - 1];
            Vector3 after = path[j == count - 1 ? (loop ? 0 : count - 1) : j + 1];
            Vector3 tangent = (after - before).normalized;
            x = Vector3.ProjectOnPlane(x, tangent).normalized;
            if (x.sqrMagnitude < .5f) x = Vector3.Cross(tangent, Mathf.Abs(tangent.y) > .8f ? Vector3.forward : Vector3.up).normalized;
            Vector3 z = Vector3.Cross(x, tangent);
            for (int i = 0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                Vertex(path[j] + (x * Mathf.Cos(angle) + z * Mathf.Sin(angle)) * radius,
                    new Vector2(i / (float)sides, j / (float)(count - 1)));
            }
        }
        int links = loop ? count : count - 1;
        for (int j = 0; j < links; j++) for (int i = 0; i < sides; i++)
        {
            int row = start + j * sides, next = start + ((j + 1) % count) * sides;
            Quad(row + i, next + i, row + (i + 1) % sides, next + (i + 1) % sides);
        }
        surface = previous;
    }

    static float FrontAt(float y, float[] profileY, float[] depths, float[] offsets, float folds)
    {
        int j = 0;
        while (j < profileY.Length - 2 && y > profileY[j + 1]) j++;
        float f = Mathf.InverseLerp(profileY[j], profileY[j + 1], y);
        float a = 1.5f * Mathf.PI;
        float z0 = (offsets == null ? 0 : offsets[j]) - depths[j] * (1 + folds * Mathf.Sin(a * 7 + j * 1.4f));
        float z1 = (offsets == null ? 0 : offsets[j + 1]) - depths[j + 1] * (1 + folds * Mathf.Sin(a * 7 + (j + 1) * 1.4f));
        return Mathf.Lerp(z0, z1, f);
    }

    static void FrontPlacket(float[] y, float[] depths, float[] offsets, float folds, float width)
    {
        // Match the actual centre-front vertices; a thin sewn strip sits above, never inside, the fabric.
        int previous = surface; surface = 1; int start = vertices.Count;
        for (int j = 0; j < y.Length; j++)
        {
            float z = FrontAt(y[j], y, depths, offsets, folds) - .0025f;
            Vertex(new Vector3(-width * .5f, y[j], z), new Vector2(0, j / (float)(y.Length - 1)));
            Vertex(new Vector3(width * .5f, y[j], z), new Vector2(1, j / (float)(y.Length - 1)));
        }
        for (int j = 0; j < y.Length - 1; j++) Quad(start + j * 2, start + j * 2 + 2, start + j * 2 + 1, start + j * 2 + 3);
        surface = previous;
    }

    static void Ring(float y, float width, float depth, float z = 0, float radius = .002f, int material = 1)
    {
        var p = new List<Vector3>();
        for (int i = 0; i < 36; i++)
        {
            float a = i * Mathf.PI * 2 / 36;
            p.Add(new Vector3(Mathf.Cos(a) * width, y, z + Mathf.Sin(a) * depth));
        }
        Cord(p, radius, material, true);
    }

    static void Body(float[] y, float[] widths, float[] depths, float[] offsets = null, float fold = .015f)
    {
        int start = vertices.Count;
        for (int j = 0; j < y.Length; j++)
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2 / Sides;
                float r = 1 + fold * Mathf.Sin(a * 7 + j * 1.4f);
                Vertex(new Vector3(Mathf.Cos(a) * widths[j] * r, y[j],
                    Mathf.Sin(a) * depths[j] * r + (offsets == null ? 0 : offsets[j])),
                    new Vector2(i / (float)Sides, j / (float)(y.Length - 1)));
            }
        for (int j = 0; j < y.Length - 1; j++)
            for (int i = 0; i < Sides; i++)
            {
                int a = start + j * (Sides + 1) + i;
                Quad(a, a + Sides + 1, a + 1, a + Sides + 2);
            }
    }

    static Vector3 Limb(Vector3 startPoint, Vector3 joint, Vector3 endPoint,
        Vector2 startRadius, Vector2 jointRadius, Vector2 endRadius)
    {
        // One continuous surface crosses the elbow/knee; no intersecting caps or dark joint gaps.
        Vector3 control = 2 * joint - (startPoint + endPoint) * .5f;
        int first = vertices.Count, rings = 24;
        for (int j = 0; j <= rings; j++)
        {
            float t = j / (float)rings, u = 1 - t;
            Vector3 centre = u * u * startPoint + 2 * u * t * control + t * t * endPoint;
            Vector3 tangent = (2 * u * (control - startPoint) + 2 * t * (endPoint - control)).normalized;
            Vector3 x = Vector3.Cross(tangent, Vector3.forward).normalized;
            Vector3 z = Vector3.Cross(x, tangent);
            Vector2 radii = t <= .5f ? Vector2.Lerp(startRadius, jointRadius, t * 2)
                : Vector2.Lerp(jointRadius, endRadius, (t - .5f) * 2);
            for (int a = 0; a <= Sides; a++)
            {
                float angle = a * Mathf.PI * 2 / Sides;
                float fold = 1 + .035f * Mathf.Sin(t * 23 + angle * 5) * Mathf.Sin(t * Mathf.PI);
                Vertex(centre + (x * Mathf.Cos(angle) * radii.x + z * Mathf.Sin(angle) * radii.y) * fold,
                    new Vector2(a / (float)Sides, t));
            }
        }
        for (int j = 0; j < rings; j++) for (int a = 0; a < Sides; a++)
        {
            int v = first + j * (Sides + 1) + a;
            Quad(v, v + Sides + 1, v + 1, v + Sides + 2);
        }
        return (endPoint - control).normalized;
    }

    static void Suit(bool personal)
    {
        var torsoY = new[] { -.16f, -.07f, .08f, .22f, .34f, .38f };
        var torsoDepth = new[] { .105f, .12f, .102f, .125f, .105f, .078f };
        Body(torsoY, new[] { .19f, .205f, .18f, .225f, .235f, .12f }, torsoDepth);
        // Upright stand collar with an open centre, rather than a capped cylinder.
        Body(new[] { .375f, .425f, .445f }, new[] { .091f, .082f, .083f }, new[] { .071f, .066f, .068f });
        Ring(.444f, .084f, .069f, 0, .0023f);
        foreach (float side in new[] { -1f, 1f })
        {
            var shoulder = new Vector3(side * .205f, .305f, 0);
            var elbow = new Vector3(side * .32f, .105f, -.025f);
            var wrist = new Vector3(side * .36f, -.14f, -.06f);
            Vector3 sleeveEnd = Limb(shoulder, elbow, wrist,
                new Vector2(.079f, .088f), new Vector2(.073f, .075f), new Vector2(.054f, .055f));
            surface = 1;
            Section(wrist, wrist + sleeveEnd * .035f, .054f, .055f, .97f, 4, .04f, false);
            surface = 0;
            if (!personal)
            {
                var hip = new Vector3(side * .101f, -.11f, 0);
                var knee = new Vector3(side * .113f, -.49f, -.015f);
                var ankle = new Vector3(side * .123f, -.84f, .012f);
                Vector3 legEnd = Limb(hip, knee, ankle,
                    new Vector2(.102f, .108f), new Vector2(.087f, .094f), new Vector2(.065f, .069f));
                surface = 1; Section(ankle, ankle + legEnd * .025f, .065f, .069f, .98f, 3, .03f, false); surface = 0;
            }
        }
        // A continuous surface-following zipper placket, with a small pull above its surface.
        FrontPlacket(torsoY, torsoDepth, null, .015f, .012f);
        float pullBottom = FrontAt(.285f, torsoY, torsoDepth, null, .015f) - .006f;
        float pullTop = FrontAt(.31f, torsoY, torsoDepth, null, .015f) - .006f;
        surface = 2;
        Section(new Vector3(0, .285f, pullBottom), new Vector3(0, .31f, pullTop), .004f, .0015f, 1, 2, 0);
        surface = 0;
        Ring(.025f, .186f, .111f, 0, .002f);
        if (personal)
        {
            foreach (float s in new[] { -1f, 1f })
                Cord(new[] { new Vector3(s * .09f, -.03f, -.116f), new Vector3(s * .15f, .07f, -.101f) }, .003f);
        }
    }

    static void Hood()
    {
        // A three-dimensional hood shell, open around the face, closed around the crown.
        int start = vertices.Count, rings = 28, sides = 48;
        for (int j = 0; j <= rings; j++)
        {
            float p = Mathf.Lerp(.73f, Mathf.PI - .015f, j / (float)rings);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                float r = 1 + .012f * Mathf.Sin(a * 7 + p * 8);
                Vertex(new Vector3(.15f * Mathf.Sin(p) * Mathf.Cos(a) * r,
                    .2f * Mathf.Sin(p) * Mathf.Sin(a) + .015f, -.168f * Mathf.Cos(p) + .035f),
                    new Vector2(i / (float)sides, j / (float)rings));
            }
        }
        for (int j = 0; j < rings; j++)
            for (int i = 0; i < sides; i++)
            {
                int a = start + j * (sides + 1) + i;
                Quad(a, a + 1, a + sides + 1, a + sides + 2);
            }
        var rim = new List<Vector3>();
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides;
            rim.Add(new Vector3(.15f * Mathf.Sin(.73f) * Mathf.Cos(a), .2f * Mathf.Sin(.73f) * Mathf.Sin(a) + .015f,
                -.168f * Mathf.Cos(.73f) + .032f));
        }
        Cord(rim, .0035f, 1, true);
        // The hood falls over the shoulders as a continuous cape/bib.
        var bibY = new[] { -.30f, -.27f, -.22f, -.15f, -.105f };
        var bibDepth = new[] { .16f, .156f, .13f, .105f, .097f };
        var bibOffset = new[] { .045f, .04f, .035f, .035f, .035f };
        Body(bibY, new[] { .255f, .25f, .198f, .124f, .107f }, bibDepth, bibOffset, .025f);
        FrontPlacket(bibY, bibDepth, bibOffset, .025f, .004f);

    }

    static void Mask()
    {
        // The pleats are one continuous piece of fabric with curved edges.
        int start = vertices.Count, nx = 24, ny = 24;
        for (int j = 0; j <= ny; j++)
        {
            float fy = j / (float)ny;
            float y = Mathf.Lerp(-.049f, .049f, fy);
            float pleat = .0035f * Mathf.Sin(fy * Mathf.PI * 8);
            for (int i = 0; i <= nx; i++)
            {
                float u = i / (float)nx, x = Mathf.Lerp(-.084f, .084f, u);
                float bulge = .018f * Mathf.Sin(u * Mathf.PI);
                Vertex(new Vector3(x, y, -.012f - bulge + pleat), new Vector2(u, fy));
            }
        }
        for (int j = 0; j < ny; j++) for (int i = 0; i < nx; i++)
        {
            int a = start + j * (nx + 1) + i;
            Quad(a, a + nx + 1, a + 1, a + nx + 2);
        }
        foreach (float y in new[] { -.049f, .049f })
        {
            var edge = new List<Vector3>();
            for (int i = 0; i <= nx; i++) edge.Add(new Vector3(Mathf.Lerp(-.084f, .084f, i / (float)nx), y,
                -.013f - .018f * Mathf.Sin(i / (float)nx * Mathf.PI)));
            Cord(edge, .002f, 1);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            Cord(new[] { new Vector3(s * .084f, -.049f, -.012f), new Vector3(s * .084f, .049f, -.012f) }, .002f);
            var ear = new List<Vector3>();
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI * 2 / 40;
                ear.Add(new Vector3(s * (.083f + .03f * (1 + Mathf.Cos(a))), .042f * Mathf.Sin(a), .008f + .006f * Mathf.Cos(a)));
            }
            Cord(ear, .0016f, 1, true);
        }
        Cord(new[] { new Vector3(-.027f, .047f, -.030f), new Vector3(0, .052f, -.034f), new Vector3(.027f, .047f, -.030f) }, .0015f, 2);
    }

    static void Finger(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float radius)
    {
        int first = vertices.Count, rings = 20, sides = 20;
        for (int j = 0; j <= rings; j++)
        {
            float t = j / (float)rings, u = 1 - t;
            Vector3 centre = u*u*u*a + 3*u*u*t*b + 3*u*t*t*c + t*t*t*d;
            Vector3 tangent = (3*u*u*(b-a) + 6*u*t*(c-b) + 3*t*t*(d-c)).normalized;
            Vector3 x = Vector3.Cross(tangent, Vector3.forward).normalized;
            Vector3 z = Vector3.Cross(x, tangent);
            float tip = Mathf.Clamp01((t - .80f) / .20f);
            float r = radius * Mathf.Lerp(1, .86f, t) * Mathf.Sqrt(Mathf.Max(0, 1 - tip*tip));
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                Vertex(centre + x * Mathf.Cos(angle) * r + z * Mathf.Sin(angle) * r * .87f,
                    new Vector2(i/(float)sides, t));
            }
        }
        for (int j = 0; j < rings; j++) for (int i = 0; i < sides; i++)
        {
            int v = first + j * (sides + 1) + i;
            Quad(v, v + sides + 1, v + 1, v + sides + 2);
        }
    }

    static void Glove(bool right)
    {
        float s = right ? -1 : 1;
        Body(new[] { -.113f, -.08f, -.052f, -.02f, .017f, .034f },
            new[] { .032f, .03f, .033f, .045f, .044f, .036f },
            new[] { .018f, .016f, .018f, .021f, .019f, .015f });
        Ring(-.112f, .033f, .019f, 0, .0018f);
        for (int i = 0; i < 4; i++)
        {
            float x = s * (-.031f + i * .021f);
            float length = new[] { .067f, .084f, .079f, .058f }[i];
            float radius = new[] { .0098f, .0103f, .0095f, .0081f }[i];
            Finger(new Vector3(x, .022f, 0), new Vector3(x * 1.08f, .022f + length * .45f, 0),
                new Vector3(x * 1.1f, .022f + length * .80f, -.012f),
                new Vector3(x * 1.09f, .022f + length, -.020f), radius);
        }
        Finger(new Vector3(s * -.030f, -.043f, 0), new Vector3(s * -.063f, -.018f, -.005f),
            new Vector3(s * -.071f, .012f, -.012f), new Vector3(s * -.073f, .033f, -.020f), .014f);
    }

    static void Footwear(bool overshoe)
    {
        // Rounded toe, narrower heel, shaped instep, and a separate non-slip sole.
        surface = 2;
        Body(new[] { -.126f, -.116f, -.104f }, new[] { .084f, .09f, .089f },
            new[] { .16f, .165f, .162f }, new[] { -.05f, -.05f, -.05f }, 0);
        Section(new Vector3(0, -.126f, -.05f), new Vector3(0, -.123f, -.05f), .084f, .16f, 1, 1, 0);
        surface = 0;
        Body(overshoe ? new[] { -.103f, -.085f, -.043f, -.005f, .036f, .07f }
            : new[] { -.103f, -.085f, -.065f, -.039f, -.018f, -.009f },
            new[] { .088f, .089f, .083f, .073f, .065f, .062f },
            new[] { .161f, .16f, .145f, .117f, .088f, .078f },
            new[] { -.05f, -.049f, -.04f, -.025f, .001f, .009f }, overshoe ? .018f : .004f);
        if (overshoe)
        {
            Body(new[] { .02f, .08f, .16f, .24f, .30f }, new[] { .072f, .069f, .082f, .08f, .068f },
                new[] { .091f, .08f, .078f, .076f, .068f }, new[] { .012f, .012f, .015f, .012f, .012f }, .045f);
            Ring(.299f, .069f, .069f, .012f, .003f);
            Ring(.28f, .072f, .07f, .012f, .0015f);
        }
        else
        {
            // Open-top cleanroom clog, matching the footwear silhouette in the supplied slide.
            Ring(-.009f, .063f, .078f, .009f, .003f);
            // A recessed insole makes the opening legible from above, instead of a tall ankle cylinder.
            surface = 1;
            Section(new Vector3(0, -.087f, .009f), new Vector3(0, -.085f, .009f), .059f, .073f, 1, 1, 0);
            surface = 0;
            var strap = new List<Vector3>();
            for (int i = 0; i <= 24; i++)
            {
                float a = Mathf.Lerp(0, Mathf.PI, i / 24f);
                strap.Add(new Vector3(Mathf.Cos(a) * .075f, -.025f, .012f + Mathf.Sin(a) * .088f));
            }
            Cord(strap, .006f, 1);
        }
        for (int i = 0; i < 7; i++)
            Cord(new[] { new Vector3(-.064f, -.124f, -.16f + i * .035f),
                new Vector3(.064f, -.124f, -.16f + i * .035f) }, .002f, 1);
    }

    static void HairCap()
    {
        int start = vertices.Count, rings = 16, sides = 64;
        for (int j = 0; j <= rings; j++)
        {
            float p = Mathf.Lerp(.01f, Mathf.PI * .58f, j / (float)rings);
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                float pleat = 1 + .035f * Mathf.Sin(a * 24) * Mathf.Sin(p);
                Vertex(new Vector3(.16f * Mathf.Sin(p) * Mathf.Cos(a) * pleat,
                    .14f * Mathf.Cos(p), .15f * Mathf.Sin(p) * Mathf.Sin(a) * pleat),
                    new Vector2(i / (float)sides, j / (float)rings));
            }
        }
        for (int j = 0; j < rings; j++) for (int i = 0; i < sides; i++)
        {
            int a = start + j * (sides + 1) + i;
            Quad(a, a + 1, a + sides + 1, a + sides + 2);
        }
        Ring(.14f * Mathf.Cos(Mathf.PI * .58f), .16f * Mathf.Sin(Mathf.PI * .58f),
            .15f * Mathf.Sin(Mathf.PI * .58f), 0, .003f);
    }

    static void Glasses()
    {
        foreach (float s in new[] { -1f, 1f })
        {
            var frame = new List<Vector3>();
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI * 2 / 40;
                // A superellipse gives the reference's broad rounded safety-goggle lenses.
                float x = Mathf.Sign(Mathf.Cos(a)) * Mathf.Pow(Mathf.Abs(Mathf.Cos(a)), .65f) * .036f;
                float y = Mathf.Sign(Mathf.Sin(a)) * Mathf.Pow(Mathf.Abs(Mathf.Sin(a)), .75f) * .028f;
                frame.Add(new Vector3(s * .043f + x, y, -.015f + .04f * Mathf.Abs(x)));
            }
            Cord(frame, .004f, 1, true);
            surface = 3;
            int centre = Vertex(new Vector3(s * .043f, 0, -.019f), new Vector2(.5f, .5f));
            int start = vertices.Count;
            foreach (var p in frame) Vertex(p, new Vector2(p.x * 6 + .5f, p.y * 12 + .5f));
            for (int i = 0; i < frame.Count; i++) Tri(centre, start + (i + 1) % frame.Count, start + i);
            surface = 0;
        }
        Cord(new[] { new Vector3(-.012f, .008f, -.016f), new Vector3(0, .012f, -.025f), new Vector3(.012f, .008f, -.016f) }, .004f, 1);
        var strap = new List<Vector3>();
        for (int i = 0; i <= 40; i++)
        {
            float a = i * Mathf.PI / 40;
            strap.Add(new Vector3(Mathf.Cos(a) * .084f, 0, Mathf.Sin(a) * .15f));
        }
        Cord(strap, .005f, 2);
    }

    static Mesh Save(string name)
    {
        Directory.CreateDirectory(Folder);
        var mesh = new Mesh { name = name, subMeshCount = triangles.Length };
        mesh.SetVertices(vertices); mesh.SetUVs(0, coordinates);
        for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        string path = Folder + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing)
        {
            EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(mesh); return existing;
        }
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }

    public static Mesh Make(string type)
    {
        Begin();
        switch (type)
        {
            case "Coverall": Suit(false); break;
            case "PersonalJacket": Suit(true); break;
            case "Hood": Hood(); break;
            case "Mask": Mask(); break;
            case "Glove": case "LeftGlove": Glove(false); break;
            case "RightGlove": Glove(true); break;
            case "Boot": Footwear(false); break;
            case "Overshoe": Footwear(true); break;
            case "HairCap": HairCap(); break;
            case "SafetyGlasses": Glasses(); break;
            default: throw new ArgumentException("Unknown garment mesh: " + type, nameof(type));
        }
        return Save(type);
    }

    static Material Finish(string name, Color color, float smoothness, bool woven = false, bool glass = false)
    {
        string path = Folder + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", smoothness); mat.SetFloat("_Metallic", 0);
        mat.SetFloat("_Cull", (float)CullMode.Off);
        if (woven)
        {
            string texturePath = Folder + "CleanroomWeave.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (!texture)
            {
                var pixels = new Texture2D(128, 128, TextureFormat.RGB24, false);
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float thread = .975f + .017f * Mathf.Sin(x * Mathf.PI * .5f) * Mathf.Cos(y * Mathf.PI * .5f);
                    if (x % 64 == 0 || y % 64 == 0) thread -= .045f;
                    pixels.SetPixel(x, y, new Color(thread, thread, thread));
                }
                pixels.Apply(); File.WriteAllBytes(texturePath, pixels.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(pixels);
                AssetDatabase.ImportAsset(texturePath); texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }
            mat.SetTexture("_BaseMap", texture); mat.SetTextureScale("_BaseMap", new Vector2(9, 9));
        }
        if (glass)
        {
            mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", 0);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0); mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        EditorUtility.SetDirty(mat); return mat;
    }

    public static Material[] Materials(string type)
    {
        Directory.CreateDirectory(Folder);
        bool fabric = type == "Coverall" || type == "PersonalJacket" || type == "Hood" || type == "HairCap" || type == "Overshoe";
        Color baseColor = type == "PersonalJacket" ? new Color(.055f, .10f, .17f)
            : type == "Hood" || type == "HairCap" ? new Color(.67f, .79f, .85f)
            : type == "Mask" ? new Color(.75f, .87f, .89f) : new Color(.91f, .93f, .91f);
        var cloth = Finish("Garment " + type, baseColor, fabric ? .22f : .36f, fabric);
        var seam = Finish("Garment seams and elastic", new Color(.68f, .76f, .78f), .25f);
        var rubber = Finish("Garment sole and fasteners", new Color(.22f, .28f, .31f), .3f);
        var lens = Finish("Safety goggle lenses", new Color(.61f, .87f, .9f, .32f), .8f, false, true);
        return new[] { cloth, seam, rubber, lens };
    }

    /// <summary>Updates an existing object without replacing its transform, collider or interactions.</summary>
    public static void Apply(GameObject item, string type)
    {
        var filter = item.GetComponent<MeshFilter>();
        if (!filter) filter = item.AddComponent<MeshFilter>();
        var renderer = item.GetComponent<MeshRenderer>();
        if (!renderer) renderer = item.AddComponent<MeshRenderer>();
        filter.sharedMesh = Make(type); renderer.sharedMaterials = Materials(type);
        var legacyZip = item.transform.Find("Front zipper");
        if (legacyZip) UnityEngine.Object.DestroyImmediate(legacyZip.gameObject);
    }
}
