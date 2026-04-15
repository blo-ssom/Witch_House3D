import bpy
import sys
import os

argv = sys.argv[sys.argv.index("--") + 1:]
src = argv[0]
dst = argv[1]

bpy.ops.wm.read_factory_settings(use_empty=True)

try:
    bpy.types.CyclesLightSettings.cast_shadow = bpy.props.BoolProperty(default=True)
except Exception as e:
    print(f"[warn] cast_shadow patch skipped: {e}")

from io_scene_fbx import import_fbx as _ifbx
_orig_light = _ifbx.blen_read_light
def _safe_light(fbx_tmpl, fbx_obj, settings):
    try:
        return _orig_light(fbx_tmpl, fbx_obj, settings)
    except AttributeError as e:
        print(f"[warn] skipped light import: {e}")
        return None
_ifbx.blen_read_light = _safe_light

bpy.ops.import_scene.fbx(filepath=src)

mesh_objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
print(f"[info] mesh count: {len(mesh_objs)}")

for obj in mesh_objs:
    print(f"[info] processing: {obj.name}  verts={len(obj.data.vertices)}  polys={len(obj.data.polygons)}")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')

    bpy.ops.mesh.remove_doubles(threshold=0.0001)

    bpy.ops.mesh.mark_seam(clear=True)

    bpy.ops.mesh.normals_make_consistent(inside=False)

    bpy.ops.object.mode_set(mode='OBJECT')

    for p in obj.data.polygons:
        p.use_smooth = True

    obj.select_set(False)
    print(f"[info] after: verts={len(obj.data.vertices)}  polys={len(obj.data.polygons)}")

for obj in mesh_objs:
    obj.select_set(True)

bpy.ops.export_scene.fbx(
    filepath=dst,
    use_selection=True,
    apply_unit_scale=True,
    bake_space_transform=False,
    mesh_smooth_type='FACE',
    use_mesh_modifiers=True,
    add_leaf_bones=False,
    path_mode='AUTO',
)

print(f"[done] exported: {dst}")
