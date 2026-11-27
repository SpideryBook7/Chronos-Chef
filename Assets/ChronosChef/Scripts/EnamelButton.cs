// A rounded enamel rim with a clipped corner, baked once and sliced around any label.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChronosChef
{
    public sealed class EnamelButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        static Sprite surface;
        Vector3 resting;
        bool pressed;
        internal static void Style(Button button)
        {
            if (!surface)
            {
                const int size=64;
                var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Enamel control",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color32[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float dx=Mathf.Max(0,Mathf.Abs(x-31.5f)-21);
                    float dy=Mathf.Max(0,Mathf.Abs(y-31.5f)-21);
                    float edge=Mathf.Min(Mathf.Min(x+.5f,size-x-.5f),Mathf.Min(y+.5f,size-y-.5f));
                    edge=Mathf.Min(edge,10.5f-Mathf.Sqrt(dx*dx+dy*dy));
                    edge=Mathf.Min(edge,(x+size-y-6)*.707f);
                    byte value=(byte)(edge<2?112:edge<4?240:edge<6?157:222+y*.46f);
                    pixels[y*size+x]=new Color32(value,value,value,(byte)(255*Mathf.Clamp01(edge)));
                }
                texture.SetPixels32(pixels);texture.Apply(false,true);
                surface=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(13,13,13,13));
            }
            button.image.sprite=surface;button.image.type=Image.Type.Sliced;
            if(!button.GetComponent<EnamelButton>())button.gameObject.AddComponent<EnamelButton>();
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(pressed||!GetComponent<Button>().IsInteractable())return;
            resting=transform.localScale;pressed=true;transform.localScale=resting*.975f;
        }
        public void OnPointerUp(PointerEventData e)=>Release();
        void OnDisable()=>Release();
        void Release(){if(pressed)transform.localScale=resting;pressed=false;}
    }
}
