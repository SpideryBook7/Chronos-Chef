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
    public sealed partial class KitchenModelGraphic : MaskableGraphic, IDragHandler
    {
        struct Corner
        {
            public Vector3 point, normal;
            public Color pigment;
            public byte joint;
        }
        sealed class Face
        {
            public int a, b, c;
            public byte joint;
            public Vector3 center, normal;
        }
        sealed class Model
        {
            public Face[] faces;
            public Corner[] corners;
            public float fit, middle;
            public int jointCount = 1;
        }
        struct DrawFace : IComparable<DrawFace>
        {
            public int index;
            public float depth;
            public int CompareTo(DrawFace other)
            {
                int result = depth.CompareTo(other.depth);
                return result == 0 ? index.CompareTo(other.index) : result;
            }
        }
        static Dictionary<string, Model> models;
        static Vector3[] pivots;
        // Indices match the rest pivots in ArtSource/kitchen_cast.py.
        enum Joint { Body, Head, ArmL, ArmR, LegL, LegR, ForeL, ForeR, ShinL, ShinR, Knife, Ladle, Count }
        public enum HandTool { None, Knife, Ladle }
        readonly Quaternion[] rotations = new Quaternion[(int)Joint.Count];
        readonly Matrix4x4[] transforms = new Matrix4x4[(int)Joint.Count];
        readonly Vector3[] viewDirections = new Vector3[(int)Joint.Count];
        readonly float[] depthOffsets = new float[(int)Joint.Count];
        static readonly Vector3 cameraDirection = new Vector3(0, -TiltCos, TiltSin);
        public struct CharacterPose
        {
            public float nod, lean, twist, leftArm, rightArm, leftLeg, rightLeg;
            public float leftElbow, rightElbow, leftKnee, rightKnee, bounce, gather;
            public HandTool tool;
        }
        CharacterPose pose;
        public void Articulate(CharacterPose value)
        {
            pose = value;
            SetVerticesDirty();
        }
        Model model;
        DrawFace[] order;
        int[] vertexMap;
        float yaw, dragYaw, headYaw;
        bool inspectable;
        // Orthographic view pitched 23 degrees above the work surface.
        const float TiltSin = .3907311f, TiltCos = .9205049f;

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
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "CCM3") throw new InvalidDataException("Unknown kitchen model format");
            pivots = new Vector3[reader.ReadByte()];
            if (pivots.Length != (int)Joint.Count) throw new InvalidDataException("Invalid kitchen skeleton");
            for (int i = 0; i < pivots.Length; i++)
                pivots[i] = new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
            int count = reader.ReadInt32();
            for (int k = 0; k < count; k++)
            {
                var name = Encoding.ASCII.GetString(reader.ReadBytes(reader.ReadByte()));
                var entry = new Model { faces = new Face[reader.ReadInt32()] };
                var corners = new List<Corner>();
                var indices = new Dictionary<(Vector3,Vector3,Color32,byte),int>();
                float radius = 0, bottom = float.MaxValue, top = float.MinValue;
                for (int i = 0; i < entry.faces.Length; i++)
                {
                    var pigment = new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), 255);
                    var face = new Face { joint = reader.ReadByte() };
                    face.a = IndexCorner(pigment, face.joint); face.b = IndexCorner(pigment, face.joint); face.c = IndexCorner(pigment, face.joint);
                    var a = corners[face.a].point; var b = corners[face.b].point; var c = corners[face.c].point;
                    face.center = (a + b + c) / 3;
                    entry.jointCount = Mathf.Max(entry.jointCount, face.joint + 1);
                    face.normal = Vector3.Cross(b - a, c - a).normalized;
                    entry.faces[i] = face;
                }
                entry.corners = corners.ToArray();
                int IndexCorner(Color32 pigment, byte joint)
                {
                    var corner = ReadCorner(reader);
                    var key = (corner.point,corner.normal,pigment,joint);
                    if (indices.TryGetValue(key, out int index)) return index;
                    index = corners.Count;
                    corner.pigment = pigment; corner.joint = joint;
                    indices.Add(key,index); corners.Add(corner); Measure(corner.point);
                    return index;
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
            useSkin = name == "Ares";
            if (useSkin) LoadSkin();
            SetMaterialDirty();
            if (order == null || order.Length < model.faces.Length) order = new DrawFace[model.faces.Length];
            if (vertexMap == null || vertexMap.Length < model.corners.Length) vertexMap = new int[model.corners.Length];
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
        // The carried item is a sibling in the kitchen Canvas. Use the same
        // projection as the animated mitts so it stays seated through turns.
        public Vector3 GripPoint()
        {
            PrepareTransforms();
            var left = transforms[(int)Joint.ForeL].MultiplyPoint3x4(useSkin ? skinPivots[10] : new Vector3(-.72f,-.18f,.86f));
            var right = transforms[(int)Joint.ForeR].MultiplyPoint3x4(useSkin ? skinPivots[11] : new Vector3(.72f,-.18f,.86f));
            return (left + right) * .5f;
        }
        public Vector2 GripCenter()
        {
            var p = GripPoint();
            var rect = rectTransform.rect;
            float scale = Mathf.Min(rect.width, rect.height) * model.fit;
            return rect.center + new Vector2(p.x, p.y*TiltSin + (p.z-model.middle)*TiltCos + .065f) * scale;
        }
        void PrepareTransforms()
        {
            var turn = Quaternion.AngleAxis(yaw + dragYaw, Vector3.forward);
            rotations[0] = Quaternion.Euler(pose.lean, 0, pose.twist);
            rotations[1] = Quaternion.Euler(pose.nod, 0, headYaw);
            rotations[2] = Quaternion.Euler(pose.leftArm, -pose.gather, 0);
            rotations[3] = Quaternion.Euler(pose.rightArm, pose.gather, 0);
            rotations[4] = Quaternion.Euler(pose.leftLeg, 0, 0);
            rotations[5] = Quaternion.Euler(pose.rightLeg, 0, 0);
            rotations[6] = Quaternion.Euler(pose.leftElbow, 0, 0);
            rotations[7] = Quaternion.Euler(pose.rightElbow, 0, 0);
            rotations[8] = Quaternion.Euler(pose.leftKnee, 0, 0);
            rotations[9] = Quaternion.Euler(pose.rightKnee, 0, 0);
            rotations[(int)Joint.Knife] = rotations[(int)Joint.Ladle] = Quaternion.identity;
            var root = Matrix4x4.TRS(Vector3.forward * pose.bounce, turn, Vector3.one);
            for (int i = 0; i < model.jointCount; i++)
            {
                var parent = root;
                if (i == 1 || i == 2 || i == 3) parent = transforms[0];
                else if (i >= (int)Joint.Knife) parent = transforms[(int)Joint.ForeR];
                else if (i >= (int)Joint.ForeL) parent = transforms[i - 4];
                var pivot = useSkin ? skinPivots[i] : pivots[i];
                var rotation = rotations[i];
                var transform = parent * Matrix4x4.TRS(pivot - rotation * pivot, rotation, Vector3.one);
                transforms[i] = transform;
                viewDirections[i] = transform.transpose.MultiplyVector(cameraDirection);
                depthOffsets[i] = Vector3.Dot(transform.GetColumn(3), cameraDirection);
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (model == null) return;
            PrepareTransforms();
            if (useSkin)
            {
                DrawSkin(vh);
                if (pose.tool == HandTool.None) return;
            }
            int visible = 0;
            for (int i = 0; i < model.faces.Length; i++)
            {
                var f = model.faces[i];
                if (useSkin && f.joint < (int)Joint.Knife) continue;
                if (f.joint == (int)Joint.Knife && pose.tool != HandTool.Knife ||
                    f.joint == (int)Joint.Ladle && pose.tool != HandTool.Ladle) continue;
                var direction = viewDirections[f.joint];
                if (Vector3.Dot(f.normal, direction) <= 0) continue;
                order[visible++] = new DrawFace { index = i, depth = Vector3.Dot(f.center, direction) + depthOffsets[f.joint] };
            }
            Array.Sort(order, 0, visible);
            var rect = rectTransform.rect;
            float scale = Mathf.Min(rect.width, rect.height) * model.fit;
            Array.Clear(vertexMap, 0, model.corners.Length);
            var tint = color;
            for (int i = 0; i < visible; i++)
            {
                var face = model.faces[order[i].index];
                vh.AddTriangle(VertexIndex(face.a), VertexIndex(face.b), VertexIndex(face.c));
            }
            int VertexIndex(int index)
            {
                if (vertexMap[index] > 0) return vertexMap[index] - 1;
                var corner = model.corners[index];
                var p = transforms[corner.joint].MultiplyPoint3x4(useSkin ? corner.point + skinPivots[11] - pivots[(int)Joint.Knife] : corner.point);
                float x = p.x, y = p.y;
                var normal = transforms[corner.joint].MultiplyVector(corner.normal);
                float nx = normal.x, ny = normal.y;
                var pigment = corner.pigment;
                float key = Mathf.Clamp01(-.5f*nx - .44f*ny + .745f*normal.z);
                float rim = 1 - Mathf.Clamp01((-ny*TiltCos + normal.z*TiltSin)*4);
                float light = (.57f + .55f*key) * (1 - .26f*rim);
                float highlight = Mathf.Clamp01(-.24f*nx - .73f*ny + .63f*normal.z);
                highlight *= highlight; highlight *= highlight; highlight *= highlight;
                float gloss = highlight * highlight * .16f;
                var shade = new Color(pigment.r*light + gloss, pigment.g*light + gloss*.91f, pigment.b*(light+.035f) + gloss*.8f, 1);
                vertexMap[index] = vh.currentVertCount + 1;
                vh.AddVert(new Vector3(x*scale+rect.center.x,(y*TiltSin+(p.z-model.middle)*TiltCos)*scale+rect.center.y),shade * tint, useSkin ? new Vector2(.003f,.003f) : Vector2.zero);
                return vertexMap[index] - 1;
            }
        }
    }
}
