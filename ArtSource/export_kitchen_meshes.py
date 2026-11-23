"""Export edited Blender collections into the Unity kitchen mesh library."""
import bpy
import os
import struct
import sys
from mathutils import Vector
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from kitchen_cast import JOINTS


def export_library(destination):
    collections = [c for c in bpy.data.collections if "chronos_origin" in c]
    if not collections:
        raise ValueError("No kitchen model collections in this Blender file")
    deps = bpy.context.evaluated_depsgraph_get()
    payload = bytearray(b"CCM3" + bytes([len(JOINTS)]))
    for pivot in JOINTS.values():
        payload.extend(struct.pack("<3f", *pivot))
    payload.extend(struct.pack("<i", len(collections)))
    joint_indices = {name: i for i, name in enumerate(JOINTS)}
    for collection in sorted(collections, key=lambda c: c.name):
        origin = Vector(collection["chronos_origin"])
        records = bytearray()
        count = 0
        for obj in sorted(collection.all_objects, key=lambda o: o.name):
            if obj.type != "MESH":
                continue
            evaluated = obj.evaluated_get(deps)
            mesh = evaluated.to_mesh()
            try:
                mesh.calc_loop_triangles()
                transform = evaluated.matrix_world
                normals = transform.to_3x3().inverted().transposed()
                for tri in mesh.loop_triangles:
                    mat = mesh.materials[tri.material_index]
                    records.extend(max(0, min(255, round(v*255))) for v in mat.diffuse_color[:3])
                    records.append(joint_indices.get(obj.name.partition("|")[0], 0))
                    for index in tri.vertices:
                        vert = mesh.vertices[index]
                        pos = transform @ vert.co - origin
                        normal = (normals @ (vert.normal if tri.use_smooth else tri.normal)).normalized()
                        records.extend(struct.pack("<6f", *pos, *normal))
                    count += 1
            finally:
                evaluated.to_mesh_clear()
        if not 0 < count < 20000:
            raise ValueError(f"Unexpected triangle budget for {collection.name}: {count}")
        name = collection.name.encode("ascii")
        payload.extend(bytes([len(name)]) + name + struct.pack("<i", count) + records)
    with open(destination + ".tmp", "wb") as stream:
        stream.write(payload)
    os.replace(destination + ".tmp", destination)
    print(f"Exported {len(collections)} models, {len(payload)} bytes.")


if __name__ == "__main__":
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    export_library(os.path.join(root, "Assets", "ChronosChef", "Resources", "KitchenModels.bytes"))
