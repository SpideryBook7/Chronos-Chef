using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ChronosChef
{
    public sealed partial class KitchenModelGraphic
    {
        struct SkinVertex
        {
            public Vector3 position, normal;
            public Vector2 uv;
            public BoneWeight weight;
        }
        static SkinVertex[] skinVertices;
        static ushort[] skinIndices;
        static Vector3[] skinPivots;
        static Texture2D albedo;
        bool useSkin;
        Vector3[] posedPoints;
        DrawFace[] skinOrder;
        public override Texture mainTexture => useSkin && albedo ? albedo : base.mainTexture;

        void LoadSkin()
        {
            if (skinVertices == null)
            {
                var asset = Resources.Load<TextAsset>("AresSkin");
                albedo = Resources.Load<Texture2D>("AresColor");
                using var r = new BinaryReader(new MemoryStream(asset.bytes));
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "ASK1") throw new InvalidDataException("Unknown Ares skin format");
                skinVertices = new SkinVertex[r.ReadInt32()];
                skinIndices = new ushort[r.ReadInt32()];
                skinPivots = new Vector3[(int)Joint.Count];
                Vector3 ReadPoint() => new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
                for (int i=0;i<skinPivots.Length;i++) skinPivots[i]=ReadPoint();
                for (int i=0;i<skinVertices.Length;i++)
                {
                    skinVertices[i] = new SkinVertex {
                        position=ReadPoint(), normal=ReadPoint(), uv=new Vector2(r.ReadSingle(),r.ReadSingle()),
                        weight=new BoneWeight {
                            boneIndex0=r.ReadByte(),boneIndex1=r.ReadByte(),boneIndex2=r.ReadByte(),boneIndex3=r.ReadByte(),
                            weight0=r.ReadSingle(),weight1=r.ReadSingle(),weight2=r.ReadSingle(),weight3=r.ReadSingle()
                        }
                    };
                }
                for (int i=0;i<skinIndices.Length;i++) skinIndices[i]=r.ReadUInt16();
            }
            posedPoints = new Vector3[skinVertices.Length];
            skinOrder = new DrawFace[skinIndices.Length/3];
        }

        void DrawSkin(VertexHelper vh)
        {
            var rect = rectTransform.rect;
            float scale = Mathf.Min(rect.width,rect.height)*model.fit;
            var tint = color;
            for (int i=0;i<skinVertices.Length;i++)
            {
                ref var v = ref skinVertices[i];
                var w=v.weight;
                var m=transforms[w.boneIndex0];
                Vector3 p=m.MultiplyPoint3x4(v.position)*w.weight0;
                Vector3 n=m.MultiplyVector(v.normal)*w.weight0;
                Blend(ref p,ref n,v,w.boneIndex1,w.weight1);
                Blend(ref p,ref n,v,w.boneIndex2,w.weight2);
                Blend(ref p,ref n,v,w.boneIndex3,w.weight3);
                posedPoints[i]=p;
                float light=.76f+.24f*Mathf.Clamp01(-.45f*n.x-.5f*n.y+.74f*n.z);
                vh.AddVert(rect.center + new Vector2(p.x,p.y*TiltSin+(p.z-model.middle)*TiltCos)*scale,
                    new Color(tint.r*light,tint.g*light,tint.b*light,tint.a),v.uv);
            }
            int count=0;
            for(int i=0;i<skinIndices.Length;i+=3)
            {
                var a=posedPoints[skinIndices[i]];
                var b=posedPoints[skinIndices[i+1]];
                var c=posedPoints[skinIndices[i+2]];
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),cameraDirection)<=0) continue;
                skinOrder[count++]=new DrawFace {index=i,depth=Vector3.Dot(a+b+c,cameraDirection)};
            }
            Array.Sort(skinOrder,0,count);
            for(int i=0;i<count;i++)
            {
                int t=skinOrder[i].index;
                vh.AddTriangle(skinIndices[t],skinIndices[t+1],skinIndices[t+2]);
            }
        }
        void Blend(ref Vector3 point,ref Vector3 normal,SkinVertex v,int joint,float influence)
        {
            if(influence<=0) return;
            var m=transforms[joint];
            point+=m.MultiplyPoint3x4(v.position)*influence;
            normal+=m.MultiplyVector(v.normal)*influence;
        }
    }
}
