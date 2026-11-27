using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        GameObject controlEditor;
        RectTransform[] controls, handles;
        Vector2[] defaults;
        readonly string[] controlNames = { "Pausa", "Recetario", "Congelar", "Anomalía" };
        readonly Image[] meterBars = new Image[12];
        static readonly Color inactiveSegment = C("554553");
        TMP_Text meterText;
        readonly KitchenModelGraphic[] orderModels = new KitchenModelGraphic[3];
        readonly RectTransform[,] burst = new RectTransform[3,10];
        readonly float[] burstAge = {2,2,2};
        readonly int[] burstKind = new int[3];
        readonly RectTransform[,] trails = new RectTransform[3,8];
        float nextOrderPose;

        void InstallServiceHud()
        {
            var meter = Panel(stage,"AnomalyGauge",1467,675,235,84,new Color(.12f,.11f,.18f,.89f));
            meterText = Label(meter,"",-110,22,220,26,19,Pink,FontStyles.Bold);
            for(int i=0;i<meterBars.Length;i++)
                meterBars[i]=Panel(meter,"Segment"+i,-98+18*i,-1,14,12+i*.6f,Ink,false).GetComponent<Image>();
            Label(meter,"ERRORES = CAOS TEMPORAL",-111,-24,222,20,12,Color.white);
            for(int i=0;i<3;i++)
            {
                var card=customerLabels[i].transform.parent;
                var holder = Panel(card,"Plato del día",-75,4,86,79,Color.clear);
                orderModels[i]=KitchenModelGraphic.Attach(holder,Food.Burger);
                for(int k=0;k<8;k++)
                {
                    var ray = Panel(stage,"Service impact",0,0,12,4,Cyan,false);
                    ray.SetSiblingIndex(menu.transform.GetSiblingIndex());
                    ray.gameObject.SetActive(false);
                    trails[i,k]=ray;
                }
                for(int k=0;k<10;k++)
                {
                    var spark=Dot(stage,"CustomerReaction",0,0,k%3==0?16:9,Gold);
                    spark.SetSiblingIndex(menu.transform.GetSiblingIndex());
                    spark.gameObject.SetActive(false);burst[i,k]=spark;
                }
            }
            game.CustomerStormedOut += StartReaction;
            EnamelButton.Style(borrowButton);
            EnamelButton.Style(stage.Find("RecipeHelp").GetComponent<Button>());
            controls = new[]{(RectTransform)pause.transform,(RectTransform)stage.Find("RecipeHelp"),(RectTransform)borrowButton.transform,meter};
            defaults = new Vector2[controls.Length];handles=new RectTransform[controls.Length];
            controlEditor=ScreenPanel("ControlLayout");
            var preview=PageArt(controlEditor,"EarthService"); preview.color=new Color(.62f,.62f,.62f);
            preview.transform.SetAsFirstSibling();
            for(int i=0;i<controls.Length;i++)
            {
                var target=controls[i];defaults[i]=target.anchoredPosition;
                string key="ChronosChef.Hud."+target.name;
                target.anchoredPosition=new Vector2(PlayerPrefs.GetFloat(key+".x",defaults[i].x),PlayerPrefs.GetFloat(key+".y",defaults[i].y));
                var handle=Panel(controlEditor.transform,"Move"+target.name,0,0,target.rect.width*target.localScale.x,target.rect.height*target.localScale.y,C("385354"));
                handle.anchorMin=handle.anchorMax=Vector2.zero;
                handle.GetComponent<Image>().raycastTarget=true;
                Label(handle,controlNames[i],-handle.rect.width/2,0,handle.rect.width,handle.rect.height,18,Color.white,FontStyles.Bold);
                handles[i]=handle;handle.gameObject.AddComponent<HudHandle>().PlacementValid=LayoutFits;
            }
            var note=Panel(controlEditor.transform,"LayoutToolbar",0,361,680,96,C("233B38"));
            Label(note,"Arrastra los controles",-323,0,268,65,22,Color.white);
            var reset=Button(note,"ResetLayout",28,0,196,57,C("526760"));
            Label(reset.transform,"Restablecer",-97,0,194,48,18,Color.white);
            reset.onClick.AddListener(()=>{for(int i=0;i<handles.Length;i++)handles[i].anchoredPosition=defaults[i];});
            var done=Button(note,"SaveLayout",220,0,188,57,C("477150"));
            Label(done.transform,"Guardar",-93,0,186,48,20,Color.white,FontStyles.Bold);
            done.onClick.AddListener(CloseControlEditor);
            ((RectTransform)settings.transform.Find("OpenPrivacy")).anchoredPosition=new Vector2(-332,-340);
            var edit=Button(settings.transform,"MoveControls",306,-340,460,55,C("44634E"));
            Label(edit.transform,"ACOMODAR CONTROLES",-220,0,440,48,21,Color.white,FontStyles.Bold);
            edit.onClick.AddListener(()=>{
                preview.texture=WorldKitchen(KitchenLevel.World(game.Level));
                for(int i=0;i<handles.Length;i++)handles[i].anchoredPosition=controls[i].anchoredPosition;
                controlEditor.SetActive(true);controlEditor.transform.SetAsLastSibling();
            });
            controlEditor.SetActive(false);
            meter.SetSiblingIndex(menu.transform.GetSiblingIndex());
            meter.gameObject.SetActive(false);
        }

        bool LayoutFits()
        {
            for(int i=0;i<handles.Length;i++)
            {
                var a=handles[i];var rect=new Rect(a.anchoredPosition-a.rect.size*.5f,a.rect.size);
                if(rect.Overlaps(new Rect(460,763,680,96))) return false;
                for(int j=0;j<i;j++)
                {
                    var b=handles[j];if(rect.Overlaps(new Rect(b.anchoredPosition-b.rect.size*.5f,b.rect.size)))return false;
                }
            }
            return true;
        }
        void CloseControlEditor()
        {
            for(int i=0;i<controls.Length;i++)
            {
                var pos=handles[i].anchoredPosition;controls[i].anchoredPosition=pos;
                string key="ChronosChef.Hud."+controls[i].name;
                PlayerPrefs.SetFloat(key+".x",pos.x);PlayerPrefs.SetFloat(key+".y",pos.y);
            }
            PlayerPrefs.Save();controlEditor.SetActive(false);
        }
        void StartReaction(int slot,int kind) { burstAge[slot]=0; burstKind[slot]=kind; }
        void RefreshServiceHud(KitchenGame g)
        {
            controls[3].gameObject.SetActive(g.Running && !g.Paused && g.MeterDiscovered);
            int pct=g.CloneRemaining>0?100:Mathf.RoundToInt(g.Anomaly);
            meterText.SetText("ANOMALÍA  {0}%",pct);
            for(int k=0;k<meterBars.Length;k++)
                meterBars[k].color = k*100 < pct*meterBars.Length ? Color.Lerp(Gold,Pink,k/11f) : inactiveSegment;
            bool animate=Time.unscaledTime>=nextOrderPose;
            if(animate)nextOrderPose=Time.unscaledTime+.08f;
            for(int i=0;i<3;i++)
            {
                if(animate && g.Customers[i])
                {
                    var model=orderModels[i];
                    model.Show(KitchenModelGraphic.RegionModel(KitchenLevel.World(g.Level),g.Orders[i].ToString()));
                    float wave=ReducedMotion?0:Mathf.Sin(castClock*2.4f+i);
                    model.Pose(-16+wave*20);
                    model.rectTransform.localScale=Vector3.one*(1+wave*.035f);
                }
                float age=burstAge[i];
                if(age>1 && !burst[i,0].gameObject.activeSelf)continue;
                int kind=burstKind[i];float t=Mathf.Clamp01(age);
                Vector2 origin=InGalley?galley.ScreenPoint(new Vector3((i-1)*3.9f,1.3f,2.92f)):new Vector2(SeatX(i),702);
                var station=(RectTransform)stations[kind==1?2:1].transform;
                Vector2 impact=station.anchoredPosition+Vector2.up*20;
                for(int k=0;k<8;k++)
                {
                    var ray=trails[i,k];
                    bool visible=age<.65f && g.Running && kind!=3 && !ReducedMotion;
                    ray.gameObject.SetActive(visible);
                    if(!visible)continue;
                    float u=k/8f, v=(k+1)/8f;
                    Vector2 Point(float along) => Vector2.Lerp(origin,impact,along) +
                        Vector2.up*Mathf.Sin(along*Mathf.PI)*(kind==1?Mathf.Sin(along*34+t*19)*15:55);
                    Vector2 a=Point(u), b=Point(v), delta=b-a;
                    ray.anchoredPosition=(a+b)*.5f;
                    ray.sizeDelta=new Vector2(delta.magnitude*(kind==1?1.02f:.38f),kind==1?3:6);
                    ray.localEulerAngles=Vector3.forward*Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg;
                    Color tint=kind==1?Cyan:kind==2?Green:Gold;
                    tint.a=1-age/.65f;ray.GetComponent<Image>().color=tint;
                }
                for(int k=0;k<10;k++)
                {
                    var p=burst[i,k];p.gameObject.SetActive(age<1 && g.Running);
                    float a=k*Mathf.PI*.2f;
                    var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                    p.anchoredPosition=origin+dir*(ReducedMotion?28:(kind==1?125:74)*t)+Vector2.up*(kind==2?62*t:-32*t*t);
                    p.localScale=Vector3.one*(1-t)*(kind==1?1.7f:1);
                    p.GetComponent<Image>().color=kind==1?(k%2==0?Gold:Cyan):kind==2?Green:Pink;
                }
                if(!g.Paused)burstAge[i]+=Time.deltaTime;
            }
        }
    }
}
