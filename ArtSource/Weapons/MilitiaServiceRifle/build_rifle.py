"""Harvest fictional exterior prop, Blender 4.5.3. No functional weapon internals.
Run with Blender --background --python build_rifle.py, or the bpy Python module.
"""
import bpy, bmesh, math, json, argparse, sys, random
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parent
GAME=ROOT.parents[2]/'Assets/Harvest/Art/Weapons/MilitiaServiceRifle'
GAME.mkdir(parents=True,exist_ok=True)
PREVIEW=ROOT/'Review';PREVIEW.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
source=bpy.data.collections.new('01 EDITABLE RIFLE');scene.collection.children.link(source)
studio=bpy.data.collections.new('02 REVIEW STUDIO');scene.collection.children.link(studio)
parts=[]

def relocate(obj,collection):
    for c in list(obj.users_collection):c.objects.unlink(obj)
    collection.objects.link(obj)

def material(name,color,metal=0,rough=.55,wear=.25):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
    out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled');l.new(bs.outputs['BSDF'],out.inputs['Surface'])
    pos=n.new('ShaderNodeNewGeometry')
    noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=180;noise.inputs['Detail'].default_value=3;l.new(pos.outputs['Position'],noise.inputs['Vector'])
    ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.22;ramp.color_ramp.elements[0].color=(*(c*.78 for c in color),1)
    ramp.color_ramp.elements[1].position=.8;ramp.color_ramp.elements[1].color=(*(min(c*1.13,1) for c in color),1);l.new(noise.outputs['Fac'],ramp.inputs[0])
    edge=n.new('ShaderNodeValToRGB');edge.color_ramp.elements[0].position=.505;edge.color_ramp.elements[0].color=(0,0,0,1)
    edge.color_ramp.elements[1].position=.60;edge.color_ramp.elements[1].color=(wear,wear,wear,1);l.new(pos.outputs['Pointiness'],edge.inputs[0])
    mix=n.new('ShaderNodeMixRGB');mix.blend_type='MIX';mix.inputs[2].default_value=(.25,.255,.23,1) if metal else (min(color[0]*1.7,.20),min(color[1]*1.7,.20),min(color[2]*1.7,.20),1)
    l.new(edge.outputs[0],mix.inputs[0]);l.new(ramp.outputs[0],mix.inputs[1]);l.new(mix.outputs[0],bs.inputs['Base Color'])
    bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    fine=n.new('ShaderNodeTexNoise');fine.inputs['Scale'].default_value=1800;fine.inputs['Detail'].default_value=2;l.new(pos.outputs['Position'],fine.inputs[0])
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.22;bump.inputs['Distance'].default_value=.00012;l.new(fine.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs['Normal'],bs.inputs['Normal'])
    m['bake_metallic']=metal;m['bake_roughness']=rough
    return m
olive=material('Paint | colony olive grey',(.145,.165,.120),.5,.54,.55)
poly=material('Polymer | graphite',(.019,.026,.024),0,.67,.2)
steel=material('Steel | parkerized',(.043,.058,.053),.82,.46,.45)
rubber=material('Rubber | butt pad',(.019,.023,.021),0,.83,.1)
recess=material('Dark recessed surfaces',(.009,.013,.012),.2,.64,.05)
orange=material('Faded orange inspection paint',(.48,.245,.065),.05,.68,.4)
magmat=material('Magazine | graphite alloy',(.091,.107,.095),.65,.49,.45)
fastener=material('Hardware | dark steel',(.13,.145,.13),.85,.35,.3)


def finish(obj,name,mat,bevel=.002,group='Body'):
    obj.name=name;relocate(obj,source);obj.data.materials.append(mat);obj['part']=group;parts.append(obj)
    if bevel:
        mod=obj.modifiers.new('Machined edge radii','BEVEL');mod.width=bevel;mod.segments=3;mod.limit_method='ANGLE'
        mod=obj.modifiers.new('Face weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=40
    for p in obj.data.polygons:p.use_smooth=True
    # Smooth by angle is encoded as sharp edges, so large plates retain flat normals.
    if obj.type=='MESH':
        bm=bmesh.new();bm.from_mesh(obj.data)
        for e in bm.edges:
            if len(e.link_faces)==2 and e.calc_face_angle()>.5:e.smooth=False
        bm.to_mesh(obj.data);bm.free()
    return obj

def mesh(name,vertices,faces,mat,bevel=.002,group='Body'):
    m=bpy.data.meshes.new(name);m.from_pydata(vertices,[],faces);m.update()
    bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free()
    obj=bpy.data.objects.new(name,m);source.objects.link(obj);return finish(obj,name,mat,bevel,group)

def profile(name,points,width,mat,y=0,bevel=.002,group='Body'):
    k=len(points);v=[(x,y-width/2,z) for x,z in points]+[(x,y+width/2,z) for x,z in points]
    f=[tuple(range(k-1,-1,-1)),tuple(range(k,2*k))]+[(i,(i+1)%k,(i+1)%k+k,i+k) for i in range(k)]
    return mesh(name,v,f,mat,bevel,group)

def ring_profile(name,outer,inner,width,mat,y=0,bevel=.0015,group='Body'):
    k=len(outer);v=[(x,yy,z) for yy in [y-width/2,y+width/2] for loop in [outer,inner] for x,z in loop];f=[]
    for i in range(k):
        j=(i+1)%k
        f += [(i,j,k+j,k+i),(2*k+i,3*k+i,3*k+j,2*k+j),(i,2*k+i,2*k+j,j),(k+i,k+j,3*k+j,3*k+i)]
    return mesh(name,v,f,mat,bevel,group)

def box(name,location,scale,mat,bevel=.002,group='Body'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=location);o=bpy.context.object;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,name,mat,bevel,group)

def cylinder(name,location,radius,depth,mat,axis='Y',vertices=16,bevel=.001,group='Body'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=location);o=bpy.context.object
    o.rotation_euler=(math.pi/2,0,0) if axis=='Y' else (0,math.pi/2,0) if axis=='X' else (0,0,0)
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);o['keep_round']=True;return finish(o,name,mat,bevel,group)

def cut(target,cutter):
    bpy.context.view_layer.objects.active=target
    m=target.modifiers.new('Exterior recess','BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cutter
    # Apply cuts before the edge treatment.
    while target.modifiers.find(m.name)>0:bpy.ops.object.modifier_move_up(modifier=m.name)
    bpy.ops.object.modifier_apply(modifier=m.name)
    if cutter in parts:parts.remove(cutter)
    bpy.data.objects.remove(cutter,do_unlink=True)

def slot(target,name,location,scale,radius=.003):
    c=box(name,location,scale,recess,radius)
    bpy.context.view_layer.objects.active=c
    for m in list(c.modifiers):bpy.ops.object.modifier_apply(modifier=m.name)
    cut(target,c)

def tube(name,x0,x1,z,r,inner,mat,segments=24):
    loops=[(x0,r),(x1,r),(x1,inner),(x0,inner)];v=[(x,math.sin(i*math.tau/segments)*radius,z+math.cos(i*math.tau/segments)*radius) for x,radius in loops for i in range(segments)];f=[]
    for j in range(4):
        for i in range(segments):f.append((j*segments+i,j*segments+(i+1)%segments,((j+1)%4)*segments+(i+1)%segments,((j+1)%4)*segments+i))
    o=mesh(name,v,f,mat,.0008);o['keep_round']=True;return o

def wire_loop(name,points,mat,radius=.003,group='Body'):
    c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=12;c.bevel_depth=radius;c.bevel_resolution=3
    s=c.splines.new('POLY');s.points.add(len(points)-1)
    for p,co in zip(s.points,points):p.co=(*co,1)
    s.use_cyclic_u=True;o=bpy.data.objects.new(name,c);source.objects.link(o)
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False)
    return finish(o,name,mat,0,group)

def screw(x,z,side,y=.061,r=.005):
    o=cylinder('Recessed hex fastener',(x,side*y,z),r,.003,fastener,vertices=12,bevel=.0005)
    slot(o,'Fastener slot',(x,side*(y+.0016),z),(.006,.002,.0014),.0003)
    return o

# Large silhouette: compact colonial bullpup with panel gaps over a solid inner shell.
profile('Receiver chassis',[(-.453,.081),(-.437,.210),(-.407,.238),(.203,.238),(.247,.219),(.405,.219),(.420,.183),(.409,.072),(.292,.051),(.086,.045),(-.026,.061),(-.400,.061)],.091,steel,bevel=.004)
profile('Shoulder structure',[(.212,.198),(.432,.204),(.445,.180),(.439,-.061),(.411,-.078),(.372,-.040),(.319,.040),(.220,.062)],.086,poly,bevel=.006)
profile('Raised cheek rest',[(.213,.220),(.235,.245),(.415,.245),(.438,.230),(.438,.190),(.224,.190)],.104,poly,bevel=.008)
profile('Elastomer shoulder pad',[(.432,.245),(.455,.239),(.465,.213),(.465,-.058),(.450,-.082),(.428,-.078),(.432,.214)],.108,rubber,bevel=.004)
for z in [i*.012-.062 for i in range(25)]:box('Butt pad traction rib',(.465,0,z),(.0016,.092,.004),poly,.0007)

for side in [-1,1]:
    y=side*.051
    a=profile('Front receiver cladding',[(-.438,.148),(-.428,.209),(-.403,.230),(-.125,.230),(-.107,.212),(-.110,.158),(-.209,.158),(-.229,.143)],.014,olive,y,.0025)
    for x,w in [(-.365,.075),(-.258,.078)]:slot(a,'Upper cooling recess',(x,y,.179),(w,.045,.017),.006)
    profile('Mid receiver cladding',[(-.103,.156),(-.102,.230),(.046,.230),(.060,.214),(.055,.159),(-.010,.150)],.014,olive,y,.002)
    p=profile('Rear receiver cladding',[(.061,.160),(.064,.229),(.206,.229),(.224,.213),(.219,.151),(.142,.142),(.105,.160)],.014,olive,y,.002)
    slot(p,'Rear exterior inset',(.166,y,.190),(.038,.04,.012),.004)
    profile('Lower receiver side', [(-.065,.057),(-.073,.140),(.076,.142),(.108,.121),(.315,.128),(.334,.113),(.319,.082),(.098,.067),(.057,.033),(-.018,.035)],.014,poly,side*.049,.0025)
    profile('Stock diagonal reinforcing panel',[(.322,.091),(.414,.106),(.426,.079),(.418,-.047),(.402,-.037),(.368,.015)],.009,poly,side*.048,.002)
    for x,z in [(-.411,.202),(-.114,.182),(.066,.202),(.209,.214),(.316,.102),(.405,.129),(.404,-.019),(-.041,.089)]:screw(x,z,side)
    box('Butt plate steel keeper',(.426,side*.057,.165),(.014,.008,.104),steel,.001)
    for z in [.127,.199]:screw(.426,z,side,y=.063,r=.004)
    # Faded factory inspection slashes: exterior identity only.
    for x in [.049,.061]:profile('Orange inspection stripe',[(x,.091),(x-.014,.125),(x-.006,.125),(x+.008,.091)],.0006,orange,side*.057,0)

hand=profile('Ribbed polymer handguard',[(-.431,.063),(-.413,.139),(-.372,.150),(-.095,.147),(-.069,.122),(-.077,.037),(-.099,.018),(-.394,.018),(-.424,.037)],.117,poly,bevel=.005)
for side in [-1,1]:
    slot(hand,'Forearm upper vent',(-.299,side*.058,.117),(.176,.024,.014),.006)
    box('Forearm seam bead',(-.251,side*.060,.088),(.306,.004,.005),steel,.001)
    for i in range(12):
        x=-.393+i*.024
        p=profile('Molded handguard finger rib',[(x-.004,.083),(x+.003,.092),(x+.007,.081),(x+.007,.037),(x+.001,.025),(x-.007,.024),(x-.007,.035),(x-.003,.041)],.005,poly,side*.060,.0014)
for i in range(12):box('Handguard underside rib',(-.393+i*.024,0,.017),(.010,.085,.005),poly,.0015)

# Grip and trigger guard retain open negative space.
profile('Pistol grip',[(.016,.042),(.080,.054),(.150,-.170),(.133,-.190),(.083,-.197),(.065,-.180),(.077,-.158),(.010,.012)],.066,poly,bevel=.004,group='Grip')
for side in [-1,1]:
    profile('Grip inset panel',[(.025,.009),(.070,.023),(.132,-.157),(.088,-.172),(.092,-.152)],.003,poly,side*.034,.001,group='Grip')
    for i in range(9):
        z=-.008-i*.017;x=.055+(-z)*.34
        rib=box('Grip molded diagonal rib',(x,side*.037,z),(.043,.003,.004),poly,.001,group='Grip');rib.rotation_euler.y=math.radians(-17)
ring_profile('Trigger guard',[(-.077,.054),(.044,.054),(.069,-.066),(-.047,-.078),(-.084,-.035)],[(-.062,.037),(.031,.037),(.049,-.050),(-.039,-.059),(-.066,-.028)],.030,steel,bevel=.002)
profile('Trigger',[(-.025,.037),(-.014,.034),(-.009,-.012),(-.017,-.041),(-.036,-.052),(-.028,-.032),(-.024,-.007)],.011,steel,bevel=.001,group='Trigger')

# Magazine is a distinct detachable visual group, suitable for a future reload rig.
profile('Magazine well',[(.139,.074),(.304,.074),(.301,.018),(.154,.015)],.084,poly,bevel=.003)
profile('Magazine',[(.158,.034),(.294,.034),(.298,-.148),(.285,-.174),(.178,-.174),(.164,-.163)],.067,magmat,bevel=.003,group='Magazine')
for side in [-1,1]:
    for i in range(4):
        x=.179+i*.027
        profile('Magazine pressed reinforcement',[(x-.005,.009),(x+.005,.009),(x+.008,-.140),(x+.004,-.151),(x-.004,-.151),(x-.007,-.141)],.005,magmat,side*.035,.0012,group='Magazine')
profile('Magazine floor plate',[(.166,-.150),(.298,-.150),(.298,-.168),(.287,-.183),(.173,-.183),(.166,-.174)],.076,steel,bevel=.002,group='Magazine')
for side in [-1,1]:box('Magazine base keeper',(.178,side*.042,-.164),(.020,.010,.024),poly,.002,group='Magazine')

# Open carry bridge and protected iron sights.
ring_profile('Carry bridge',[(-.371,.232),(-.330,.295),(.212,.295),(.234,.275),(.234,.232)],[(-.337,.244),(-.303,.274),(.204,.274),(.213,.260),(.213,.244)],.043,steel,bevel=.002)
for x in [-.307,.207]:
    box('Sight pedestal',(x,0,.293),(.047,.055,.025),steel,.002)
    for side in [-1,1]:
        profile('Sight protective ear',[(x-.020,.298),(x-.018,.346),(x-.010,.359),(x+.007,.359),(x+.018,.340),(x+.018,.298)],.009,steel,side*.022,.001)
        screw(x,.311,side,y=.029,r=.004)
box('Front sight blade',(-.307,0,.324),(.007,.005,.044),steel,.0008)
rear=box('Rear aperture plate',(.207,0,.331),(.009,.026,.039),steel,.001)
c=cylinder('Aperture cut',(.207,0,.337),.0055,.022,recess,axis='X',vertices=24,bevel=0);cut(rear,c)

# Exterior muzzle, collar and utility socket. No bore or functional internals modeled.
cylinder('Forward barrel jacket',(-.472,0,.139),.021,.114,steel,axis='X',vertices=24,bevel=.0015)
cylinder('Front barrel collar',(-.443,0,.139),.029,.040,steel,axis='X',vertices=12,bevel=.002)
muzzle=tube('Slotted muzzle shroud',-.589,-.517,.139,.026,.017,steel)
for angle in [0,math.pi/2,math.pi,math.pi*1.5]:
    c=box('Muzzle vent cut',(-.553,math.sin(angle)*.025,.139+math.cos(angle)*.025),(.045,.009,.026),recess,.003)
    c.rotation_euler.x=-angle
    bpy.context.view_layer.objects.active=c
    for mod in list(c.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
    cut(muzzle,c)
# Dark recessed cap keeps this a closed visual prop.
cylinder('Muzzle recessed dark cap',(-.531,0,.139),.016,.002,recess,axis='X',vertices=24,bevel=0)
tube('Lower front socket',-.476,-.443,.059,.011,.0065,steel,16)
cylinder('Socket dark cap',(-.449,0,.059),.006,.002,recess,axis='X',bevel=0)

# Sling loops and compact exterior controls.
wire_loop('Front sling loop',[(-.442,-.034,.025),(-.446,-.028,.007),(-.446,.028,.007),(-.442,.034,.025),(-.438,.028,.035),(-.438,-.028,.035)],steel,.003)
for side in [-1,1]:
    wire_loop('Rear sling loop',[(.426,side*.069,.013),(.433,side*.069,.020),(.433,side*.069,.092),(.426,side*.069,.099),(.418,side*.069,.092),(.418,side*.069,.020)],steel,.003)
    for z in [.015,.096]:box('Rear sling lug',(.425,side*.056,z),(.017,.020,.016),steel,.001)
    cylinder('Selector pivot',(.039,side*.061,.064),.007,.003,steel,vertices=16,bevel=.001)
    box('Selector lever',(.030,side*.064,.064),(.027,.005,.008),steel,.002)
    box('Magazine release pad',(.126,side*.056,.071),(.028,.007,.013),steel,.002)
# Small charging handle on one side, exterior-only animation hook.
box('Bolt',(.115,.074,.176),(.047,.030,.012),steel,.002,group='Bolt')

# Sparse exposed chips follow handling edges rather than uniformly aging every surface.
chipmat=material('Exposed alloy edge wear',(.29,.30,.255),.68,.57,.1)
rng=random.Random(1707)
for side in [-1,1]:
    verts=[];faces=[]
    for xa,xb,z,yy,count in [(-.400,-.132,.224,.0582,45),(-.09,.038,.224,.0582,20),(.075,.194,.223,.0582,20),(-.388,-.240,.151,.0582,16)]:
        for i in range(count):
            x=rng.uniform(xa,xb);zz=z+rng.uniform(-.002,.001);w=rng.uniform(.0008,.005);h=rng.uniform(.0003,.0012)
            n=len(verts);verts.extend([(x,side*yy,zz),(x+w,side*yy,zz+h*.3),(x+w*.7,side*yy,zz+h),(x-w*.2,side*yy,zz+h*.7)])
            faces.append((n,n+1,n+2,n+3))
    mesh('Scattered cladding edge chips',verts,faces,chipmat,0)

# Match the reference's long, low profile; keep circular hardware circular.
for obj in parts:
    center=(min(v.co.z for v in obj.data.vertices)+max(v.co.z for v in obj.data.vertices))*.5
    for v in obj.data.vertices:
        v.co.z=center*.65+(v.co.z-center)*(1 if obj.get('keep_round') else .65)
    obj.location.z*=.65

# Consistent grip-centred origin for authoring and export metadata.
for obj in parts:obj.location.x-=.077;obj.location.z+=.0507
root=bpy.data.objects.new('MilitiaServiceRifle_EDITABLE',None);source.objects.link(root)
for obj in parts:obj.parent=root
root['asset']='Halo Harvest militia service rifle';root['units']='metres';root['authoring_forward']='-X';root['export_forward']='Unity +Z';root['revision']='01'

# Studio cameras: real renders of this mesh, not generated concept images.
def aim(obj,point):obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
def area(name,location,power,size,color):
    bpy.ops.object.light_add(type='AREA',location=location);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.data.color=color;aim(o,(-.1,0,.14));relocate(o,studio)
area('Large soft key',(-1.0,-1.2,1.8),110,1.4,(1,.92,.82))
area('Cool rear strip',(.7,.7,1.2),140,1.1,(.74,.84,1))
area('Front fill',(-1.4,.5,.4),45,.8,(1,.96,.88))
world=bpy.data.worlds.new('Neutral studio');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.24,.25,.28,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4;scene.world=world
floor_mat=material('Studio floor',(.22,.205,.18),0,.9,0)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.080));floor=bpy.context.object;floor.name='Studio floor';floor.data.materials.append(floor_mat);relocate(floor,studio)
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.threads_mode='FIXED';scene.render.threads=8
scene.render.resolution_x=1600;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='JPEG';scene.render.image_settings.color_mode='RGB';scene.render.image_settings.quality=95
for name,loc,target,scale in [('Hero',(-1.10,-1.80,.78),(-.145,0,.075),1.26),('Side',(-.12,-2,.16),(-.13,0,.10),1.22),('Reverse',(.80,1.8,.7),(-.14,0,.09),1.3),('FirstPerson',(.70,-.20,.40),(-.6,0,.13),1.05)]:
    bpy.ops.object.camera_add(location=loc);cam=bpy.context.object;cam.name='Review '+name;cam.data.type='ORTHO';cam.data.ortho_scale=scale;aim(cam,target);relocate(cam,studio)
scene.camera=bpy.data.objects['Review Hero']
# Leave only the rifle selected when opening the source.
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.region_3d.view_location=Vector((-.14,0,.09));space.region_3d.view_distance=1.25
            space.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion();space.shading.color_type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'MilitiaServiceRifle.blend'),compress=True)
for name in ['Hero','Side','Reverse']:
    scene.camera=bpy.data.objects['Review '+name];scene.render.filepath=str(PREVIEW/(name+'.jpg'));bpy.ops.render.render(write_still=True)
print('SOURCE_READY',len(parts),'parts',flush=True)
