using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        readonly Dictionary<RectTransform,(Vector2 position,Vector3 scale)> campaignLayout = new();
        void ApplyRearLayout()
        {
            void Place(RectTransform r,Vector2 screen,float scale)
            {
                campaignLayout.TryAdd(r,(r.anchoredPosition,r.localScale));
                r.anchoredPosition=screen;
                r.localScale=Vector3.one*scale;
            }
            void StationAt(Station s,Vector3 surface)
            {
                Place((RectTransform)stations[(int)s].transform,galley.ScreenPoint(surface),
                    Mathf.Clamp(galley.ScreenScale(surface),.72f,1));
            }
            StationAt(Station.Supply,new Vector3(-6.15f,.6f,-3.90f));
            StationAt(Station.Roots,new Vector3(-5.93f,.6f,-.50f));
            StationAt(Station.Trash,new Vector3(-6.52f,.55f,-6.28f));
            StationAt(Station.Assemble,new Vector3(6.43f,.7f,-1.27f));
            StationAt(Station.Cut,new Vector3(-1.30f,.6f,.02f));
            StationAt(Station.Stove,new Vector3(1.45f,.65f,.03f));
            var burner = galley.ScreenPoint(new Vector3(1.45f,0,-.85f));
            Place(stoveLabel.rectTransform,burner+Vector2.down*23,.77f);
            Place((RectTransform)cookFill.transform.parent,burner+Vector2.down*4,.74f);
            Place((RectTransform)burnFill.transform.parent,burner+Vector2.down*10,.74f);
            Place((RectTransform)stage.Find("RecipeHelp"),galley.ScreenPoint(new Vector3(6.50f,.63f,-5.92f)),.82f);
            var rims=stage.Cast<Transform>().Where(t=>t.name=="OrderRim").ToArray();
            const float scale=.73f;
            for (int i=0;i<3;i++)
            {
                var edge=galley.ScreenPoint(new Vector3((i-1)*3.9f,.95f,2.92f));
                Place((RectTransform)stage.Find("CustomerWindow"+i),edge+Vector2.up*(56*scale),scale);
                var order=edge+Vector2.up*(160*scale);
                Place((RectTransform)customerLabels[i].transform.parent,order,scale);
                Place((RectTransform)rims[i],order,scale);
                Place((RectTransform)stations[5+i].transform,edge+Vector2.up*(96*scale),scale);
                Place(comicSounds[i].rectTransform,edge+Vector2.up*15,scale);
            }
        }
        void RestoreRearLayout()
        {
            foreach(var item in campaignLayout)
            {
                item.Key.anchoredPosition=item.Value.position;
                item.Key.localScale=item.Value.scale;
            }
            campaignLayout.Clear();
        }
    }
}
