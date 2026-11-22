using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChronosChef
{
    // Orthographic mesh projection inside the same masked uGUI space as the stations.
    // Uses one shared model library and the existing UI material, without extra cameras.
    public sealed class KitchenModelGraphic : MaskableGraphic, IDragHandler
    {
        struct Corner
        {
            public Vector3 point, normal;
        }
        sealed class Face
        {
            public Corner a, b, c;
            public Color color;
            public bool head;
            public Vector3 center, normal;
        }
        sealed class Model
        {
            public Face[] faces;
            public float fit, middle;
        }
        struct DrawFace : IComparable<DrawFace>
        {
            public int index;
            public float depth;
            public int CompareTo(DrawFace other) => depth.CompareTo(other.depth);
        }
        static Dictionary<string, Model> models;
        Model model;
        DrawFace[] order;
        float yaw, dragYaw, headYaw;
        bool inspectable;
        const float TiltSin = .52f, TiltCos = .854166f;

        static Corner ReadCorner(BinaryReader reader) => new Corner {
            point = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            normal = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
        };
        static void LoadLibrary()
        {
            if (models != null) return;
            models = new Dictionary<string, Model>();
            var bytes = Resources.Load<TextAsset>("KitchenModels");
            if (!bytes) return;
            using var reader = new BinaryReader(new MemoryStream(bytes.bytes));
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "CCM2") throw new InvalidDataException("Unknown kitchen model format");
            int count = reader.ReadInt32();
            for (int k = 0; k < count; k++)
            {
                var name = Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadByte()));
                var entry = new Model { faces = new Face[reader.ReadInt32()] };
                float radius = 0, bottom = float.MaxValue, top = float.MinValue;
                for (int i = 0; i < entry.faces.Length; i++)
                {
                    var face = new Face {
                        color = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), 255),
                        head = reader.ReadByte() == 1,
                        a = ReadCorner(reader), b = ReadCorner(reader), c = ReadCorner(reader)
                    };
                    face.center = (face.a.point + face.b.point + face.c.point) / 3;
                    face.normal = Vector3.Cross(face.b.point - face.a.point, face.c.point - face.a.point).normalized;
                    Measure(face.a.point); Measure(face.b.point); Measure(face.c.point);
                    entry.faces[i] = face;
                }
                void Measure(Vector3 p)
                {
                    radius = Mathf.Max(radius, Mathf.Sqrt(p.x*p.x + p.y*p.y));
                    bottom = Mathf.Min(bottom, p.z); top = Mathf.Max(top, p.z);
                }
                entry.middle = (bottom + top) * .5f;
                entry.fit = .95f / Mathf.Max(2 * radius, (top - bottom)*TiltCos + 2 * radius * TiltSin);
                models.Add(name, entry);
            }
        }
        public static KitchenModelGraphic Attach(RectTransform parent, Food food, bool canInspect = false)
            => Attach(parent, food.ToString(), canInspect);
        public static KitchenModelGraphic Attach(RectTransform parent, string name, bool canInspect = false)
        {
            LoadLibrary();
            if (!models.ContainsKey(name)) return null;
            var go = new GameObject("Model3D", typeof(RectTransform), typeof(CanvasRenderer), typeof(KitchenModelGraphic));
            var graphic = go.GetComponent<KitchenModelGraphic>();
            graphic.rectTransform.SetParent(parent, false);
            graphic.rectTransform.anchorMin = Vector2.zero; graphic.rectTransform.anchorMax = Vector2.one;
            graphic.rectTransform.offsetMin = graphic.rectTransform.offsetMax = Vector2.zero;
            graphic.inspectable = canInspect; graphic.raycastTarget = canInspect;
            graphic.Show(name); return graphic;
        }
        public void Show(Food food) => Show(food.ToString());
        public void Show(string name)
        {
            if (!models.TryGetValue(name, out var next) || next == model) return;
            model = next;
            if (order == null || order.Length < model.faces.Length) order = new DrawFace[model.faces.Length];
            dragYaw = 0; SetVerticesDirty();
        }
        public void Pose(float degrees)
        {
            degrees = Mathf.Round(degrees * 2) * .5f;
            if (Mathf.Abs(yaw - degrees) < .4f) return;
            yaw = degrees; SetVerticesDirty();
        }
        public void OnDrag(PointerEventData e)
        {
            if (!inspectable) return;
            dragYaw += 160 * e.delta.x / Mathf.Max(1, rectTransform.rect.width * canvas.scaleFactor);
            SetVerticesDirty();
        }
        public void Look(float offset)
        {
            offset = Mathf.Round(offset);
            if (Mathf.Approximately(headYaw, offset)) return;
            headYaw = offset; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (model == null) return;
            float angle = (yaw + dragYaw) * Mathf.Deg2Rad;
            float s = Mathf.Sin(angle), c = Mathf.Cos(angle);
            var facing = new Vector3(-s*TiltCos, -c*TiltCos, TiltSin);
            float headAngle = angle + headYaw * Mathf.Deg2Rad;
            float hs = Mathf.Sin(headAngle), hc = Mathf.Cos(headAngle);
            var headFacing = new Vector3(-hs*TiltCos, -hc*TiltCos, TiltSin);
            int visible = 0;
            for (int i = 0; i < model.faces.Length; i++)
            {
                var f = model.faces[i];
                var direction = f.head ? headFacing : facing;
                if (Vector3.Dot(f.normal, direction) <= 0) continue;
                order[visible++] = new DrawFace { index = i, depth = Vector3.Dot(f.center, direction) };
            }
            Array.Sort(order, 0, visible);
            var rect = rectTransform.rect;
            float scale = Mathf.Min(rect.width, rect.height) * model.fit;
            for (int i = 0; i < visible; i++)
            {
                var face = model.faces[order[i].index];
                int first = vh.currentVertCount;
                float sine = face.head ? hs : s, cosine = face.head ? hc : c;
                AddVertex(face.a, face.color, sine, cosine); AddVertex(face.b, face.color, sine, cosine); AddVertex(face.c, face.color, sine, cosine);
                vh.AddTriangle(first, first + 1, first + 2);
            }
            void AddVertex(Corner corner, Color pigment, float sin, float cos)
            {
                var p = corner.point;
                float x = cos*p.x - sin*p.y, y = sin*p.x + cos*p.y;
                var normal = corner.normal;
                float nx = cos*normal.x - sin*normal.y, ny = sin*normal.x + cos*normal.y;
                float key = Mathf.Clamp01(-.5f*nx - .44f*ny + .745f*normal.z);
                float rim = 1 - Mathf.Clamp01((-ny*TiltCos + normal.z*TiltSin)*4);
                float light = (.57f + .55f*key) * (1 - .26f*rim);
                float highlight = Mathf.Clamp01(-.24f*nx - .73f*ny + .63f*normal.z);
                highlight *= highlight; highlight *= highlight; highlight *= highlight;
                float gloss = highlight * highlight * .16f;
                var shade = new Color(pigment.r*light + gloss, pigment.g*light + gloss*.91f, pigment.b*(light+.035f) + gloss*.8f, 1);
                vh.AddVert(new Vector3(x*scale+rect.center.x,(y*TiltSin+(p.z-model.middle)*TiltCos)*scale+rect.center.y),shade, Vector2.zero);
            }
        }
    }
}
