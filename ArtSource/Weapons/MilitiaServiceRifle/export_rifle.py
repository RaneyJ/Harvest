"""Bake the reviewed source into a shared 2K atlas and export visual-only meshes.
Requires bpy 4.5.3. All outputs are relative to this versioned source directory.
"""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Matrix,Vector
import numpy as np
ROOT=Path(__file__).resolve().parent
GAME=ROOT.parents[2]/'Assets/Harvest/Art/Weapons/MilitiaServiceRifle'
GAME.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'MilitiaServiceRifle.blend'))
scene=bpy.context.scene;scene.cycles.samples=8;scene.render.bake.margin=12;scene.render.bake.use_clear=False
source=bpy.data.collections['01 EDITABLE RIFLE'];studio=bpy.data.collections['02 REVIEW STUDIO']
export=bpy.data.collections.new('03 BAKED EXPORT');scene.collection.children.link(export)
deps=bpy.context.evaluated_depsgraph_get();groups={}
for src in source.objects:src.name='EDIT_'+src.name
for src in source.objects:
    if src.type!='MESH':continue
    data=bpy.data.meshes.new_from_object(src.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
    data.transform(src.matrix_world)
    obj=bpy.data.objects.new(src.name+'_BAKE',data);export.objects.link(obj);groups.setdefault(src.get('part','Body'),[]).append(obj)
source.hide_render=True;source.hide_viewport=True;studio.hide_render=True
objects=[]
for name,members in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for obj in members:obj.select_set(True)
    bpy.context.view_layer.objects.active=members[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name;objects.append(obj)
    mod=obj.modifiers.new('Export triangles','TRIANGULATE');mod.keep_custom_normals=True;bpy.ops.object.modifier_apply(modifier=mod.name)
    # Normalize material indices after join without altering surface assignments.
    if not obj.data.uv_layers:obj.data.uv_layers.new(name='UVMap')
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:obj.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(68),island_margin=.008,area_weight=.25,correct_aspect=True,scale_to_bounds=True)
bpy.ops.object.mode_set(mode='OBJECT')
materials=set(m for obj in objects for m in obj.data.materials)
surface_links={}
for m in materials:
    out=next(n for n in m.node_tree.nodes if n.type=='OUTPUT_MATERIAL')
    surface_links[m]=out.inputs['Surface'].links[0].from_socket

def bake(name,kind):
    image=bpy.data.images.new(name,width=2048,height=2048,alpha=True)
    image.colorspace_settings.name='sRGB' if kind=='COLOR' else 'Non-Color'
    image.generated_color=(.5,.5,1,1) if kind=='NORMAL' else (0,0,0,1)
    for m in materials:
        n=m.node_tree.nodes;l=m.node_tree.links
        target=n.new('ShaderNodeTexImage');target.name='ATLAS_BAKE_TARGET';target.image=image;n.active=target
        out=next(x for x in n if x.type=='OUTPUT_MATERIAL');bs=next(x for x in n if x.type=='BSDF_PRINCIPLED')
        if kind!='NORMAL':
            emission=n.new('ShaderNodeEmission');emission.name='ATLAS_EMISSION'
            if kind=='COLOR':
                if bs.inputs['Base Color'].is_linked:l.new(bs.inputs['Base Color'].links[0].from_socket,emission.inputs['Color'])
                else:emission.inputs['Color'].default_value=bs.inputs['Base Color'].default_value
            else:emission.inputs['Color'].default_value=(1,m['bake_roughness'],m['bake_metallic'],1)
            l.new(emission.outputs[0],out.inputs['Surface'])
    bpy.ops.object.bake(type='NORMAL' if kind=='NORMAL' else 'EMIT')
    image.filepath_raw=str(GAME/(name+'.png'));image.file_format='PNG';image.save()
    for m in materials:
        n=m.node_tree.nodes;l=m.node_tree.links;out=next(x for x in n if x.type=='OUTPUT_MATERIAL')
        l.new(surface_links[m],out.inputs['Surface'])
        for key in ['ATLAS_BAKE_TARGET','ATLAS_EMISSION']:
            if n.get(key):n.remove(n[key])
    return image

albedo=bake('ServiceRifle_BaseColor','COLOR')
orm=bake('ServiceRifle_MetalRough','ORM')
normal=bake('ServiceRifle_Normal','NORMAL')
# URP metallic(R), occlusion(G), unused(B), smoothness(A); alpha stays linear.
pixels=np.empty(2048*2048*4,dtype=np.float32);orm.pixels.foreach_get(pixels);pixels=pixels.reshape(-1,4)
mask=np.stack([pixels[:,2],pixels[:,0],np.zeros(len(pixels)),1-pixels[:,1]],axis=1).astype(np.float32)
unity=bpy.data.images.new('ServiceRifle_UnityMask',width=2048,height=2048,alpha=True);unity.colorspace_settings.name='Non-Color';unity.pixels.foreach_set(mask.ravel());unity.filepath_raw=str(GAME/'ServiceRifle_UnityMask.png');unity.file_format='PNG';unity.save()

mat=bpy.data.materials.new('Harvest_ServiceRifle_Atlas');mat.use_nodes=True
n=mat.node_tree.nodes;l=mat.node_tree.links;bs=n.get('Principled BSDF')
c=n.new('ShaderNodeTexImage');c.image=albedo;l.new(c.outputs['Color'],bs.inputs['Base Color'])
r=n.new('ShaderNodeTexImage');r.image=orm;s=n.new('ShaderNodeSeparateColor');l.new(r.outputs['Color'],s.inputs[0]);l.new(s.outputs['Green'],bs.inputs['Roughness']);l.new(s.outputs['Blue'],bs.inputs['Metallic'])
t=n.new('ShaderNodeTexImage');t.image=normal;nm=n.new('ShaderNodeNormalMap');l.new(t.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs['Normal'],bs.inputs['Normal'])
for obj in objects:
    obj.data.materials.clear();obj.data.materials.append(mat)
    for p in obj.data.polygons:p.material_index=0
    # Triangulate explicitly to keep tangent basis consistent across FBX and glTF.
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Export triangles','TRIANGULATE');mod.keep_custom_normals=True;bpy.ops.object.modifier_apply(modifier=mod.name)

# Inspect the actual baked materials on the evaluated export geometry.
source.hide_viewport=True;source.hide_render=True;studio.hide_render=False
scene.camera=bpy.data.objects['Review Hero'];scene.cycles.samples=48
scene.render.image_settings.file_format='JPEG';scene.render.image_settings.color_mode='RGB';scene.render.image_settings.quality=95
scene.render.filepath=str(ROOT/'Review'/'Baked.jpg');bpy.ops.render.render(write_still=True)

# Convert model axes once: -X authoring forward -> -Y Blender forward -> +Z Unity.
rot=Matrix.Rotation(math.pi/2,4,'Z')
for obj in objects:obj.data.transform(rot)
root=bpy.data.objects.new('MilitiaServiceRifle',None);export.objects.link(root)
pivots={'Magazine':(.230-.077,0,-.055*.65+.0507),'Trigger':(-.017-.077,0,.022*.65+.0507),'Bolt':(.115-.077,.074,.176*.65+.0507)}
for obj in objects:
    obj.parent=root
    if obj.name in pivots:
        pivot=rot@Vector(pivots[obj.name]);obj.data.transform(Matrix.Translation(-pivot));obj.location=pivot
bpy.context.view_layer.update()
for name,position in [('Muzzle',(-.589-.077,0,.139*.65+.0507)),('GripAnchor',(0,0,0)),('MagazineAnchor',(.230-.077,0,-.055*.65+.0507))]:
    o=bpy.data.objects.new(name,None);export.objects.link(o);o.location=rot@Vector(position);o.parent=root

def selection(members):
    bpy.ops.object.select_all(action='DESELECT')
    for o in members:o.select_set(True)
    bpy.context.view_layer.objects.active=members[0]
selection(list(export.objects))
bpy.ops.export_scene.fbx(filepath=str(GAME/'MilitiaServiceRifle.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',use_mesh_modifiers=True,mesh_smooth_type='OFF',add_leaf_bones=False,bake_anim=False,path_mode='RELATIVE')
bpy.ops.export_scene.gltf(filepath=str(ROOT/'MilitiaServiceRifle.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_draco_mesh_compression_enable=True)

# Validation records actual output topology and bounds; non-manifold decorative chips are intentional.
info={'blender':bpy.app.version_string,'units':'meters','unity_forward':'+Z','origin':'pistol grip centre','materials':1,'atlas_size':2048,'parts':[]}
all_vertices=[]
for obj in objects:
    assert obj.data.uv_layers.active is not None
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    uv=obj.data.uv_layers.active.data
    assert all(-.0001<=c<=1.0001 for loop in uv for c in loop.uv)
    info['parts'].append({'name':obj.name,'vertices':len(obj.data.vertices),'triangles':len(obj.data.polygons)})
    all_vertices += [obj.matrix_world@v.co for v in obj.data.vertices]
info['triangles']=sum(x['triangles'] for x in info['parts'])
info['dimensions_blender']=[round(max(v[i] for v in all_vertices)-min(v[i] for v in all_vertices),4) for i in range(3)]
# A static, reduced mesh is supplied for actor/pickup presentation at distance.
lod_members=[]
for obj in objects:
    copy=obj.copy();copy.data=obj.data.copy();copy.matrix_world=obj.matrix_world.copy();copy.parent=None;export.objects.link(copy);copy.name='LOD1_'+obj.name;lod_members.append(copy)
selection(lod_members);bpy.ops.object.join();lod=bpy.context.object;lod.name='MilitiaServiceRifle_LOD1'
mod=lod.modifiers.new('World distance reduction','DECIMATE');mod.ratio=.32;mod.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier=mod.name)
selection([lod])
bpy.ops.export_scene.fbx(filepath=str(GAME/'MilitiaServiceRifle_LOD1.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='OFF',bake_anim=False,path_mode='RELATIVE')
info['lod1_triangles']=sum(len(p.vertices)-2 for p in lod.data.polygons)
assert info['lod1_triangles']<info['triangles']*.4
(ROOT/'validation.json').write_text(json.dumps(info,indent=2))
print('EXPORT_COMPLETE',json.dumps(info),flush=True)
