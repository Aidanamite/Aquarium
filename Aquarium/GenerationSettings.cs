using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Aquarium
{
    public abstract class GenerationSettings
    {
        public float GlassThickness;
        public float SwimSpeed;
        public bool Snap;
        public GenerationSettings(float glassThickness, float swimSpeed, bool snap = true) => (GlassThickness, SwimSpeed, Snap) = (glassThickness, swimSpeed, snap);
        public abstract Mesh GenerateMesh(string meshName);
        public abstract MovementRules GenerateMovement();
        public abstract Collider[] GenerateColliders(GameObject target);
        public Collider[] GenerateColliders(Component target) => GenerateColliders(target.gameObject);
        public abstract void PopulateQuadPrefab(Transform target, BlockQuad floorTemplate, BlockQuad wallTemplate);
        public abstract CostMultiple[] GenerateRecipe();
    }
    public class CylinderGeneration : GenerationSettings
    {
        public int Steps;
        public float Radius;
        public float Height;
        public CylinderGeneration(int steps, float radius, float height, float glassThickness, float swimSpeed, bool snap = true) : base(glassThickness, swimSpeed, snap) => (Steps, Radius, Height) = (steps, radius, height);
        public override Mesh GenerateMesh(string meshName)
        {
            var builder = new MeshBuilder();
            for (int i = 0; i < Steps; i++)
            {
                Vertex GetPoint(int step, float height, float radius, int unique = 0) => new Vertex(new Vector3(Mathf.Sin(Mathf.PI * 2 * ((step % Steps) / (float)Steps)) * radius, height, Mathf.Cos(Mathf.PI * 2 * ((step % Steps) / (float)Steps)) * radius),unique: unique);
                void AddSquare(float min, float max, float radius, int submesh) => builder.AddSquare(GetPoint(i, min, radius, submesh), GetPoint(i + 1, min, radius, submesh), GetPoint(i + 1, max, radius, submesh), GetPoint(i, max, radius, submesh), submesh);

                AddSquare(0, Height, Radius, 0);
                AddSquare(Height - GlassThickness, GlassThickness, Radius - GlassThickness, 0);
                AddSquare(GlassThickness, Height - GlassThickness, Radius - GlassThickness, 1);
                if (i > 0 && i + 1 < Steps)
                {
                    builder.AddTriangle(GetPoint(0, 0, Radius), GetPoint(i + 1, 0, Radius), GetPoint(i, 0, Radius), 0);
                    builder.AddTriangle(GetPoint(0, Height, Radius), GetPoint(i, Height, Radius), GetPoint(i + 1, Height, Radius), 0);
                    builder.AddTriangle(GetPoint(0, GlassThickness, Radius - GlassThickness), GetPoint(i, GlassThickness, Radius - GlassThickness), GetPoint(i + 1, GlassThickness, Radius - GlassThickness), 0);
                    builder.AddTriangle(GetPoint(0, Height - GlassThickness, Radius - GlassThickness), GetPoint(i + 1, Height - GlassThickness, Radius - GlassThickness), GetPoint(i, Height - GlassThickness, Radius - GlassThickness), 0);
                    builder.AddTriangle(GetPoint(0, GlassThickness, Radius - GlassThickness, 1), GetPoint(i + 1, GlassThickness, Radius - GlassThickness, 1), GetPoint(i, GlassThickness, Radius - GlassThickness, 1), 1);
                    builder.AddTriangle(GetPoint(0, Height - GlassThickness, Radius - GlassThickness, 1), GetPoint(i, Height - GlassThickness, Radius - GlassThickness, 1), GetPoint(i + 1, Height - GlassThickness, Radius - GlassThickness, 1), 1);
                }
            }
            return builder.ToMesh(meshName);
        }
        public override MovementRules GenerateMovement()
        {
            var settings = ScriptableObject.CreateInstance<CylinderMovement>();
            Main.created.Add(settings);
            settings.Bottom = GlassThickness;
            settings.Top = Height - GlassThickness;
            settings.Radius = Radius - GlassThickness;
            settings.SwimSpeed = SwimSpeed;
            return settings;
        }
        public override Collider[] GenerateColliders(GameObject target)
        {
            var n = new BoxCollider[ Steps / 2];
            var s = new Vector3(Mathf.Sin(Mathf.PI / n.Length / 2) * Radius * 2, Height, Mathf.Cos(Mathf.PI / n.Length / 2) * Radius * 2);
            for (int i = 0; i < n.Length; i++)
            {
                var g = new GameObject("ColliderPart" + i);
                g.transform.SetParent(target.transform, false);
                n[i] = g.AddComponent<BoxCollider>();
                n[i].size = s;
                n[i].center = new Vector3(0, Height / 2, 0);
                g.layer = target.layer;
                g.transform.localRotation = Quaternion.Euler(0, 180f * (i + 0.5f) / n.Length, 0);
            }
            return n;
        }
        public override void PopulateQuadPrefab(Transform target, BlockQuad floorTemplate, BlockQuad wallTemplate)
        {
            var n = Steps / 2;
            var s1 = new Vector3(Mathf.Sin(Mathf.PI / n / 2) * (Radius * 2 + 0.01f), Height, Mathf.Cos(Mathf.PI / n / 2) * (Radius * 2 + 0.01f));
            var s2 = new Vector3(Mathf.Sin(Mathf.PI / n / 2) * (Radius * 2), 0.01f, Mathf.Cos(Mathf.PI / n / 2) * (Radius * 2));
            for (int i = 0; i < n; i++)
            {
                {
                    var g = new GameObject("BlockQuad_Wall_" + i);
                    g.SetActive(false);
                    g.transform.SetParent(target, false);
                    var c = g.AddComponent<BoxCollider>();
                    c.isTrigger = true;
                    c.size = s1;
                    c.center = new Vector3(0, Height / 2, 0);
                    g.layer = wallTemplate.gameObject.layer;
                    g.transform.localRotation = Quaternion.Euler(0, 180f * (i + 0.5f) / n, 0);
                    g.AddComponent<BlockQuad>().CopyFieldsOf(wallTemplate);
                }
                {
                    var g = new GameObject("BlockQuad_Floor_" + i);
                    g.SetActive(false);
                    g.transform.SetParent(target, false);
                    var c = g.AddComponent<BoxCollider>();
                    c.isTrigger = true;
                    c.size = s2;
                    c.center = new Vector3(0, Height + 0.005f, 0);
                    g.layer = floorTemplate.gameObject.layer;
                    g.transform.localRotation = Quaternion.Euler(0, 180f * (i + 0.5f) / n, 0);
                    g.AddComponent<BlockQuad>().CopyFieldsOf(floorTemplate);
                    g.AddComponent<DisableBySnapping>().DisableOnSnapping = true;
                }
            }
            var s3 = new Vector3(BlockCreator.BlockSize, 0.01f, BlockCreator.BlockSize);
            var s = Mathf.Ceil(Radius * 2 / BlockCreator.BlockSize).Odd();
            for (int x = 0; x < s; x++)
                for (int y = 0; y < s; y++)
                {
                    if (new Vector2((x - (s / 2) + 0.5f).MoveTowards(0, 0.5f, out _) * BlockCreator.BlockSize, (y - (s / 2) + 0.5f).MoveTowards(0, 0.5f, out _) * BlockCreator.BlockSize).sqrMagnitude <= Radius * Radius)
                    {
                        var g = new GameObject("BlockQuad_Floor_" + x + "_" + y);
                        g.SetActive(false);
                        g.transform.SetParent(target, false);
                        var c = g.AddComponent<BoxCollider>();
                        c.isTrigger = true;
                        c.size = s3;
                        c.center = new Vector3(0, 0.005f, 0);
                        g.transform.localPosition = new Vector3((x - (s / 2) + 0.5f) * BlockCreator.BlockSize, Height, (y - (s / 2) + 0.5f) * BlockCreator.BlockSize);
                        g.layer = floorTemplate.gameObject.layer;
                        g.AddComponent<BlockQuad>().CopyFieldsOf(floorTemplate);
                        g.AddComponent<DisableBySnapping>().DisableOnSnapping = false;
                    }
                }
        }
        public override CostMultiple[] GenerateRecipe() => new CostMultiple[] { new CostMultiple(new[] { ItemManager.GetItemByName("Glass") },Mathf.CeilToInt(((Radius * 2 * Mathf.PI * Height) + (Radius * Radius * Mathf.PI * 2)) / 22)) };
    }
    public class BowlGeneration : CylinderGeneration
    {
        public BowlGeneration(int steps, float radius, float height, float glassThickness, float swimSpeed, bool snap = true) : base(steps, radius, height, glassThickness, swimSpeed, snap) { }
        public override Mesh GenerateMesh(string meshName)
        {
            var builder = new MeshBuilder();
            for (int i = 0; i < Steps; i++)
            {
                Vertex GetPoint(int step, float height, float radius, int unique = 0)
                {
                    radius -= (1 - Mathf.Cos((height - (Height / 2)) / Radius * Mathf.PI / 2)) * Radius / 2;
                    return new Vertex(new Vector3(Mathf.Sin(Mathf.PI * 2 * ((step % Steps) / (float)Steps)) * radius, height, Mathf.Cos(Mathf.PI * 2 * ((step % Steps) / (float)Steps)) * radius), unique: unique);
                }
                void AddSquare(float min, float max, float radius, int submesh) => builder.AddSquare(GetPoint(i, min, radius, submesh), GetPoint(i + 1, min, radius, submesh), GetPoint(i + 1, max, radius, submesh), GetPoint(i, max, radius, submesh), submesh);

                for (int j = 0; j < Steps; j++)
                {
                    AddSquare(j / (float)Steps * Height, (j + 1) / (float)Steps * Height, Radius, 0);
                    AddSquare(GlassThickness + ((j + 1) / (float)Steps * (Height - GlassThickness)), GlassThickness + (j / (float)Steps * (Height - GlassThickness)), Radius - GlassThickness, 0);
                    if (j < Steps - 1)
                        AddSquare(GlassThickness + (j / (float)Steps * (Height - GlassThickness)), GlassThickness + ((j + 1) / (float)Steps * (Height - GlassThickness)), Radius - GlassThickness, 1);
                }
                builder.AddSquare(GetPoint(i, Height, Radius), GetPoint(i + 1, Height, Radius), GetPoint(i + 1, Height, Radius - GlassThickness), GetPoint(i, Height, Radius - GlassThickness), 0);
                if (i > 0 && i + 1 < Steps)
                {
                    builder.AddTriangle(GetPoint(0, 0, Radius), GetPoint(i + 1, 0, Radius), GetPoint(i, 0, Radius), 0);
                    builder.AddTriangle(GetPoint(0, GlassThickness, Radius - GlassThickness), GetPoint(i, GlassThickness, Radius - GlassThickness), GetPoint(i + 1, GlassThickness, Radius - GlassThickness), 0);
                    builder.AddTriangle(GetPoint(0, GlassThickness, Radius - GlassThickness, 1), GetPoint(i + 1, GlassThickness, Radius - GlassThickness, 1), GetPoint(i, GlassThickness, Radius - GlassThickness, 1), 1);
                    builder.AddTriangle(GetPoint(0, (Steps - 1) / (float)Steps * (Height - GlassThickness) + GlassThickness, Radius - GlassThickness, 1), GetPoint(i, (Steps - 1) / (float)Steps * (Height - GlassThickness) + GlassThickness, Radius - GlassThickness, 1), GetPoint(i + 1, (Steps - 1) / (float)Steps * (Height - GlassThickness) + GlassThickness, Radius - GlassThickness, 1), 1);
                }
            }
            return builder.ToMesh(meshName);
        }
        public override MovementRules GenerateMovement()
        {
            var settings = ScriptableObject.CreateInstance<SimpleBowlMovement>();
            Main.created.Add(settings);
            settings.Height = Height / 2;
            settings.Radius = Radius - GlassThickness;
            settings.SwimSpeed = SwimSpeed;
            return settings;
        }
    }
    public class BoxGeneration : GenerationSettings
    {
        public float Width;
        public float Depth;
        public float Height;
        public BoxGeneration(float width, float depth, float height, float glassThickness, float swimSpeed, bool snap = true) : base(glassThickness, swimSpeed, snap) => (Width, Depth, Height) = (width, depth, height);

        public override Mesh GenerateMesh(string meshName)
        {
            var builder = new MeshBuilder();
            var size = new[] { Width / 2, Height / 2, Depth / 2 };
            var center = new Vector3(0, Height / 2, 0);
            for (int i = 0; i < 6; i++)
            {
                var a = new[] { 0f, 0, 0 };
                var b = new[] { 0f, 0, 0 };
                var c = new[] { 0f, 0, 0 };
                var d = new[] { 0f, 0, 0 };
                a[(i + 1) % 3] = d[(i + 1) % 3] = -(c[(i + 1) % 3] = b[(i + 1) % 3] = size[(i + 1) % 3]);
                a[(i + 2) % 3] = b[(i + 2) % 3] = -(c[(i + 2) % 3] = d[(i + 2) % 3] = size[(i + 2) % 3]);
                a[i % 3] = b[i % 3] = c[i % 3] = d[i % 3] = size[i % 3];
                if (i >= 3)
                    for (int j = 0; j < 3; j++)
                        if (j != (i + 1) % 3)
                        {
                            a[j] = -a[j];
                            b[j] = -b[j];
                            c[j] = -c[j];
                            d[j] = -d[j];
                        }
                builder.AddSquare(a.ToVector3() + center, b.ToVector3() + center, c.ToVector3() + center, d.ToVector3() + center, 0);
                for (int j = 0; j < 3; j++)
                {
                    float Off(float[] v) => v[j].MoveTowards(0, GlassThickness, out _);
                    a[j] = Off(a);
                    b[j] = Off(b);
                    c[j] = Off(c);
                    d[j] = Off(d);
                }
                builder.AddSquare(new Vertex(a.ToVector3() + center,unique: 1), new Vertex(b.ToVector3() + center, unique: 1), new Vertex(c.ToVector3() + center, unique: 1), new Vertex(d.ToVector3() + center, unique: 1), 1);
                builder.AddSquare(a.ToVector3() + center, d.ToVector3() + center, c.ToVector3() + center, b.ToVector3() + center, 0);
            }
            return builder.ToMesh(meshName);
        }
        public override MovementRules GenerateMovement()
        {
            var settings = ScriptableObject.CreateInstance<BoxMovement>();
            Main.created.Add(settings);
            settings.Max = new Vector3(Width / 2 - GlassThickness, Height - GlassThickness, Depth / 2 - GlassThickness);
            settings.Min = new Vector3(-Width / 2 + GlassThickness, GlassThickness, -Depth / 2 + GlassThickness);
            settings.SwimSpeed = SwimSpeed;
            return settings;
        }
        public override Collider[] GenerateColliders(GameObject target)
        {
            var b = target.AddComponent<BoxCollider>();
            b.size = new Vector3(Width, Height, Depth);
            b.center = new Vector3(0, Height / 2, 0);
            return new[] { b };
        }

        public override void PopulateQuadPrefab(Transform target, BlockQuad floorTemplate, BlockQuad wallTemplate)
        {
            {
                var g = new GameObject("BlockQuad_Wall");
                g.SetActive(false);
                g.transform.SetParent(target, false);
                var c = g.AddComponent<BoxCollider>();
                c.isTrigger = true;
                c.size = new Vector3(Width + 0.02f, Height, Depth + 0.02f);
                c.center = new Vector3(0, Height / 2, 0);
                g.layer = wallTemplate.gameObject.layer;
                var q = g.AddComponent<BlockQuad>();
                q.CopyFieldsOf(wallTemplate);
                Array.Resize(ref q.acceptableBuildSides, q.acceptableBuildSides.Length + 2);
                q.acceptableBuildSides[q.acceptableBuildSides.Length - 1] = new BlockSurface() { dpsType = DPS.Wall, surfaceType = SurfaceType.Left };
                q.acceptableBuildSides[q.acceptableBuildSides.Length - 2] = new BlockSurface() { dpsType = DPS.Wall, surfaceType = SurfaceType.Right };
            }
            {
                var s = new Vector3(BlockCreator.BlockSize, 0.01f, BlockCreator.BlockSize);
                var w = Mathf.Ceil(Width / BlockCreator.BlockSize);
                w += 1 - (w % 2);
                var h = Mathf.Ceil(Depth / BlockCreator.BlockSize);
                h += 1 - (h % 2);
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                    {
                        var g = new GameObject("BlockQuad_Floor_" + x + "_" + y);
                        g.SetActive(false);
                        g.transform.SetParent(target, false);
                        var c = g.AddComponent<BoxCollider>();
                        c.isTrigger = true;
                        c.size = s;
                        c.center = new Vector3(0, 0.005f, 0);
                        g.transform.localPosition = new Vector3((x - (w / 2) + 0.5f) * BlockCreator.BlockSize, Height, (y - (h / 2) + 0.5f) * BlockCreator.BlockSize);
                        g.layer = floorTemplate.gameObject.layer;
                        g.AddComponent<BlockQuad>().CopyFieldsOf(floorTemplate);
                    }
            }
        }
        public override CostMultiple[] GenerateRecipe() => new CostMultiple[] { new CostMultiple(new[] { ItemManager.GetItemByName("Glass") }, Mathf.CeilToInt(((Width * Height * 2) + (Depth * Height * 2) + (Width * Depth * 2)) / 22)) };
    }

    public class OpenBoxGeneration : BoxGeneration
    {
        public OpenBoxGeneration(float width, float depth, float height, float glassThickness, float swimSpeed, bool snap = true) : base(width, depth, height, glassThickness, swimSpeed, snap) { }

        public override Mesh GenerateMesh(string meshName)
        {
            var builder = new MeshBuilder();
            var size = new[] { Width / 2, Height / 2, Depth / 2 };
            var center = new Vector3(0, Height / 2, 0);
            for (int i = 0; i < 6; i++)
            {
                var a = new[] { 0f, 0, 0 };
                var b = new[] { 0f, 0, 0 };
                var c = new[] { 0f, 0, 0 };
                var d = new[] { 0f, 0, 0 };
                a[(i + 1) % 3] = d[(i + 1) % 3] = -(c[(i + 1) % 3] = b[(i + 1) % 3] = size[(i + 1) % 3]);
                a[(i + 2) % 3] = b[(i + 2) % 3] = -(c[(i + 2) % 3] = d[(i + 2) % 3] = size[(i + 2) % 3]);
                a[i % 3] = b[i % 3] = c[i % 3] = d[i % 3] = size[i % 3];
                if (i >= 3)
                    for (int j = 0; j < 3; j++)
                        if (j != (i + 1) % 3)
                        {
                            a[j] = -a[j];
                            b[j] = -b[j];
                            c[j] = -c[j];
                            d[j] = -d[j];
                        }
                if (i != 1)
                    builder.AddSquare(a.ToVector3() + center, b.ToVector3() + center, c.ToVector3() + center, d.ToVector3() + center, 0);
                for (int j = 0; j < 3; j++)
                {
                    float Off(float[] v) => v[j].MoveTowards(0,GlassThickness,out _);
                    a[j] = Off(a);
                    b[j] = Off(b);
                    c[j] = Off(c);
                    d[j] = Off(d);
                }
                builder.AddSquare(new Vertex(a.ToVector3() + center, unique: 1), new Vertex(b.ToVector3() + center, unique: 1), new Vertex(c.ToVector3() + center, unique: 1), new Vertex(d.ToVector3() + center, unique: 1), 1);
                if (i != 1)
                    builder.AddSquare(a.ToVector3() + center, d.ToVector3() + center, c.ToVector3() + center, b.ToVector3() + center, 0);
            }
            var v1 = size.ToVector3() + center;
            var v2 = new Vector3(size[0].MoveTowards(0, GlassThickness, out _), size[1].MoveTowards(0, GlassThickness, out _), size[2].MoveTowards(0, GlassThickness, out _)) + center;
            builder.AddSquare(v1, Quaternion.Euler(0, 90, 0) * v1, Quaternion.Euler(0, 90, 0) * v2, v2);
            builder.AddSquare(Quaternion.Euler(0, 90, 0) * v1, Quaternion.Euler(0, 180, 0) * v1, Quaternion.Euler(0, 180, 0) * v2, Quaternion.Euler(0, 90, 0) * v2);
            builder.AddSquare(Quaternion.Euler(0, 180, 0) * v1, Quaternion.Euler(0, 270, 0) * v1, Quaternion.Euler(0, 270, 0) * v2, Quaternion.Euler(0, 180, 0) * v2);
            builder.AddSquare(Quaternion.Euler(0, 270, 0) * v1, v1, v2, Quaternion.Euler(0, 270, 0) * v2);
            return builder.ToMesh(meshName);
        }

        public override MovementRules GenerateMovement()
        {
            var settings = ScriptableObject.CreateInstance<SimpleBowlMovement>();
            Main.created.Add(settings);
            settings.Radius = Mathf.Min(Width, Depth) / 2 - GlassThickness;
            settings.Height = Height / 2;
            settings.SwimSpeed = SwimSpeed;
            return settings;
        }
    }
}
