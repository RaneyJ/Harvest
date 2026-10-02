"""Validate exported geometry, skin weights, clip timing and sampled deformations after FBX reimport."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;REPO=ROOT.parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(REPO/'Assets/Harvest/Art/Characters/MarineViewArms/MarineServiceRifleView.fbx'))
scene=bpy.context.scene;scene.render.fps=30
rig=next(o for o in scene.objects if o.type=='ARMATURE');meshes=[o for o in scene.objects if o.type=='MESH']
assert len(rig.data.bones)==48
assert len(meshes)==6
for o in meshes:
 assert o.data.uv_layers.active is not None
 for vertex in o.data.vertices:
  weights=[g.weight for g in vertex.groups if g.weight>1e-6]
  assert weights and len(weights)<=4,(o.name,vertex.index,'weights')
  assert abs(sum(weights)-1)<.001
  assert all(math.isfinite(c) for c in vertex.co)
for track in rig.animation_data.nla_tracks:track.mute=True
report={'bone_count':len(rig.data.bones),'mesh_count':len(meshes),'skin_weights':'normalized; at most four influences','clips':[]}
expected={'Idle':3,'Fire':.2,'Reload':2.4}
for name,duration in expected.items():
 action=next(a for a in bpy.data.actions if a.name.endswith('|'+name));a,b=action.frame_range;assert abs((b-a)/30-duration)<.001
 rig.animation_data.action=action
 rig.animation_data.action_slot=action.slots[0]
 samples=[]
 for phase in [0,.24,.44,.61,.82,1]:
  frame=a+(b-a)*phase;scene.frame_set(int(frame),subframe=frame-int(frame));deps=bpy.context.evaluated_depsgraph_get()
  for o in meshes:
   data=o.evaluated_get(deps).to_mesh()
   assert all(math.isfinite(c) for v in data.vertices for c in v.co)
   o.evaluated_get(deps).to_mesh_clear()
  samples.append([phase,[round(c,4) for c in rig.pose.bones['Magazine'].matrix.translation]])
 report['clips'].append({'name':name,'seconds':duration,'sampled_magazine_positions':samples})
# Imported magazine must visibly leave the well and return, without accumulated offsets.
reload=report['clips'][2]['sampled_magazine_positions'];assert (Vector(reload[0][1])-Vector(reload[-1][1])).length<.001
assert max((Vector(x[1])-Vector(reload[0][1])).length for x in reload)>.45
report['result']='passed; Unity compiler/importer and Play mode still require verification'
(ROOT/'round_trip_validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report),flush=True)
