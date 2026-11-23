using TMPro;
using UnityEngine;

namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        KitchenModelGraphic ares, echo;
        readonly KitchenModelGraphic[,] visitors = new KitchenModelGraphic[3,4];
        RectTransform drivePad;
        UnityEngine.UI.Button useButton;
        TMP_Text useCaption, useTarget;
        static readonly string[] StationCaptions = { "DODO", "CORTE", "PLASMA", "PAN", "BASURA", "CLIENTE 1", "CLIENTE 2", "CLIENTE 3", "RAÍCES" };
        float castClock, angle, lastDistance, gait, carryBlend, nextChefFrame, nextVisitorFrame;
        static readonly string[] VisitorMeshes = { "Grog", "BipIdle", "Zorg", "Rex" };

        void BuildCastControls()
        {
            ares = MountCharacter(chef, "Ares", new Vector2(205,205), 48);
            echo = MountCharacter(clone, "Ares", new Vector2(205,205), 48);
            if (echo) echo.color = new Color(.73f,.46f,1);
            for (int slot = 0; slot < 3; slot++)
            for (int kind = 0; kind < 4; kind++)
                visitors[slot,kind] = MountCharacter(customerArt[slot][kind], VisitorMeshes[kind], new Vector2(190,190), 7);

            drivePad = Dot(stage, "PilotStick", 113, 81, 148, C("42687A"));
            drivePad.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Dot(drivePad, "Socket", 0, 0, 140, C("18283E"));
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f;
                Dot(drivePad, "Direction", Mathf.Sin(a)*57, Mathf.Cos(a)*57, 7, C("95D4C8"));
            }
            var knob = Dot(drivePad, "Thumb", 0, 0, 67, C("579F9F"));
            Dot(knob, "Grip", 0, 1, 57, C("304F65"));
            Dot(knob, "Locator", 0, 1, 16, Cyan);
            drivePad.gameObject.AddComponent<ChefStick>().Bind(game, knob);
            useButton = Button(stage, "ChefAction", 1475, 83, 142, 93, C("337267"));
            useTarget = Label(useButton.transform, "ACÉRCATE", -69, 20, 138, 26, 14, Cyan, FontStyles.Bold);
            useCaption = Label(useButton.transform, "ACCIÓN", -69, -9, 138, 40, 20, Color.white, FontStyles.Bold);
            useButton.onClick.AddListener(game.UseNearbyStation);
            drivePad.SetSiblingIndex(borrowButton.transform.GetSiblingIndex() + 1);
            useButton.transform.SetSiblingIndex(drivePad.GetSiblingIndex() + 1);
        }
        KitchenModelGraphic MountCharacter(RectTransform root, string mesh, Vector2 size, float height)
        {
            var image = root.Find("IllustratedCharacter")?.GetComponent<UnityEngine.UI.Image>();
            if (!image) return null;
            var model = KitchenModelGraphic.Attach(image.rectTransform, mesh);
            if (!model) return null;
            image.enabled = false;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = Vector2.up * height;
            return model;
        }

        static string ActionCaption(KitchenGame g)
        {
            switch (g.NearbyStation)
            {
                case Station.Supply:
                case Station.Roots: return "TOMAR";
                case Station.Cut:
                    if (g.IsWorking) return "ESPERAR";
                    if (g.PrepFood != Food.Empty) return "RECOGER";
                    return g.Carry == Food.RawDodo || g.Carry == Food.Root || g.Carry == Food.Roots ? "CORTAR" : "DEJAR";
                case Station.Stove:
                    if (g.StoveFood == Food.Empty) return "COCINAR";
                    return g.StoveFood == Food.ChoppedDodo || g.StoveFood == Food.Roots ? "ESPERAR" : "RETIRAR";
                case Station.Assemble: return "MONTAR";
                case Station.Trash: return "TIRAR";
                case null: return "ACCIÓN";
                default: return "ENTREGAR";
            }
        }
        void RefreshCastControls(KitchenGame g)
        {
            bool live = g.Running && !g.Paused;
            drivePad.gameObject.SetActive(live);
            useButton.gameObject.SetActive(live);
            if (!live) return;
            useButton.interactable = g.NearbyStation.HasValue;
            useTarget.text = g.NearbyStation.HasValue ? StationCaptions[(int)g.NearbyStation.Value] : "ACÉRCATE";
            useCaption.text = ActionCaption(g);
            if (g.NearbyStation.HasValue && !g.TutorialActive && !g.HasDestination)
                stations[(int)g.NearbyStation.Value].image.color = new Color(.36f,1,.84f,.14f);
            float dt = Time.deltaTime;
            if (g.Travelled < lastDistance) { gait = 0; angle = 0; }
            float distance = Mathf.Max(0, g.Travelled - lastDistance);
            lastDistance = g.Travelled;
            castClock += dt;
            float targetAngle = Mathf.Atan2(g.Facing.x, -g.Facing.y) * Mathf.Rad2Deg;
            angle = Mathf.MoveTowardsAngle(angle, targetAngle, dt * 600);
            gait = Mathf.MoveTowards(gait, Mathf.Clamp01(distance / Mathf.Max(.001f, 300*dt)), dt * 8);
            carryBlend = Mathf.MoveTowards(carryBlend, g.Carry != Food.Empty ? 1 : 0, dt * 7);
            if (ares && castClock >= nextChefFrame)
            {
                nextChefFrame = castClock + 1f/30;
                float phase = g.Travelled * .053f;
                float swing = Mathf.Sin(phase) * gait;
                float left = Mathf.Max(0, Mathf.Cos(phase)) * gait, right = Mathf.Max(0, -Mathf.Cos(phase)) * gait;
                float reach = Mathf.Sin(Mathf.PI * Mathf.Clamp01(g.GestureRemaining / .46f));
                bool chopping = g.TaskLabel == "CORTANDO";
                bool tending = !g.IsMoving && g.NearbyStation == Station.Stove && g.StoveFood != Food.Empty && g.Carry == Food.Empty;
                float chop = chopping ? .5f + .5f*Mathf.Sin(castClock * 17) : 0;
                float breath = ReducedMotion ? 0 : Mathf.Sin(castClock * 2.5f);
                var pose = new KitchenModelGraphic.CharacterPose {
                    leftLeg = swing * 32, rightLeg = -swing * 32,
                    leftKnee = left * 36, rightKnee = right * 36,
                    leftArm = Mathf.Lerp(-swing*31, -36, carryBlend) - reach*33,
                    rightArm = Mathf.Lerp(swing*31, -36, carryBlend) - reach*43 - chop*47,
                    leftElbow = -13 - carryBlend*38 - reach*15,
                    rightElbow = -13 - carryBlend*38 - (chopping ? 38-chop*26 : 0),
                    nod = breath*2 + reach*7 + chop*7, lean = gait*6 + reach*6,
                    twist = swing * (ReducedMotion ? 0 : 5),
                    bounce = ReducedMotion ? 0 : .018f*breath + Mathf.Abs(swing)*.095f
                };
                if (g.ServingGesture) pose.leftElbow -= reach*22;
                if (tending)
                {
                    pose.rightArm -= 32 + Mathf.Sin(castClock*5)*10;
                    pose.rightElbow -= 29 + Mathf.Cos(castClock*5)*12;
                    pose.twist += Mathf.Sin(castClock*5)*5;
                }
                pose.gather = carryBlend*17 + reach*6;
                pose.tool = chopping ? KitchenModelGraphic.HandTool.Knife : tending ? KitchenModelGraphic.HandTool.Ladle : KitchenModelGraphic.HandTool.None;
                ares.Pose(angle); ares.Articulate(pose);
                chef.localScale = Vector3.one; chef.localEulerAngles = Vector3.zero;
                carriedIcon.localScale = Vector3.one * .80f;
                float heading = angle * Mathf.Deg2Rad;
                if (g.Carry != Food.Empty)
                    carriedIcon.anchoredPosition = chef.InverseTransformPoint(ares.rectTransform.TransformPoint(ares.GripCenter()));
                // Let the torso occlude the platter when the chef turns his back.
                var body = ares.transform.parent;
                if (Mathf.Cos(heading) < -.25f && carriedIcon.GetSiblingIndex() > body.GetSiblingIndex())
                    carriedIcon.SetSiblingIndex(body.GetSiblingIndex());
                else if (Mathf.Cos(heading) >= -.25f && carriedIcon.GetSiblingIndex() < body.GetSiblingIndex())
                    carriedIcon.SetSiblingIndex(body.GetSiblingIndex());
            }
            if (castClock < nextVisitorFrame) return;
            nextVisitorFrame = castClock + 1f/16;
            if (echo && g.CloneRemaining > 0)
            {
                echo.Pose(-angle*.6f);
                echo.Articulate(new KitchenModelGraphic.CharacterPose { leftArm = -39, rightArm = -20, rightElbow = -55, nod = ReducedMotion ? 0 : Mathf.Sin(castClock*5)*9 });
            }
            for (int i = 0; i < 3; i++) PoseVisitor(i,g);
        }

        void PoseVisitor(int slot, KitchenGame g)
        {
            int kind = g.Species[slot];
            var model = visitors[slot,kind];
            if (!model || !g.Customers[slot]) return;
            bool impatient = g.Patience[slot] < 20;
            if (kind == 1) model.Show(impatient ? "BipAlarm" : "BipIdle");
            float urgency = impatient ? 1.8f : 1;
            float t = (castClock + slot*1.73f) * urgency;
            float gesture = Mathf.SmoothStep(0,1, Mathf.Clamp01((Mathf.Sin(t*1.35f)-.24f)*1.8f));
            float wave = Mathf.Sin(t * (kind == 1 ? 7 : 3.6f));
            float look = Mathf.Clamp((g.ChefPosition.x - 630 - slot*270)*.052f,-27,27);
            model.Pose(kind == 3 ? -14 : kind == 2 ? 11 : -5);
            model.Look(look + (ReducedMotion ? 0 : wave*4));
            var pose = new KitchenModelGraphic.CharacterPose();
            if (!ReducedMotion)
            {
                switch (kind)
                {
                    case 0: // Grog winds up and thumps the counter.
                        float thump = Mathf.Pow(.5f+.5f*wave,2)*gesture;
                        pose.rightArm = -18-thump*61; pose.rightElbow = -23-thump*24;
                        pose.nod = thump*12; pose.twist = thump*9; break;
                    case 1: // BIP ticks in short, mechanical increments.
                        pose.leftArm = -12-gesture*45; pose.rightArm = -22+Mathf.Round(wave*3)*4;
                        pose.nod = Mathf.Round(wave*2)*2; break;
                    case 2: // Zorg raises a tasting hand and appraises the aroma.
                        pose.leftArm = -21-gesture*46; pose.leftElbow = -18-gesture*52;
                        pose.nod = -gesture*11; pose.twist = wave*6; break;
                    case 3: // Rex sniffs in pairs, little arms paddling below his muzzle.
                        pose.lean = -gesture*12; pose.nod = Mathf.Abs(wave)*gesture*14;
                        pose.leftArm = -35+wave*12; pose.rightArm = -35-wave*12; break;
                }
                pose.bounce = Mathf.Sin(t*2.2f)*.027f;
            }
            model.Articulate(pose);
        }
    }
}
