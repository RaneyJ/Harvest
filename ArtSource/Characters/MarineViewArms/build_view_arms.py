"""Harvest marine view arms and authored service-rifle animation, Blender 4.5.3.
Exterior game art only. Regenerates the editable rig, FBX clips and review scene.
"""
import bpy, bmesh, math, json
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[2]
RIFLE=REPO/'ArtSource/Weapons/MilitiaServiceRifle'
GAME=REPO/'Assets/Harvest/Art/Characters/MarineViewArms'
GAME.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.fps=30
# Construct in Unity axes for readable grip coordinates; convert all authoring data to Blender.
def v(p):return Vector((p[0],-p[2],p[1]))
def unity(p):return Vector((p[0],p[2],-p[1]))
U=Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))
def mat(name,color,rough,metal=0,grain=250):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1);n=m.node_tree.nodes;l=m.node_tree.links;b=n.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=rough;b.inputs['Metallic'].default_value=metal
 noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=grain;noise.inputs['Detail'].default_value=2
 mix=n.new('ShaderNodeMixRGB');mix.inputs[1].default_value=(*(c*.8 for c in color),1);mix.inputs[2].default_value=(*(c*1.15 for c in color),1);l.new(noise.outputs['Fac'],mix.inputs[0]);l.new(mix.outputs[0],b.inputs['Base Color'])
 fine=n.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=1800
 bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.00025;l.new(fine.outputs[0],bump.inputs['Height']);l.new(bump.outputs[0],b.inputs['Normal']);m['rough']=rough;m['metal']=metal;return m
cloth=mat('Fatigue | faded olive weave',(.055,.066,.038),.9,grain=95)
glove=mat('Glove | charcoal leather',(.014,.019,.014),.78,grain=210)
reinforce=mat('Glove | palm reinforcement',(.032,.040,.025),.9)
armor=mat('Wrist armor | colony olive',(.078,.086,.051),.65,.12)
webbing=mat('Wrist strap | dark canvas',(.026,.030,.020),.95)
stitch=mat('Seams | muted thread',(.11,.12,.075),.95)
metal=mat('Buckle | blackened metal',(.023,.028,.025),.58,.65)
# Import the baked rifle, preserve the material atlas.
bpy.ops.import_scene.gltf(filepath=str(RIFLE/'MilitiaServiceRifle.glb'))
weapon_meshes=[o for o in scene.objects if o.type=='MESH']
for o in weapon_meshes:
 mw=o.matrix_world.copy();o.parent=None;o.matrix_world=mw
for o in list(scene.objects):
 if o.type=='EMPTY':bpy.data.objects.remove(o,do_unlink=True)
# Bone positions in Unity local weapon space, with the root at the gripping hand.
joints={
 'R':{'shoulder':(.26,-.30,-.40),'elbow':(.23,-.14,-.25),'wrist':(.065,-.065,-.055),'hand':(.045,.008,.005)},
 'L':{'shoulder':(-.36,-.30,-.38),'elbow':(-.27,-.14,.04),'wrist':(-.067,-.02,.39),'hand':(-.059,.050,.408)}}
bones={'Root':((0,-.35,-.45),(0,-.25,-.45),None),'Weapon':((0,0,0),(0,0,.1),'Root'),
 'Magazine':((0,.01495,-.153),(0,.08,-.153),'Weapon'),'Bolt':((.074,.1651,-.038),(.074,.1651,.025),'Weapon')}
for side,j in joints.items():
 bones['UpperArm.'+side]=(j['shoulder'],j['elbow'],'Root');bones['Forearm.'+side]=(j['elbow'],j['wrist'],'UpperArm.'+side);bones['Hand.'+side]=(j['wrist'],j['hand'],'Forearm.'+side)
arm_meshes=[]
def mesh(name,verts,faces,material,weights):
 data=bpy.data.meshes.new(name);data.from_pydata([v(p) for p in verts],[],faces);data.update();bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
 o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.data.materials.append(material)
 for poly in data.polygons:poly.use_smooth=True
 for i,w in enumerate(weights):
  for bone,weight in w.items():
   group=o.vertex_groups.get(bone) or o.vertex_groups.new(name=bone);group.add([i],weight,'REPLACE')
 arm_meshes.append(o);return o
# Smooth elliptical rings follow a path; deformation weights blend adjacent arm bones.
def loft(name,points,radii,material,bone,axis=(1,0,0),sides=14,weight_fn=None):
 points=[Vector(p) for p in points];verts=[];weights=[];faces=[]
 for k,p in enumerate(points):
  direction=(points[min(k+1,len(points)-1)]-points[max(0,k-1)]).normalized();a=Vector(axis);a=(a-direction*a.dot(direction)).normalized();b=direction.cross(a).normalized()
  for n in range(sides):
   t=n*math.tau/sides;verts.append(p+a*(math.cos(t)*radii[k][0])+b*(math.sin(t)*radii[k][1]));weights.append(weight_fn(k,n) if weight_fn else {bone:1})
 for k in range(len(points)-1):
  for n in range(sides):q=k*sides+n;r=k*sides+(n+1)%sides;faces.append((q,r,r+sides,q+sides))
 faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+n for n in range(sides))]);return mesh(name,verts,faces,material,weights)
def capsule(name,a,b,r,material,bone,flat=1):
 a=Vector(a);b=Vector(b);d=b-a
 return loft(name,[a-d*.04,a,a+d*.22,a+d*.78,b,b+d*.04],[(r*.25,r*.25*flat),(r*.78,r*.78*flat),(r,r*flat),(r,r*flat),(r*.78,r*.78*flat),(r*.25,r*.25*flat)],material,bone,sides=12)
def plate(name,center,scale,material,bone,bevel=.005):
 bpy.ops.mesh.primitive_cube_add(size=1,location=v(center));o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);m=o.modifiers.new('Soft sewn or rolled edge','BEVEL');m.width=bevel;m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
 o.data.materials.append(material);g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE');arm_meshes.append(o);return o
for side,j in joints.items():
 shoulder,elbow,wrist,hand=[Vector(j[n]) for n in ['shoulder','elbow','wrist','hand']];s=1 if side=='R' else -1
 upper='UpperArm.'+side;fore='Forearm.'+side;hb='Hand.'+side
 # Broad fatigue sleeve tapers through the elbow into a gathered cuff.
 upper_ts=[i/9 for i in range(10)];fore_ts=[i/36*.97 for i in range(1,37)]
 points=[shoulder.lerp(elbow,t) for t in upper_ts]+[elbow.lerp(wrist,t) for t in fore_ts]
 rad=[(.073-.012*t,.066-.009*t) for t in upper_ts]+[(.061-.021*t,.057-.019*t) for t in fore_ts]
 def weights(k,n,upper=upper,fore=fore):
  t=max(0,min(1,(k-7)/5));return {upper:1-t,fore:t}
 sleeve=loft('Fatigue sleeve '+side,points,rad,cloth,fore,weight_fn=weights,sides=32)
 # Shallow diagonal creases break up the silhouette and catch light like gathered fabric.
 for vertex in sleeve.data.vertices:
  ring=vertex.index//32;theta=(vertex.index%32)*math.tau/32
  if ring<10:frac=ring/9;strength=.022+.045*frac
  else:frac=(ring-10)/35;strength=.040+.050*frac
  center=v(points[ring]);radial=vertex.co-center
  wrinkle=math.sin(frac*math.tau*5.5+1.8*math.sin(theta)+(.6 if side=='L' else 0))
  secondary=math.sin(frac*math.tau*12-theta*2)*.018
  vertex.co=center+radial*(1+strength*wrinkle+secondary)
 # Five circumferential compressed fabric folds near the wrist, no detached rings.
 for t in [.60,.70,.78,.86,.92]:
  p=elbow.lerp(wrist,t);d=(wrist-elbow).normalized();radius=.055-.015*t
  loft('Gathered sleeve fold '+side,[p-d*.006,p,p+d*.006],[(radius*.94,radius*.87),(radius*1.06,radius*.97),(radius*.94,radius*.87)],cloth,fore,sides=24)
 cuff=elbow.lerp(wrist,.97);d=(wrist-elbow).normalized()
 loft('Reinforced cuff '+side,[cuff-d*.018,cuff+d*.010],[(.043,.039),(.039,.035)],webbing,fore,sides=24)
 # Armor lies over the outside forearm, tapering toward the wrist.
 # A faceted shell follows the forearm axis and wraps its visible outer surface.
 direction=(wrist-elbow).normalized();outside=Vector((s,0,0));outside=(outside-direction*outside.dot(direction)).normalized();up=direction.cross(outside).normalized()
 centers=[elbow.lerp(wrist,t) for t in [.33,.38,.68,.76]];verts=[]
 for k,center in enumerate(centers):
  r=.056-.013*[.33,.38,.68,.76][k];width=[.72,1,1,.70][k]
  for theta in [-.72,-.36,0,.36,.72]:
   angle=theta*width;verts.append(center+outside*(math.cos(angle)*(r+.008))+up*(math.sin(angle)*(r+.008)))
 faces=[]
 for k in range(3):
  for n in range(4):a=k*5+n;faces.append((a,a+1,a+6,a+5))
 shell=mesh('Wrapped forearm armor '+side,verts,faces,armor,[{fore:1} for _ in verts])
 solid=shell.modifiers.new('Rolled shell thickness','SOLIDIFY');solid.thickness=.005
 bevel=shell.modifiers.new('Worn shell rim','BEVEL');bevel.width=.002;bevel.segments=2
 bpy.context.view_layer.objects.active=shell
 for mod in list(shell.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
 for t in [.35,.76]:
  p=elbow.lerp(wrist,t);d=(wrist-elbow).normalized();r=.056-.013*t
  loft('Armor fastening band '+side,[p-d*.008,p+d*.008],[(r,r*.95),(r,r*.95)],webbing,fore,sides=20)
  plate('Inset strap keeper '+side,p+Vector((s*(r-.005),.014,0)),(.008,.025,.025),metal,fore,.003)
 # Continuous palm, cuff and knuckles. Glove fingers are articulated, with rigid protective pads.
 loft('Glove palm '+side,[wrist,wrist.lerp(hand,.35),hand,hand+(hand-wrist)*.17],[(.027,.023),(.032,.025),(.035,.021),(.028,.017)],glove,hb,axis=(0,0,1) if side=='L' else (0,1,0),sides=20)
 if side=='R':
  fingerpaths={}
  for i,name in enumerate(['Index','Middle','Ring','Little']):
   y=.036-i*.022
   fingerpaths[name]=[(.049,y,.016),(.043,y,.049),(.004,y-.002,.064),(-.027,y-.005,.053),(-.037,y-.008,.035)]
  fingerpaths['Index']=[(.048,.045,.02),(.041,.044,.071),(.018,.036,.097),(-.003,.022,.104),(-.013,.014,.094)]
  fingerpaths['Thumb']=[(.041,.019,-.026),(.028,.052,-.015),(-.001,.060,.007),(-.021,.055,.023)]
 else:
  fingerpaths={}
  for i,name in enumerate(['Index','Middle','Ring','Little']):
   z=.447-i*.021
   fingerpaths[name]=[(-.062,.052,z),(-.034,.031,z),(.008,.031,z),(.049,.049,z),(.058,.077,z)]
  fingerpaths['Thumb']=[(-.071,.063,.391),(-.071,.102,.416),(-.051,.129,.429),(-.024,.137,.436)]
 for f,path in fingerpaths.items():
  for k in range(len(path)-1):
   bn=f'{f}{k+1}.{side}';bones[bn]=(path[k],path[k+1],hb if k==0 else f'{f}{k}.{side}')
   r=.010 if f!='Little' else .0085
   if f=='Thumb':r=.012
   capsule(f'Glove {f} {k+1} {side}',path[k],path[k+1],r*(1-k*.10),glove,bn,flat=.88)
   if k==0:
    mid=Vector(path[k]).lerp(Vector(path[k+1]),.40)
    mid+=Vector((.007 if side=='R' else 0,.002 if side=='R' else -.007,0))
    capsule(f'Knuckle pad {f} {side}',mid,mid+(Vector(path[k+1])-Vector(path[k]))*.35,r*.82,reinforce,bn,flat=.5)
 # Glove seam on the visible outer wrist.
 for q in [-1,1]:capsule('Glove seam '+side,wrist+Vector((s*.025,.008,q*.011)),wrist.lerp(hand,.66)+Vector((s*.025,.008,q*.011)),.0011,stitch,hb)
# Skeleton with deform bones for arms/fingers and rigid weapon parts.
bpy.ops.object.armature_add();rig=bpy.context.object;rig.name='MarineRifleRig';bpy.ops.object.mode_set(mode='EDIT');rig.data.edit_bones.remove(rig.data.edit_bones[0])
for name,(a,b,parent) in bones.items():
 eb=rig.data.edit_bones.new(name);eb.head=v(a);eb.tail=v(b)
 if parent:eb.parent=rig.data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
for o in weapon_meshes:
 name='Magazine' if o.name=='Magazine' else 'Bolt' if o.name=='Bolt' else 'Weapon';g=o.vertex_groups.new(name=name);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
for o in arm_meshes+weapon_meshes:
 mw=o.matrix_world.copy();o.parent=rig;o.matrix_world=mw;m=o.modifiers.new('Marine deformation','ARMATURE');m.object=rig
# Bake the arm material atlas once, before animation, retaining a single arm material in Unity.
bpy.ops.object.select_all(action='DESELECT')
for o in arm_meshes:o.select_set(True)
bpy.context.view_layer.objects.active=arm_meshes[0];bpy.ops.object.join();arms=bpy.context.object;arms.name='MarineArms'
mod=arms.modifiers.new('Stable export triangles','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(65),island_margin=.008,area_weight=.25);bpy.ops.object.mode_set(mode='OBJECT')
materials=set(arms.data.materials);links={m:next(n for n in m.node_tree.nodes if n.type=='OUTPUT_MATERIAL').inputs['Surface'].links[0].from_socket for m in materials}
scene.render.engine='CYCLES';scene.cycles.samples=4;scene.cycles.device='CPU';scene.render.threads_mode='FIXED';scene.render.threads=8;scene.render.bake.margin=8
images={}
for kind in ['Color','Normal','Mask']:
 im=bpy.data.images.new('MarineArms_'+kind,width=1024,height=1024,alpha=True);im.colorspace_settings.name='sRGB' if kind=='Color' else 'Non-Color'
 for mat in materials:
  n=mat.node_tree.nodes;l=mat.node_tree.links;t=n.new('ShaderNodeTexImage');t.name='BAKE';t.image=im;n.active=t;b=n.get('Principled BSDF');out=next(x for x in n if x.type=='OUTPUT_MATERIAL')
  if kind!='Normal':
   e=n.new('ShaderNodeEmission');e.name='BAKE_EMIT'
   if kind=='Color':l.new(b.inputs['Base Color'].links[0].from_socket,e.inputs['Color'])
   else:e.inputs['Color'].default_value=(mat['metal'],1,1-mat['rough'],1)
   l.new(e.outputs[0],out.inputs['Surface'])
 bpy.ops.object.bake(type='NORMAL' if kind=='Normal' else 'EMIT')
 if kind=='Mask':
  px=np.empty(1024*1024*4,dtype=np.float32);im.pixels.foreach_get(px);px=px.reshape(-1,4);px[:,3]=px[:,2].copy();px[:,2]=0;im.pixels.foreach_set(px.ravel())
 im.filepath_raw=str(GAME/('MarineArms_'+kind+'.png'));im.file_format='PNG';im.save();images[kind]=im
 for mat in materials:
  n=mat.node_tree.nodes;l=mat.node_tree.links;out=next(x for x in n if x.type=='OUTPUT_MATERIAL');l.new(links[mat],out.inputs['Surface']);n.remove(n['BAKE'])
  if n.get('BAKE_EMIT'):n.remove(n['BAKE_EMIT'])
atlas=bpy.data.materials.new('Harvest_MarineArms_Atlas');atlas.use_nodes=True;n=atlas.node_tree.nodes;l=atlas.node_tree.links;b=n.get('Principled BSDF')
t=n.new('ShaderNodeTexImage');t.image=images['Color'];l.new(t.outputs[0],b.inputs['Base Color'])
t=n.new('ShaderNodeTexImage');t.image=images['Normal'];normal=n.new('ShaderNodeNormalMap');l.new(t.outputs[0],normal.inputs[1]);l.new(normal.outputs[0],b.inputs['Normal'])
t=n.new('ShaderNodeTexImage');t.image=images['Mask'];sep=n.new('ShaderNodeSeparateColor');l.new(t.outputs[0],sep.inputs[0]);l.new(sep.outputs['Red'],b.inputs['Metallic']);inv=n.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;l.new(t.outputs['Alpha'],inv.inputs[1]);l.new(inv.outputs[0],b.inputs['Roughness'])
arms.data.materials.clear();arms.data.materials.append(atlas)
for poly in arms.data.polygons:poly.material_index=0
# Share the authoring and standalone animation workflow.
exec(compile((ROOT/'animate_view_arms.py').read_text(), str(ROOT/'animate_view_arms.py'), 'exec'), globals())
