"""Rebuild the playable orbital galley. Coordinates are in metres, Z up."""
import bpy
import math
import random
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
rng = random.Random(421)
pieces = []
palette = {}
navy, steel, alloy = "263448","465C70","8AA5A8"
mint, amber, copper = "65E6CE","F7BA5B","BD7552"
def finish(obj,name,colour):
    obj.name=name
    if colour not in palette:
        mat=bpy.data.materials.new(colour)
        mat.diffuse_color=tuple(int(colour[i:i+2],16)/255 for i in (0,2,4))+(1,)
        palette[colour]=mat
    obj.data.materials.append(palette[colour])
    pieces.append((obj,colour))
    return obj

def box(name,x,y,z,w,d,h,col,bevel=.06):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(x,y,z))
    obj=bpy.context.object
    for v in obj.data.vertices:
        v.co.x*=w; v.co.y*=d; v.co.z*=h
    if bevel:
        mod=obj.modifiers.new("Rounded seams","BEVEL")
        mod.width=min(bevel,min(w,d,h)*.35); mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj,name,col)

def cyl(name,x,y,z,rx,ry,height,col):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=1,depth=1,location=(x,y,z))
    obj=bpy.context.object;obj.scale=(rx,ry,height)
    return finish(obj,name,col)

def orb(name,x,y,z,rx,ry,rz,col):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=14,ring_count=8,location=(x,y,z))
    obj=bpy.context.object;obj.scale=(rx,ry,rz)
    for p in obj.data.polygons:p.use_smooth=True
    return finish(obj,name,col)

# A tiled service deck with a recessed perimeter and two illuminated rails.
box("Deck foundation",0,-.85,-.25,18,15,.50,navy)
for ix in range(12):
    for iy in range(9):
        box("Ceramic tile",(ix-5.5)*1.46,(iy-4)*1.54-.8,-.025,1.445,1.52,.10,
            rng.choice(["505C71","536579","617183","526474"]),.028)
for x in (-5.27,5.27):
    box("Inset runway",x,-1.15,.035,.12,9.7,.04,navy)
    box("Luminous inlay",x,-1.15,.065,.038,9.65,.025,mint)
box("Forward rail",0,-6,.055,10.6,.07,.036,mint)
for x in (-3.6,2.9):
    for j in range(8):
        box("Drain slot",x+j*.065,-2.7,.038,.03,.32,.018,navy,.008)

def bench(name,x,y,w,d,h,accent):
    box(name+" contact shadow",x+.065,y+.065,.042,w+.18,d+.18,.016,"424B58",.015)
    box(name+" plinth",x,y,.105,w*.92,d*.91,.21,"192737")
    box(name+" enamel",x,y,h*.49,w,d,h*.82,steel,.11)
    box(name+" fascia",x,y-d*.503,h*.51,w*.85,.055,h*.49,navy)
    for j in range(5):
        box("Vent",x-w*.28+j*.105,y-d*.529,h*.49,.052,.04,h*.27,"556879",.015)
    box("Status",x+w*.30,y-d*.53,h*.5,w*.12,.036,.05,accent)
    box(name+" rim",x,y,h-.065,w+.07,d+.07,.15,accent,.055)
    box(name+" worktop",x,y,h+.025,w+.14,d+.10,.14,alloy,.08)
    return h+.095

# Coordinates map onto the same walkable footprint as the timed service.
cut=bench("Laser prep",-1.30,.02,1.92,1.58,.65,mint)
box("Knife board",-1.3,.02,cut+.03,1.52,1.12,.07,"C99565")
for j in range(5):
    box("End grain",-1.88+j*.28,.02,cut+.07,.016,1.0,.012,"9F724F")
blade=box("Cleaver",-1.26,-.10,cut+.17,.56,.07,.26,"D9E5DD")
handle=box("Cleaver grip",-.86,-.10,cut+.25,.28,.10,.10,navy)
stove=bench("Induction reactor",1.45,.03,1.88,1.62,.64,copper)
for r,h,col in [(.71,.10,navy),(.58,.08,copper),(.48,.065,"324653"),(.36,.04,"5EE1AF")]:
    cyl("Burner",1.45,.03,stove+h/2,r,r,h,col);stove+=h
for k in range(4):
    a=k*math.pi/2
    block=box("Pan support",1.45+math.cos(a)*.52,.03+math.sin(a)*.52,stove,.29,.085,.09,navy)
    block.rotation_euler.z=a
for x,y,label,accent in [(-6.15,-3.90,"Dodo","E98078"),(-5.93,-.50,"Roots",mint)]:
    top=bench(label+" chiller",x,y,1.96,1.75,.58,accent)
    box("Cold well",x,y,top+.03,1.63,1.38,.075,navy)
    for j in range(4):
        xx=x-.43+(j%2)*.72; yy=y-.34+(j//2)*.60
        if label=="Dodo":
            orb("Cutlet",xx,yy,top+.13,.43,.27,.12,"C8656B")
            orb("Bone",xx+.14,yy,top+.23,.08,.065,.025,"FADAB0")
        else:
            orb("Radish",xx,yy,top+.19,.23,.22,.25,"B465AC")
            for side in (-1,1):
                leaf=orb("Leaf",xx+side*.09,yy,top+.44,.08,.11,.18,"70AE70");leaf.rotation_euler.y=side*.45
top=bench("Bakery",6.43,-1.27,2.05,2.15,.70,amber)
for j in range(3):
    orb("Brioche",6.0+j*.38,-1.22+j%2*.37,top+.19,.39,.32,.24,"E7A754")
    for k in range(3): orb("Sesame",5.90+j*.38+k*.065,-1.22+j%2*.37,top+.41,.033,.018,.018,"F7D797")
binTop=bench("Reclaimer",-6.52,-6.28,1.64,1.65,.54,mint)
cyl("Waste chute",-6.52,-6.28,binTop+.026,.62,.65,.07,navy)
top=bench("Cashier",6.5,-5.92,1.94,1.55,.63,copper)
box("Receipt terminal",6.5,-5.92,top+.20,.90,.55,.40,navy)
box("Display",6.5,-6.20,top+.22,.66,.04,.22,mint)

# Long pass, inset drawer fronts, fasteners and an overhead brass rail.
top=bench("Service pass",.35,2.07,11.3,1.72,.85,copper)
for i in range(8):
    x=-4.45+i*1.37
    box("Drawer front",x,1.195,.47,1.28,.06,.53,"46525F")
    box("Brass pull",x,1.15,.61,.42,.055,.049,"DDA96D")
for x in (-4.7,5.33):
    for j in range(3):
        cyl("Condiment",x+j*.15,2.27,top+.12,.064,.064,.24,["DF835D","96AD76","D8AE73"][j])
        cyl("Bottle cap",x+j*.15,2.27,top+.26,.041,.041,.054,navy)

# Observation windows and ribbed hull panels behind the passengers.
box("Aft bulkhead",0,6.65,2.0,18.2,.45,4.8,"3D435B")
for j in range(5):
    x=-6.9+j*3.45
    box("Window bezel",x,6.36,1.92,3.16,.20,2.38,copper,.20)
    box("Window gasket",x,6.22,1.92,3.00,.12,2.21,navy,.16)
    box("Night sky",x,6.14,1.92,2.82,.08,2.05,"18263D",.15)
    for k in range(13):
        size=rng.uniform(.014,.035)
        orb("Star",x+rng.uniform(-1.28,1.28),6.085,1.92+rng.uniform(-.88,.88),size,.009,size,"AFD7D2")
    box("Frame lamp",x,6.16,3.32,1.35,.16,.07,mint)
# A ringed moon and a small satellite give the viewport a focal point.
orb("Ochre moon",-3.65,5.96,1.69,.49,.09,.49,"D59870")
ring=orb("Orbital ring",-3.65,5.84,1.66,.70,.038,.077,"E9C589")
ring.rotation_euler.y=-.34
orb("Ice moon",2.53,5.99,2.45,.22,.06,.22,"82C8CE")
for i in range(6):
    x=-4.45+i*1.76
    box("Service inset",x,1.39,top+.009,1.43,.21,.026,"485B68")
    for k in range(6):
        box("Warming slot",x-.49+k*.19,1.39,top+.026,.11,.09,.016,"253D49")
for side in (-1,1):
    box("Hull",side*8.10,.7,.83,.44,12.0,1.85,"42495F")
    box("Wall trim",side*7.85,.7,1.65,.10,11.9,.12,copper)
    for j in range(6):
        box("Rib",side*7.84,-4+j*1.86,.88,.20,.14,1.70,steel)
for j in range(3):
    pipe=cyl("Heat exchanger",-7.23+j*.27,5.71,1.18,.10,.10,2.36,copper)
    for z in (.34,1.55):cyl("Pipe collar",pipe.location.x,5.71,z,.14,.14,.09,navy)

# Pressure shell, maintenance galleries and illuminated vaulted ceiling.
box("Overhead shell",0,-2,6.2,18,22,.28,"39374D")
box("Aft crown",0,6.65,5.25,18.2,.48,2.2,"474157")
box("Forward apron",0,-9.6,-.24,19,6,.4,steel)
for j in range(13):
    box("Apron tread",0,-7.3-j*.38,-.035,18,.06,.018,"354452")
for side in (-1,1):
    box("Pressure liner",side*8.36,-2,3.1,.38,19,6.4,"49465E")
    for j in range(7):
        y=5-j*2.35
        box("Recess",side*8.08,y,3.25,.18,2.06,2.65,navy)
        box("Inspection lamp",side*7.96,y,4.28,.09,1.56,.075,mint)
        box("Copper conduit",side*7.94,y,2.13,.14,2.35,.1,copper)
for j in range(6):
    y=5-j*3.05
    box("Ceiling spar",0,y,5.98,16.6,.31,.33,steel)
    for side in (-1,1):
        box("Galley strip",side*3.8,y,5.78,3.1,.22,.09,"FFE0A4")
for x in (-2.4,-2.05,2.05,2.4):
    box("Thermal trunk",x,-2,5.91,.19,18,.23,copper)
for j in range(13):
    x=-7.8+j*1.3
    box("Aft grille",x,6.31,4.58,1.14,.16,.63,navy)
    for k in range(3): box("Louvre",x,6.19,4.39+k*.16,.95,.08,.045,steel)

# Bake bevel normals and palette to one indexed, vertex-coloured runtime mesh.
bpy.context.view_layer.update()
verts={}; triangles=[]; data=bytearray()
for obj,hexcol in pieces:
    mesh=obj.data;mesh.calc_loop_triangles()
    matrix=obj.matrix_world; normalMatrix=matrix.to_3x3().inverted().transposed()
    rgb=bytes.fromhex(hexcol)
    for tri in mesh.loop_triangles:
        for i in reversed(tri.vertices):
            v=mesh.vertices[i]; p=matrix@v.co; n=(normalMatrix@(v.normal if tri.use_smooth else tri.normal)).normalized()
            key=struct.pack("<6f",p.x,p.z,p.y,n.x,n.z,n.y)+rgb
            if key not in verts:
                verts[key]=len(verts);data.extend(key)
            triangles.append(verts[key])
dst=root/"Assets/ChronosChef/Resources/OrbitalGalley.bytes"
dst.write_bytes(struct.pack("<4sII",b"GAL1",len(verts),len(triangles))+data+struct.pack(f"<{len(triangles)}I",*triangles))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(root/"ArtSource/OrbitalGalley.blend"))
print("Galley:",len(verts),"vertices,",len(triangles)//3,"triangles")
