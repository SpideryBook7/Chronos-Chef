using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChronosChef
{
    // The camera's image occupies the same safe-area frame as the touch controls.
    public sealed class OrbitalGalley : MonoBehaviour
    {
        // Floor coordinates are authored against the original deck projection.
        const float FloorPixelsPerMetre = 39.07311f;
        Camera lens;
        const int SceneLayer = 30;
        public RenderTexture Image { get; private set; }
        Mesh galleyMesh, actorMesh;
        Material galleySurface, actorSurface;
        Transform actor;
        float burnerPhase;
        static readonly int ChefFloor = Shader.PropertyToID("_ChefFloor");
        static readonly int Burner = Shader.PropertyToID("_Burner");

        public void Build(KitchenModelGraphic chef)
        {
            transform.position = new Vector3(4000,0,4000);
            var shader = Resources.Load<Shader>("GalleySurface");
            galleySurface = new Material(shader);
            galleyMesh = LoadGeometry();
            AddSurface("Orbital service deck", galleyMesh, galleySurface);

            actorSurface = new Material(shader) { mainTexture = chef.mainTexture };
            actorMesh = new Mesh { name = "Posed Ares" };
            actorMesh.MarkDynamic();
            actor = AddSurface("Ares",actorMesh,actorSurface);


            lens = new GameObject("Galley camera").AddComponent<Camera>();
            lens.transform.SetParent(transform,false);
            lens.transform.localRotation = Quaternion.Euler(10,0,0);
            lens.transform.localPosition = new Vector3(0,3.25f,-15.8f);
            lens.orthographic = false;
            lens.fieldOfView = 50;
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = KitchenView.C("18263D");
            lens.cullingMask = 1 << SceneLayer;
            lens.nearClipPlane = .1f; lens.farClipPlane = 52;
            lens.allowHDR = false;
            Image = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32) { name="Orbital galley view" };
            Image.Create(); lens.targetTexture = Image; lens.aspect = 16f/9;
        }

        public void BindChef(KitchenModelGraphic chef) => chef.SetWorldSkin(actorMesh);

        public void UpdateService(KitchenGame game, float modelScale)
        {
            actor.localPosition = FloorPoint(game.ChefPosition);
            actor.localScale = Vector3.one*modelScale;
            var feet = actor.localPosition;
            galleySurface.SetVector(ChefFloor,new Vector4(feet.x,feet.z,0,0));
            if (game.Running && !game.Paused) burnerPhase += Time.deltaTime;
            float heat = game.StoveFood == Food.Empty ? .17f : .75f;
            galleySurface.SetFloat(Burner,heat * (.9f + .1f*Mathf.Sin(burnerPhase*6)));
        }
        static Vector3 FloorPoint(Vector2 floor) => new Vector3((floor.x-800)/100f,.05f,(floor.y-473)/FloorPixelsPerMetre);
        public Vector2 FloorScreen(Vector2 floor) => ScreenPoint(FloorPoint(floor));
        public Vector2 ScreenPoint(Vector3 local)
        {
            var v = lens.WorldToViewportPoint(transform.TransformPoint(local));
            return new Vector2(v.x*1600,v.y*900);
        }
        public float ScreenScale(Vector3 local) => (ScreenPoint(local+Vector3.right).x-ScreenPoint(local).x)/100f;
        public Vector2 HandScreen(Vector3 point) => ScreenPoint(actor.localPosition + new Vector3(point.x,point.z,point.y)*actor.localScale.x);
        Transform AddSurface(string title, Mesh mesh, Material mat)
        {
            var part = new GameObject(title,typeof(MeshFilter),typeof(MeshRenderer));
            part.layer = SceneLayer; part.transform.SetParent(transform,false);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            part.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return part.transform;
        }
        static Mesh LoadGeometry()
        {
            using var r = new BinaryReader(new MemoryStream(Resources.Load<TextAsset>("OrbitalGalley").bytes));
            if (new string(r.ReadChars(4)) != "GAL1") throw new InvalidDataException("Invalid galley mesh");
            int count=r.ReadInt32(), indices=r.ReadInt32();
            var positions=new Vector3[count]; var normals=new Vector3[count]; var colours=new Color32[count];
            Vector3 ReadVector() => new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
            for (int i=0;i<count;i++)
            {
                positions[i]=ReadVector(); normals[i]=ReadVector();
                colours[i]=new Color32(r.ReadByte(),r.ReadByte(),r.ReadByte(),255);
            }
            var triangles=new int[indices];
            for (int i=0;i<indices;i++) triangles[i]=r.ReadInt32();
            var mesh=new Mesh {name="Orbital galley",indexFormat=count>65535 ? IndexFormat.UInt32 : IndexFormat.UInt16};
            mesh.vertices=positions; mesh.normals=normals; mesh.colors32=colours; mesh.triangles=triangles;
            mesh.UploadMeshData(true);
            return mesh;
        }
        void OnDestroy()
        {
            if(Image) { Image.Release(); Destroy(Image); }
            Destroy(galleyMesh); Destroy(actorMesh); Destroy(galleySurface); Destroy(actorSurface);
        }
    }
}
