"""Rebuild clips and review renders from the editable marine rig without rebaking textures."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
if 'rig' not in globals():
 ROOT=Path(__file__).resolve().parent;REPO=ROOT.parents[2];GAME=REPO/'Assets/Harvest/Art/Characters/MarineViewArms'
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'MarineServiceRifleView.blend'))
 scene=bpy.context.scene;rig=bpy.data.objects['MarineRifleRig'];arms=bpy.data.objects['MarineArms'];weapon_meshes=[o for o in scene.objects if o.type=='MESH' and o!=arms]
 def v(p):return Vector((p[0],-p[2],p[1]))
 def unity(p):return Vector((p[0],p[2],-p[1]))
 U=Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
 bones={b.name:(unity(b.head_local),unity(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones}
 joints={side:{'shoulder':bones['UpperArm.'+side][0],'elbow':bones['Forearm.'+side][0],'wrist':bones['Hand.'+side][0],'hand':bones['Hand.'+side][1]} for side in ['R','L']}
 rig.animation_data_clear()
 for action in list(bpy.data.actions):bpy.data.actions.remove(action)
# Animation is baked explicitly, avoiding exported Blender IK/constraint dependencies.
rests={name:bone.matrix_local.copy() for name,bone in rig.data.bones.items()}
def orient(a,b,rest):
 a=v(a);b=v(b);q=(b-a).to_track_quat('Y','Z');return Matrix.Translation(a)@q.to_matrix().to_4x4()
def apply_matrix(name,m):
 pb=rig.pose.bones[name];pb.matrix=m;bpy.context.view_layer.update()
def smooth(t):t=max(0,min(1,t));return t*t*(3-2*t)
def interp(keys,t):
 for (a,av),(b,bv) in zip(keys,keys[1:]):
  if a<=t<=b:return Vector(av).lerp(Vector(bv),smooth((t-a)/(b-a)))
 return Vector(keys[-1][1] if t>keys[-1][0] else keys[0][1])
# The support hand travels below the trigger guard before taking hold of the rear magazine.
hand_keys=[(0,(0,0,0)),(.12,(-.065,-.13,-.21)),(.24,(.026,-.125,-.553)),(.33,(.026,-.125,-.553)),(.44,(.026,-.32,-.59)),(.53,(-.013,-.66,-.56)),(.61,(-.013,-.66,-.56)),(.73,(.026,-.31,-.59)),(.82,(.026,-.125,-.553)),(.86,(.026,-.125,-.553)),(.94,(-.06,-.13,-.20)),(1,(0,0,0))]
mag_keys=[(0,(0,0,0)),(.33,(0,0,0)),(.44,(0,-.195,-.037)),(.53,(-.039,-.535,-.007)),(.61,(-.039,-.535,-.007)),(.73,(0,-.185,-.037)),(.82,(0,0,0)),(1,(0,0,0))]
rig.animation_data_create();actions=[]
for name,length in [('Idle',90),('Fire',6),('Reload',72)]:
 action=bpy.data.actions.new(name);rig.animation_data.action=action;actions.append(action)
 for frame in range(length+1):
  scene.frame_set(frame);t=frame/length
  for pb in rig.pose.bones:pb.rotation_mode='QUATERNION';pb.matrix_basis=Matrix.Identity(4)
  breathe=math.sin(t*math.tau)*.0015 if name=='Idle' else 0
  amount=math.sin(math.pi*t)**.6 if name=='Reload' else 0
  # Deliberate small rifle cant opens the magazine silhouette to the camera.
  W=U@(Matrix.Translation((0,-.018*amount+breathe,0))@Matrix.Rotation(math.radians(-11*amount),4,'Z')@Matrix.Rotation(math.radians(6*amount),4,'X'))@U.inverted()
  apply_matrix('Weapon',W@rests['Weapon'])
  delta=interp(mag_keys,t) if name=='Reload' else Vector((0,0,0))
  apply_matrix('Magazine',W@Matrix.Translation(v(delta))@rests['Magazine'])
  bolt=-.028*math.sin(math.pi*min(1,t*1.5)) if name=='Fire' else 0
  apply_matrix('Bolt',W@Matrix.Translation(v((0,0,bolt)))@rests['Bolt'])
  for side,j in joints.items():
   delta=interp(hand_keys,t) if name=='Reload' and side=='L' else Vector((0,0,0))
   wrist0=Vector(j['wrist']);hand0=Vector(j['hand']);wrist=unity(W@v(wrist0+delta));hand=unity(W@v(hand0+delta));shoulder=Vector(j['shoulder']);elbow0=Vector(j['elbow'])
   # Analytic two-bone solve keeps sleeve and limb lengths constant through the reload.
   upper_length=(elbow0-shoulder).length;lower_length=(wrist0-elbow0).length
   target=wrist-shoulder;distance=max(.001,min(target.length,upper_length+lower_length-.0001));direction=target.normalized()
   along=(upper_length**2-lower_length**2+distance**2)/(2*distance)
   bend=elbow0-shoulder;perpendicular=(bend-direction*bend.dot(direction)).normalized()
   elbow=shoulder+direction*along+perpendicular*math.sqrt(max(0,upper_length**2-along**2))
   for bn,a,b in [('UpperArm.'+side,shoulder,elbow),('Forearm.'+side,elbow,wrist)]:
    rest=rests[bn];direction=(v(b)-v(a)).normalized();old=(rest.to_3x3()@Vector((0,1,0))).normalized();rotation=old.rotation_difference(direction).to_matrix().to_4x4();matrix=Matrix.Translation(v(a))@rotation@rest.to_3x3().to_4x4();scale=(Vector(b)-Vector(a)).length/(Vector(bones[bn][1])-Vector(bones[bn][0])).length;matrix=matrix@Matrix.Diagonal((1,scale,1,1));apply_matrix(bn,matrix)
   apply_matrix('Hand.'+side,W@Matrix.Translation(v(delta))@rests['Hand.'+side])
   if name=='Reload' and side=='L':
    grip=smooth((t-.17)/.07)*(1-smooth((t-.86)/.06))
    # Close the support fingers around the narrower magazine, maintaining their contact surface.
    squeeze=Matrix.Translation(v((-.067,0,0)))@Matrix.Diagonal((1-.35*grip,1,1,1))@Matrix.Translation(v((.067,0,0)))
    for bn in bones:
     if bn.endswith('.L') and any(bn.startswith(f) for f in ['Index','Middle','Ring','Little','Thumb']):
      apply_matrix(bn,W@Matrix.Translation(v(delta))@squeeze@rests[bn])
  if name=='Fire':rig.pose.bones['Index2.R'].rotation_quaternion=Vector((1,0,0)).rotation_difference(Vector((1,.11*math.sin(t*math.pi),0)))
  for pb in rig.pose.bones:
   pb.keyframe_insert('location',frame=frame,group=pb.name);pb.keyframe_insert('rotation_quaternion',frame=frame,group=pb.name);pb.keyframe_insert('scale',frame=frame,group=pb.name)
 action.use_fake_user=True
 # NLA takes yield stable named FBX clips and do not depend on an active action.
 track=rig.animation_data.nla_tracks.new();track.name=name;strip=track.strips.new(name,0,action);track.mute=True
rig.animation_data.action=actions[0];scene.frame_start=0;scene.frame_end=90;scene.frame_set(0)
# Pack images so the editable source is self-contained.
for im in bpy.data.images:
 if im.source=='FILE' and not im.packed_file:
  try:im.pack()
  except RuntimeError:pass
scene['harvest_notes']='Authored service-rifle view arms; idle/fire/reload. Clip duration follows existing gameplay reload timer in Unity. No gameplay changes.'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'MarineServiceRifleView.blend'),compress=True)
# Export each action as a named take. Standard FBX armature skinning; no leaf bones or root motion.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in weapon_meshes+[arms]:o.select_set(True)
bpy.context.view_layer.objects.active=rig
for track in rig.animation_data.nla_tracks:track.mute=False
rig.animation_data.action=None
bpy.ops.export_scene.fbx(filepath=str(GAME/'MarineServiceRifleView.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,bake_anim_force_startend_keying=True,path_mode='RELATIVE')
# Review uses the baked materials, the same shared camera pose, and authored animations.
for track in rig.animation_data.nla_tracks:track.mute=True
rig.animation_data.action=actions[0];scene.frame_set(0)
pose=json.loads((REPO/'Assets/Harvest/Art/Weapons/MilitiaServiceRifle/ViewPose.json').read_text())
C=Matrix.Diagonal((1,1,-1,1));toUnity=U.inverted();p=Vector(tuple(pose['Position'][x] for x in 'xyz'));base=Matrix.Rotation(math.radians(pose['Euler']['y']),4,'Y')@Matrix.Rotation(math.radians(pose['Euler']['x']),4,'X')
rig.matrix_world=C@Matrix.Translation(p)@base@toUnity
bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='Review camera';cam.data.type='PERSP';cam.data.sensor_fit='VERTICAL';cam.data.sensor_height=24;cam.data.lens=24/(2*math.tan(math.radians(pose['VerticalFov']/2)));cam.data.clip_start=pose['NearClip'];scene.camera=cam
# Review uses the same near plane as the gameplay camera.
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.view_settings.view_transform='AgX'
world=bpy.data.worlds.new('Neutral studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.17,.16,1);world.node_tree.nodes['Background'].inputs[1].default_value=.65;scene.world=world
for loc,power,size in [((-1,1,-.5),90,2),((1,.3,-.4),40,1.5),((0,.5,-2),80,1.2)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.25,-.4,-.8))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.image_settings.file_format='JPEG';scene.render.image_settings.color_mode='RGB';scene.render.image_settings.quality=95
for name,action,frame in [('Hold',actions[0],0),('MagazineGrip',actions[2],23),('MagazineOut',actions[2],32),('MagazineSeat',actions[2],58)]:
 rig.animation_data.action=action;scene.frame_set(frame);scene.render.filepath=str(ROOT/'Review'/(name+'.jpg'));bpy.ops.render.render(write_still=True)
rig.animation_data.action=actions[0];scene.frame_set(0);bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'MarineServiceRifleReview.blend'),compress=True)
info={'blender':bpy.app.version_string,'arm_triangles':sum(len(p.vertices)-2 for p in arms.data.polygons),'bones':len(rig.data.bones),'clips':{'Idle':3,'Fire':.2,'Reload':2.4},'source_fps':30,'atlas':1024,'status':'Blender export and renders; Unity verification required'}
(ROOT/'validation.json').write_text(json.dumps(info,indent=2));print('ARMS_COMPLETE',json.dumps(info),flush=True)
