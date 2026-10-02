"""Round-trip the delivered FBX/GLB, checking dimensions, parts and texture presence."""
import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parent;GAME=ROOT.parents[2]/'Assets/Harvest/Art/Weapons/MilitiaServiceRifle'
expected=json.loads((ROOT/'validation.json').read_text())
results=[]
for path in [GAME/'MilitiaServiceRifle.fbx',GAME/'MilitiaServiceRifle_LOD1.fbx',ROOT/'MilitiaServiceRifle.glb']:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if path.suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(path))
 else:bpy.ops.import_scene.gltf(filepath=str(path))
 objects=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert objects
 vertices=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
 bounds=[max(v[i] for v in vertices)-min(v[i] for v in vertices) for i in range(3)]
 assert all(abs(a-b)<.002 for a,b in zip(bounds,expected['dimensions_blender'])),(path.name,bounds)
 assert all(o.data.uv_layers.active for o in objects)
 triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in objects)
 assert triangles==(expected['lod1_triangles'] if 'LOD1' in path.name else expected['triangles'])
 if 'LOD1' not in path.name:
  for name in ['Magazine','Bolt','Trigger','Muzzle','GripAnchor']:
   assert bpy.data.objects.get(name) is not None,(path.name,name)
 if path.suffix=='.glb':
  assert sum(i.size[0]==2048 and i.size[1]==2048 for i in bpy.data.images)>=3
 results.append({'file':path.name,'triangles':triangles,'dimensions':[round(x,4) for x in bounds],'round_trip':'passed'})
(ROOT/'round_trip_validation.json').write_text(json.dumps(results,indent=2));print(json.dumps(results,indent=2))
