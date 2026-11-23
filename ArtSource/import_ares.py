"""Run with Blender --background --python ArtSource/import_ares.py to rebuild the playable FBX skin."""
import bpy
import struct
import sys
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[1]
output = root / "Assets/ChronosChef/Resources"
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root / "Assets/Ares-personaje.fbx"))
actor, = [o for o in bpy.context.scene.objects if o.type == "MESH"]
image = next(i for i in bpy.data.images if i.name.startswith("texture_pbr_") and i.name.count("_") == 2)
image.scale(1024, 1024)
image.filepath_raw = str(output / "AresColor.png")
image.file_format = "PNG"
# A neutral swatch supplies the solid colours of the existing kitchen utensils.
for row in range(8):
    start = row * 1024 * 4
    image.pixels[start:start+32] = [1.0]*32
image.save()

bpy.context.view_layer.objects.active = actor
reduce = actor.modifiers.new("Gameplay silhouette", "DECIMATE")
reduce.ratio = min(1, 7200 / len(actor.data.polygons))
bpy.ops.object.modifier_apply(modifier=reduce.name)
mesh = actor.data
mesh.calc_loop_triangles()
# Joint order follows KitchenModelGraphic. Pivots and palm sockets are in the imported rest pose.
pivots = [(0,.015,.32),(0,.015,.73),(-.155,.02,.62),(.155,.02,.62),
          (-.09,.015,.30),(.09,.015,.30),(-.225,.005,.515),(.225,.005,.515),
          (-.095,.02,.15),(.095,.02,.15),(-.282,-.018,.41),(.282,-.018,.41)]
scale = 2.6
def smooth(x):
    t = max(0,min(1,x))
    return t*t*(3-2*t)
def weights(p):
    x,y,z = p
    right = int(x>0)
    head = smooth((z-.69)/.07)
    arm = smooth((abs(x)-(.196-(z-.48)*.31))/.035)*smooth((z-.345)/.025)*(1-smooth((z-.615)/.075))
    leg = 1-smooth((z-.27)/.09)
    elbow = 1-smooth((z-.49)/.06)
    knee = 1-smooth((z-.12)/.065)
    limbs = (1-head)*arm
    legs = (1-head-limbs)*leg
    return {0:1-head-limbs-legs,1:head,2+right:limbs*(1-elbow),6+right:limbs*elbow,
            4+right:legs*(1-knee),8+right:legs*knee}

vertices = {}
indices = []
data = bytearray()
groups = {i:actor.vertex_groups.new(name=name) for i,name in enumerate(
    ["Body","Head","ArmL","ArmR","LegL","LegR","ForeL","ForeR","ShinL","ShinR"])}
normals = actor.matrix_world.to_3x3().inverted().transposed()
uvs = mesh.uv_layers.active.data
for tri in mesh.loop_triangles:
    for loop in tri.loops:
        v = mesh.vertices[mesh.loops[loop].vertex_index]
        uv = tuple(uvs[loop].uv)
        key = (v.index, uv)
        if key not in vertices:
            vertices[key] = len(vertices)
            pos = actor.matrix_world @ v.co
            influences = sorted(weights(pos).items(),key=lambda item:-item[1])[:4]
            total = sum(w for j,w in influences)
            for j,w in influences:
                if w > 0: groups[j].add([v.index],w/total,"REPLACE")
            data.extend(struct.pack("<8f4B4f",*(pos*scale),*(normals@v.normal).normalized(),*uv,
                                    *(j for j,w in influences),*(w/total for j,w in influences)))
        indices.append(vertices[key])
with (output / "AresSkin.bytes").open("wb") as stream:
    stream.write(b"ASK1"+struct.pack("<II",len(vertices),len(indices)))
    for p in pivots: stream.write(struct.pack("<3f",*(Vector(p)*scale)))
    stream.write(data)
    stream.write(struct.pack("<"+"H"*len(indices),*indices))
print("Ares gameplay mesh:",len(vertices),"vertices,",len(indices)//3,"triangles")

# Optional authoring file for artists who want to inspect or adjust the weights.
if "--save-rig" in sys.argv:
    armature = bpy.data.armatures.new("Ares Skeleton")
    rig = bpy.data.objects.new("Ares Rig", armature)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    parents = [None,0,0,0,None,None,2,3,4,5]
    ends = [1,None,6,7,8,9,10,11,None,None]
    for i, group in groups.items():
        bone = armature.edit_bones.new(group.name)
        bone.head = pivots[i]
        bone.tail = pivots[ends[i]] if ends[i] is not None else Vector(pivots[i])+Vector((0,0,.22 if i==1 else -.12))
        if parents[i] is not None:
            bone.parent = armature.edit_bones[groups[parents[i]].name]
    bpy.ops.object.mode_set(mode="OBJECT")
    actor.parent = rig
    deform = actor.modifiers.new("Ares skin","ARMATURE")
    deform.object = rig
    # The editable gameplay rig uses the same single albedo as the mobile model.
    mat = actor.data.materials[0]
    for node in list(mat.node_tree.nodes):
        if node.type == "TEX_IMAGE" and node.image != image:
            mat.node_tree.nodes.remove(node)
    for unused in list(bpy.data.images):
        if unused != image and unused.users == 0: bpy.data.images.remove(unused)
    image.pack()
    rig.show_in_front = True
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(root / "ArtSource/Ares-rig.blend"))
