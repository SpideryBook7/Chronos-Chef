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
            BuildFoodDisplays();
            actorSurface = new Material(shader) { mainTexture = chef.mainTexture };
            actorMesh = new Mesh { name = "Posed Ares" };
            actorMesh.MarkDynamic();
            actor = AddSurface("Ares",actorMesh,actorSurface);


            lens = new GameObject("Galley camera").AddComponent<Camera>();
            lens.transform.SetParent(transform,false);
            lens.transform.localRotation = Quaternion.Euler(10,0,0);
            lens.transform.localPosition = new Vector3(-.08f,3.96f,-15.67f);
            lens.orthographic = false;
            lens.fieldOfView = 50;
            lens.clearFlags = CameraClearFlags.SolidColor;
            lens.backgroundColor = KitchenView.C("18263D");
            lens.cullingMask = 1 << SceneLayer;
            lens.nearClipPlane = .1f; lens.farClipPlane = 52;
            lens.allowHDR = false;
            Image = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32) { name="Orbital galley view" };
            Image.Create(); lens.targetTexture = Image; lens.aspect = 16f/9;
            DressEarthKitchen();
        }

        readonly Transform[] seats=new Transform[3];
        readonly Mesh[] guests=new Mesh[3];
        Matrix4x4 projection;
        public bool CameraMoved()
        {
            var next=lens.projectionMatrix*lens.worldToCameraMatrix;
            if(next==projection) return false;
            projection=next;
            return true;
        }
        Sprite backdrop;
        readonly Sprite[] portraits=new Sprite[4];
        readonly SpriteRenderer[] diners=new SpriteRenderer[3];

        void DressEarthKitchen()
        {
            var texture=Resources.Load<Texture2D>("EarthKitchen");
            backdrop=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            var wall=new GameObject("Earth restaurant");
            wall.layer=SceneLayer;
            wall.transform.SetParent(transform,false);
            wall.transform.position=lens.ViewportToWorldPoint(new Vector3(.5f,.5f,36));
            wall.transform.rotation=lens.transform.rotation;
            float height=72*Mathf.Tan(lens.fieldOfView*Mathf.Deg2Rad*.5f);
            wall.transform.localScale=new Vector3(height*lens.aspect/backdrop.bounds.size.x,height/backdrop.bounds.size.y,1);
            var plate=wall.AddComponent<SpriteRenderer>();
            plate.sprite=backdrop;
            plate.sortingOrder=-10;
            var sheet=Resources.Load<Texture2D>("EarthGuests");
            float[] cuts={0,.284f,.493f,.753f,1};
            for(int i=0;i<4;i++)
            {
                int left=Mathf.RoundToInt(sheet.width*cuts[i]);
                int right=Mathf.RoundToInt(sheet.width*cuts[i+1]);
                portraits[i]=Sprite.Create(sheet,new Rect(left,0,right-left,sheet.height),new Vector2(.5f,0),sheet.height/1.72f,0,SpriteMeshType.FullRect);
            }
            for(int i=0;i<3;i++)
            {
                var guest=new GameObject("Passenger "+(i+1));
                guest.layer=SceneLayer;
                seats[i]=guest.transform;
                seats[i].SetParent(transform,false);
                seats[i].position=lens.ViewportToWorldPoint(new Vector3(.38f+i*.12f,.516f,19));
                seats[i].rotation=lens.transform.rotation;
                diners[i]=guest.AddComponent<SpriteRenderer>();
            }
        }

        public void Seat(int slot,int kind,float phase)
        {
            diners[slot].sprite=portraits[kind];
            seats[slot].localRotation=lens.transform.localRotation*Quaternion.Euler(0,0,Mathf.Sin(phase)*1.7f);
        }
        readonly Mesh[] foodMeshes=new Mesh[System.Enum.GetValues(typeof(Food)).Length];
        MeshFilter board, burner, hands;
        Material foodMaterial;

        MeshFilter FoodDisplay(string title, Food food, Vector3 position, float size)
        {
            var part=AddSurface(title,foodMeshes[(int)food],foodMaterial);
            part.localPosition=position;
            part.localScale=Vector3.one*size;
            return part.GetComponent<MeshFilter>();
        }

        void BuildFoodDisplays()
        {
            foodMaterial=new Material(galleySurface);
            for(int i=1;i<foodMeshes.Length;i++) foodMeshes[i]=KitchenModelGraphic.CreateFoodMesh((Food)i);
            for(int i=0;i<3;i++)
            {
                float offset=(i-1)*.43f;
                float stagger=i==1 ? .34f : -.24f;
                FoodDisplay("Marbled dodo",Food.RawDodo,new Vector3(-6.15f+offset,.75f,-3.90f+stagger),.40f);
                FoodDisplay("Quantum radish",Food.Root,new Vector3(-5.93f+offset,.76f,-.50f+stagger),.48f);
                FoodDisplay("Brioche",Food.Bread,new Vector3(6.43f+offset,1.00f,-1.27f+stagger),.46f);
            }
            board=FoodDisplay("Preparation",Food.Empty,new Vector3(-1.30f,.86f,.08f),.61f);
            burner=FoodDisplay("Cooking",Food.Empty,new Vector3(1.45f,1.04f,.03f),.63f);
            hands=FoodDisplay("In hand",Food.Empty,Vector3.zero,.43f);
        }

        public void BindChef(KitchenModelGraphic chef) => chef.SetWorldSkin(actorMesh);

        public void UpdateService(KitchenGame game, KitchenModelGraphic chef)
        {
            for(int i=0;i<3;i++) { seats[i].gameObject.SetActive(game.Customers[i]); diners[i].sprite=portraits[game.Species[i]]; }
            actor.localPosition = FloorPoint(game.ChefPosition);
            actor.localScale = Vector3.one*chef.WorldScale;
            board.sharedMesh=foodMeshes[(int)game.PrepFood];
            burner.sharedMesh=foodMeshes[(int)game.StoveFood];
            hands.sharedMesh=foodMeshes[(int)game.Carry];
            if(game.Carry!=Food.Empty)
            {
                var grip=chef.GripPoint();
                hands.transform.localPosition=actor.localPosition+new Vector3(grip.x,grip.z,grip.y)*chef.WorldScale;
            }
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
            Destroy(backdrop);
            foreach(var portrait in portraits) Destroy(portrait);
            foreach(var food in foodMeshes) if(food) Destroy(food);
            Destroy(foodMaterial);
            Destroy(galleyMesh); Destroy(actorMesh); Destroy(galleySurface); Destroy(actorSurface);
        }
    }
}
