"""Segmented character sculpts in Blender space. All costumes use the same bind pose."""
import math
import bpy
from mathutils import Vector

# End joints follow their upper limb in the Unity pose solver.
JOINTS = {
    "Body": (0, 0, .86), "Head": (0, 0, 1.69),
    "ArmL": (-.62, 0, 1.45), "ArmR": (.62, 0, 1.45),
    "LegL": (-.28, 0, .84), "LegR": (.28, 0, .84),
    "ForeL": (-.70, -.04, 1.08), "ForeR": (.70, -.04, 1.08),
    "ShinL": (-.28, 0, .46), "ShinR": (.28, 0, .46),
    "Knife": (.72,-.13,.86), "Ladle": (.72,-.13,.86),
}

def rig_character(name, parts):
    """Give each rigid costume piece a weighted, editable Blender bone."""
    data = bpy.data.armatures.new(name + " Skeleton")
    rig = bpy.data.objects.new(name + " Rig", data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    master = data.edit_bones.new("Master")
    master.head, master.tail = (0,0,0), (0,0,.26)
    parents = {"Head":"Body", "ArmL":"Body", "ArmR":"Body",
               "ForeL":"ArmL","ForeR":"ArmR","ShinL":"LegL","ShinR":"LegR",
               "Knife":"ForeR","Ladle":"ForeR"}
    for joint, pivot in JOINTS.items():
        bone = data.edit_bones.new(joint)
        bone.head = pivot
        upwards = joint in ("Body","Head")
        bone.tail = Vector(pivot) + Vector((0,0,.38 if upwards else -.23))
        bone.parent = data.edit_bones[parents.get(joint,"Master")]
    bpy.ops.object.mode_set(mode="OBJECT")
    data.display_type = "STICK"
    for part in parts:
        joint = part.name.partition("|")[0]
        group = part.vertex_groups.new(name=joint if joint in JOINTS else "Body")
        group.add(list(range(len(part.data.vertices))),1.0,"REPLACE")
        modifier = part.modifiers.new("Costume rig","ARMATURE")
        modifier.object = rig
        if joint in ("Knife","Ladle"): part.hide_set(True)


def sculpt(name, oval, cube):
    chef = name == "Ares"
    grog = name == "Grog"
    rex = name == "Rex"
    skin, shadow, coat, trim = {
        "Ares": ("ecaa80", "bc715e", "39566e", "4ce4cb"),
        "Grog": ("ce895c", "824d3d", "a65738", "e8ad58"),
        "Zorg": ("99d96c", "56875b", "8957a6", "f1bd67"),
        "Rex": ("6bb995", "3d7665", "344768", "fb9663"),
    }[name]

    def ball(joint, part, xyz, dimensions, col, tilt=0):
        obj = oval(joint+"|"+part, xyz, dimensions, col, 10, 6)
        obj.rotation_euler.y = tilt
        return obj

    def plate(joint, part, xyz, dimensions, col, tilt=0):
        obj = cube(joint+"|"+part, xyz, dimensions, col)
        obj.rotation_euler.y = tilt
        return obj

    navy, sole, ivory = "293149", "47374a", "fff0ce"
    for side, sign in (("L", -1), ("R", 1)):
        x = sign*.28
        ball("Leg"+side, "Trousers", (x,0,.68), (.24,.24,.32), navy)
        ball("Shin"+side, "Gaiter", (x,0,.40), (.20,.20,.22), coat)
        ball("Shin"+side, "Boot", (x,-.09,.20), (.23,.33,.19), sole)
        plate("Shin"+side, "Tread", (x,-.09,.09), (.41,.52,.10), navy)
        ball("Arm"+side, "Sleeve", (sign*.60,0,1.34), (.21,.24,.28), coat)
        ball("Arm"+side, "Cuff", (sign*.69,-.035,1.12), (.19,.20,.12), trim)
        ball("Fore"+side, "Forearm", (sign*.71,-.07,1.02), (.155,.17,.18), skin)
        ball("Fore"+side, "Mitten", (sign*.72,-.13,.86), (.18,.20,.19), skin)
        ball("Fore"+side, "Thumb", (sign*.59,-.20,.90), (.085,.09,.12), skin)

    ball("Body","Jacket",(0,0,1.22),(.61,.40,.53),coat)
    ball("Body","Apron",(0,-.31,1.19),(.47,.20,.44), "e2ddc7" if chef else coat)
    ball("Body","Belt",(0,-.01,.94),(.55,.40,.12),navy)
    plate("Body","Buckle",(0,-.40,.95),(.22,.08,.14),trim)
    ball("Body","Neckerchief",(0,-.02,1.61),(.43,.37,.16),trim)
    ball("Body","Scarf knot",(.23,-.34,1.48),(.11,.10,.21),trim,-.36)
    ball("Head","Face",(0,0,2.05),(.63 if grog else .55,.46,.54),skin)
    for sign in (-1,1):
        ball("Head","Ear",(sign*.54,-.015,2.04),(.14,.14,.18),skin)
        ball("Head","Ear inset",(sign*.608,-.09,2.04),(.065,.06,.093),shadow)

    if chef or grog:
        hair = "49303b" if chef else "653e39"
        ball("Head","Hair cap",(0,.055,2.37),(.57,.45,.30),hair)
        for i in range(5):
            x = (i-2)*.19
            ball("Head","Swept fringe",(x,-.31,2.46-.05*abs(i-2)),(.16,.19,.19),hair,.44)
        if chef:
            # Padded headband and brass-rimmed time goggles sit above his eyes.
            ball("Head","Goggle strap",(0,0,2.35),(.58,.47,.095),navy)
            for sign in (-1,1):
                x=sign*.245
                ball("Head","Goggle bezel",(x,-.432,2.39),(.235,.081,.155),"bc8553")
                ball("Head","Glass",(x,-.49,2.408),(.186,.058,.117),"36a9b4")
                ball("Head","Glass glint",(x-.05,-.535,2.445),(.065,.018,.031),"a7f7de")
            # A tilted baker's toque has a pleated crown, a folded band and a teal pin.
            plate("Head","Hat band",(-.08,.06,2.57),(.78,.65,.22),"d9dbcc",-.12)
            ball("Head","Toque crown",(-.10,.09,2.74),(.53,.39,.24),"fff2d6",-.15)
            for i in range(5):
                a = i*math.tau/5
                ball("Head","Toque pleat",(-.10+.33*math.cos(a),.06+.20*math.sin(a),2.72-.045*math.cos(a)),(.218,.213,.185),"fff1d8")
            ball("Head","Time pin",(.19,-.29,2.57),(.067,.028,.062),trim)
            for sign in (-1,1):
                plate("Body","Apron strap",(sign*.29,-.325,1.42),(.095,.10,.34),"a0714f",sign*.17)
                ball("Body","Rivet",(sign*.29,-.39,1.31),(.045,.03,.045),trim)
            plate("Body","Backpack",(0,.35,1.30),(.66,.35,.60),navy)
            plate("Body","Apron pocket",(0,-.49,1.15),(.33,.065,.19),"779694")
            plate("Body","Pocket seam",(0,-.532,1.21),(.34,.022,.035),ivory)
            ball("ArmR","Copper pauldron",(.61,-.01,1.44),(.235,.255,.16),"aa7958")
            plate("ArmR","Time gauge",(.65,-.224,1.43),(.14,.047,.09),trim)
            for tool in ("Knife","Ladle"):
                plate(tool,"Grip",(.72,-.22,.85),(.095,.42,.11),navy)
                plate(tool,"Copper collar",(.72,-.42,.85),(.12,.08,.12),"e2ac70")
            plate("Knife","Plasma blade",(.72,-.65,.91),(.065,.48,.25),"a3dbd9")
            plate("Knife","Laser edge",(.72,-.65,.785),(.075,.48,.035),trim)
            plate("Ladle","Stem",(.72,-.60,.85),(.062,.44,.066),"9bacc0")
            ball("Ladle","Copper bowl",(.72,-.88,.84),(.18,.20,.11),"d4966c")
            for sign in (-1,1):
                ball("Body","Time cell",(sign*.21,.54,1.32),(.09,.07,.24),trim)
        else:
            ball("Head","Beard",(0,-.26,1.86),(.58,.34,.34),hair)
            for i in range(5):
                ball("Head","Beard lock",((i-2)*.19,-.42,1.72+.04*abs(i-2)),(.13,.17,.20),hair)
            for side in (-1,1):
                ball("Head","Bone tip",(side*.32,.08,2.72),(.12,.10,.09),ivory)
            plate("Head","Hairpin",(0,.08,2.72),(.66,.09,.095),ivory)
            ball("ArmL","Stone shoulder",(-.61,0,1.46),(.27,.26,.20),"717079")
            for i in range(3):
                ball("Body","Pelt spots",((i-1)*.20,-.49,1.20+(.09 if i==1 else 0)),(.065,.038,.095),"613f3d",.4)
    elif rex:
        ball("Head","Muzzle",(0,-.38,1.99),(.55,.49,.29),skin)
        ball("Head","Lower jaw",(0,-.40,1.82),(.46,.38,.12),"dade99")
        for sign in (-1,1):
            ball("Head","Nostril",(sign*.22,-.78,2.06),(.066,.03,.043),shadow)
            ball("Head","Fang",(sign*.31,-.67,1.85),(.054,.05,.105),ivory)
        for i in range(4):
            ball("Head","Crest",(0,-.20+i*.17,2.56-i*.045),(.115,.13,.20),"fbb36a")
    else:
        for sign in (-1,1):
            ball("Head","Feeler",(sign*.30,.04,2.55),(.066,.068,.24),skin,sign*.30)
            ball("Head","Feeler lamp",(sign*.36,.04,2.75),(.12,.115,.12),trim)
        ball("Body","Velvet lapel",(.22,-.39,1.39),(.13,.11,.29),"ae68a4",.48)
        plate("Body","Gold clasp",(0,-.50,1.43),(.18,.06,.16),trim)
    for sign in (-1,1):
        x = sign*(.30 if rex else .235)
        z = 2.22 if rex else 2.15
        ball("Head","Eye",(x,-.409,z),(.143,.083,.14),ivory)
        ball("Head","Iris",(x,-.479,z-.006),(.077,.041,.093),navy)
        ball("Head","Catchlight",(x-.022,-.512,z+.03),(.032,.016,.030),"ffffff")
        ball("Head","Brow",(x,-.40,z+.153),(.156,.076,.043),shadow,sign*-.14)
    if not rex:
        ball("Head","Nose",(0,-.483,2.02),(.16,.14,.12),skin)
        ball("Head","Smile",(0,-(.57 if grog else .405),1.90),(.14,.045,.041),sole,-.09)
