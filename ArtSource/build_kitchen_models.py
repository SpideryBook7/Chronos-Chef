"""Run in Blender background mode with --python to rebuild the game's mesh library."""
import bpy, math, os, sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
bpy.ops.wm.read_factory_settings(use_empty=True)
palette = {}
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from export_kitchen_meshes import export_library
from kitchen_cast import sculpt, rig_character

def material(hex_color):
    if hex_color not in palette:
        rgb = tuple(int(hex_color[i:i+2], 16)/255 for i in (0,2,4))
        mat = bpy.data.materials.new(hex_color)
        mat.diffuse_color = (*rgb, 1)
        palette[hex_color] = mat
    return palette[hex_color]

def paint(obj, color):
    obj.data.materials.append(material(color))
    for p in obj.data.polygons: p.use_smooth = True
    return obj

def oval(name, pos, scale, color, seg=12, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    return paint(obj, color)

def lathe(name, profile, color, wave=0, n=20):
    verts=[]; faces=[]
    for j,(rad,z) in enumerate(profile):
        for i in range(n):
            a=math.tau*i/n
            r=rad*(1+.045*math.sin(3*a)+.024*math.sin(5*a))
            verts.append((r*math.cos(a),r*math.sin(a)*.87,z+wave*rad*math.sin(5*a+j*.3)))
    for j in range(len(profile)-1):
        for i in range(n):
            a=j*n+i; b=j*n+(i+1)%n
            faces.append((a,b,b+n,a+n))
    faces += [tuple(reversed(range(n))),tuple(range(len(verts)-n,len(verts)))]
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(obj)
    return paint(obj,color)

def cube(name,pos,scale,color,angle=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    obj=bpy.context.object; obj.name=name
    for v in obj.data.vertices:
        for i,s in enumerate(scale): v.co[i]*=s
    obj.rotation_euler.z=angle
    bev=obj.modifiers.new("Rounded cut edges",'BEVEL'); bev.width=min(scale)*.17; bev.segments=2
    bpy.context.view_layer.objects.active=obj; bpy.ops.object.modifier_apply(modifier=bev.name)
    paint(obj,color)
    for p in obj.data.polygons: p.use_smooth=False
    return obj

def bun(z=0,top=True):
    if not top:
        lathe("Golden heel",[(.80,z),(.95,z+.07),(.97,z+.19),(.90,z+.22)],"ca752d")
        lathe("Soft crumb",[(.91,z+.195),(.89,z+.24)],"ffcf7e")
        return
    lathe("Glazed brioche",[(.94,z),(.99,z+.07),(.94,z+.22),(.78,z+.40),(.50,z+.51),(.10,z+.56)],"ed9b3d")
    for j in range(13):
        ang=j*2.4; r=.74*math.sqrt((j+.5)/13)
        seed=oval("Sesame",(r*math.cos(ang),.87*r*math.sin(ang),z+.57-.29*r*r),(.078,.035,.024),"fff0b9",8,4)
        seed.rotation_euler.z=ang

def patty(z=0):
    lathe("Seared patty",[(.74,z),(.93,z+.06),(.94,z+.22),(.78,z+.30)],"945139",.045)
    for i in range(-2,3):
        line=oval("Flame mark",(i*.26,0,z+.30),(.035,.62-abs(i)*.08,.016),"4b292b",8,4)
        line.rotation_euler.z=-.3

def cubes(color,highlight,z=0):
    for j,(x,y,a) in enumerate([(-.43,-.26,.20),(.14,-.39,-.28),(.51,.04,.32),(-.13,.16,.1),(-.45,.33,-.2),(.16,.04,.4)]):
        cube("Knife cut",(x,y,z+(.38 if j==5 else .17)),(.43,.39,.36),highlight if j%3==0 else color,a)

def root(x=0,y=0,scale=1):
    start=set(bpy.data.objects)
    lathe("Neon radish",[(.025,-.40),(.15,-.17),(.48,.05),(.55,.34),(.41,.59),(.13,.68)],"ad41bf")
    for i in range(4):
        a=i*1.57
        leaf=oval("Radish leaf",(.22*math.cos(a),.22*math.sin(a),.82),(.15,.09,.38),"63c75b" if i%2 else "338652",10,5)
        leaf.rotation_euler=(.8*math.sin(a),.8*math.cos(a),a)
    for obj in set(bpy.data.objects)-start:
        obj.location=Vector((x,y,0))+obj.location*scale; obj.scale*=scale

def eye(x,y,z):
    oval("Eye white",(x,y,z),(.16,.15,.17),"ffedbd")
    oval("Pupil",(x+.02,y-.13,z+.01),(.073,.037,.085),"292440",10,5)
    oval("Catchlight",(x+.038,y-.155,z+.05),(.023,.014,.025),"ffffff",8,4)

def bip(alarm):
    for side in (-1,1):
        x=side*.37
        oval("Magnetic boot",(x,-.09,.18),(.29,.34,.20),"2e3556")
        oval("Hydraulic knee",(x,0,.46),(.17,.16,.26),"865987")
    cube("Chest casting",(0,0,.94),(1.16,.71,.91),"526487")
    cube("Apron plate",(0,-.35,.96),(.88,.12,.64),"b3b4c8")
    cube("Meter recess",(0,-.425,1.04),(.63,.05,.23),"283353")
    for j in range(4):
        cube("Charge light",(-.22+j*.145,-.462,1.04),(.095,.025,.09),"ffc368" if j==3 else "4ee9dc")
    for side in (-1,1):
        x=side*.69; z=1.04 if not alarm else 1.29
        joint = "ArmL|" if side < 0 else "ArmR|"
        oval(joint+"Shoulder joint",(x,0,1.15),(.22,.25,.21),"443858")
        oval(joint+"Elbow housing",(x+side*.10,-.08,z-.12),(.19,.21,.22),"bc86ac")
        oval(joint+"Magnetic mitt",(x+side*.16,-.20,z-.26 if not alarm else z+.13),(.23,.22,.20),"82799c")
    # The independently named head parts can turn as one around the neck at runtime.
    cube("Head|Porcelain housing",(0,0,1.94),(1.47,.86,.90),"bdb2c3")
    cube("Head|Violet gasket",(0,-.408,1.93),(1.34,.17,.74),"8c628b")
    cube("Head|Curved visor",(0,-.497,1.95),(1.19,.14,.58),"182d45")
    for side in (-1,1):
        cube("Head|LED eye",(side*.30,-.577,2.00),(.20,.025,.078 if not alarm else .16),"6dffee" if not alarm else "ffb55b").rotation_euler.y=side*(.22 if not alarm else -.34)
        oval("Head|Side hinge",(side*.74,0,1.94),(.13,.20,.22),"766687")
        oval("Head|Cyan cap",(side*.84,0,1.94),(.052,.13,.13),"6cead7")
    cube("Head|Grin",(0,-.578,1.82),(.29,.025,.052),"75e8de")
    cube("Head|Aerial",(0,0,2.52),(.062,.062,.36),"867b9e")
    oval("Head|Beacon",(0,0,2.71),(.12,.12,.12),"ffac52" if alarm else "59efd4")

def make(name):
    if name.startswith("Bip"): bip(name=="BipAlarm")
    elif name in ("Ares","Grog","Zorg","Rex"): sculpt(name,oval,cube)
    elif name=="Bread": bun(); bun(-.37,False)
    elif name=="Burger":
        bun(0,False); patty(.18)
        lathe("Cosmic sauce",[(.85,.39),(.96,.44),(.87,.50)],"a642bf",.07)
        lathe("Ruffled lettuce",[(.73,.47),(1.04,.50),(.90,.56)],"70c843",.115,40)
        bun(.53)
    elif name=="CookedDodo": patty()
    elif name=="RawDodo":
        oval("Fat rim",(0,0,.12),(.98,.65,.23),"ffae8c",20,8)
        oval("Marbled meat",(-.04,-.015,.22),(.90,.57,.18),"c64557",20,8)
        oval("Bone center",(.38,.09,.36),(.20,.14,.04),"ffe3ba")
        for i in range(3):
            oval("Marbling",(-.48+i*.23,-.12,.37),(.037,.21,.016),"fb8787",8,4).rotation_euler.z=.63
    elif name=="ChoppedDodo": cubes("be4553","ea6a64")
    elif name=="Root": root()
    elif name=="Roots": root(-.42,-.10,.9); root(.40,.14,.86)
    elif name in ("ChoppedRoot","ChoppedRoots"):
        if name=="ChoppedRoots": cubes("b84ac2","e76bd4")
        else:
            for j in range(3):
                for r,dy,col in [(.48,0,"ae3dbe"),(.425,-.023,"fa8adb")]:
                    oval("Cut radish",(-.35+j*.34,-.14*j+dy,.15+j*.06),(r,.11,r*.72),col)
    elif name=="Toast":
        bun(0,False); cubes("af32ba","f178d0",.24)
        for j in range(5):
            a=j*1.5
            oval("Greens",(.61*math.cos(a),.49*math.sin(a),.37),(.24,.11,.09),"6cb54a").rotation_euler.z=a
        cube("Cocktail pick",(.26,.12,.71),(.045,.045,.82),"e6b968")
        oval("Suspicious olive",(.26,.12,.98),(.23,.21,.22),"83ad43");eye(.25,-.04,1.01)
    elif name=="Soup":
        lathe("Enamel bowl",[(.42,0),(.60,.07),(.90,.46),(.96,.60),(.94,.69),(.84,.67)],"357a88")
        lathe("Neon rim",[(.945,.57),(.963,.60),(.955,.64)],"8de4d1")
        oval("Broth",(0,0,.665),(.85,.74,.048),"71b641",20)
        for i in range(7):
            a=i*2.4; r=.6*math.sqrt((i+.5)/7)
            cube("Garnish",(r*math.cos(a),r*.87*math.sin(a),.706),(.17,.14,.09),"bc65c1" if i%2 else "d1e276",a)
    elif name=="Burnt":
        lathe("Charcoal",[(.63,0),(.90,.07),(.79,.29),(.44,.33)],"4c3b48",.06)
        eye(-.27,-.47,.28);eye(.20,-.50,.29)

for idx,name in enumerate(("RawDodo","ChoppedDodo","CookedDodo","Burger","Burnt","Root","Roots","Soup","Bread","ChoppedRoot","ChoppedRoots","Toast","BipIdle","BipAlarm","Ares","Grog","Zorg","Rex")):
    old=set(bpy.data.objects); make(name); objects=set(bpy.data.objects)-old
    if name in ("BipIdle","BipAlarm","Ares","Grog","Zorg","Rex"):
        rig_character(name,objects)
        objects=set(bpy.data.objects)-old
    collection=bpy.data.collections.new(name); bpy.context.scene.collection.children.link(collection)
    collection["chronos_origin"] = [(idx%4)*3.3,(idx//4)*3.3,0.]
    for obj in objects:
        for c in list(obj.users_collection): c.objects.unlink(obj)
        collection.objects.link(obj); obj.location.x+=(idx%4)*3.3; obj.location.y+=(idx//4)*3.3
dest=os.path.join(ROOT,"Assets","ChronosChef","Resources","KitchenModels.bytes")
bpy.context.view_layer.update()
export_library(dest)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,"ArtSource","KitchenModels.blend"))
