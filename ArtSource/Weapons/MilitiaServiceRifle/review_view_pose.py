"""Perspective camera study of the baked export using Harvest's camera/pose values.
This is a Blender projection study, not a Unity screenshot or traversal test.
"""
import bpy,math,json
import numpy as np
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parent
GAME=ROOT.parents[2]/'Assets/Harvest/Art/Weapons/MilitiaServiceRifle'
pose=json.loads((GAME/'ViewPose.json').read_text())
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(ROOT/'MilitiaServiceRifle.glb'))
scene=bpy.context.scene;root=bpy.data.objects['MilitiaServiceRifle']
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
# FBX/GLB are exported with Blender -Y forward, corresponding to the declared Unity +Z.
to_unity=Matrix(((1,0,0,0),(0,0,1,0),(0,-1,0,0),(0,0,0,1)))
to_camera=Matrix.Diagonal((1,1,-1,1))
position=Vector(tuple(pose['Position'][c] for c in 'xyz'))
base=Matrix.Rotation(math.radians(pose['Euler']['y']),4,'Y')@Matrix.Rotation(math.radians(pose['Euler']['x']),4,'X')@Matrix.Rotation(math.radians(pose['Euler']['z']),4,'Z')
vertices=[]
for obj in meshes:vertices.extend([obj.matrix_world@v.co for v in obj.data.vertices])
points=np.array([[*v,1] for v in vertices],dtype=np.float64)
records=[]
for name,kick,pitch,reload,thrust in [('Rest',0,0,0,0),('Maximum existing recoil',.12,12,0,0),('Reload low pose',0,0,1,0),('Melee extension',0,0,0,.18)]:
    matrix=Matrix.Translation(position+Vector((0,-reload*.18,thrust-kick)))@base@Matrix.Rotation(math.radians(-pitch+reload*22),4,'X')@Matrix.Rotation(math.radians(-reload*12),4,'Z')@to_unity
    p=points@np.array(matrix).T;depth=p[:,2]
    assert depth.min()>pose['NearClip']+.01,(name,depth.min())
    f=math.tan(math.radians(pose['VerticalFov']/2));x=.5+p[:,0]/(2*depth*f*pose['Aspect']);y=.5+p[:,1]/(2*depth*f)
    record={'state':name,'nearest_depth_m':round(float(depth.min()),4),'viewport_bounds':[round(float(x.min()),4),round(float(y.min()),4),round(float(x.max()),4),round(float(y.max()),4)]}
    if name=='Rest':assert x.min()>.51,'Rest silhouette intersects central aiming column'
    records.append(record)
(ROOT/'view_pose_validation.json').write_text(json.dumps({'camera':pose,'checks':records,'scope':'Blender projection and current presentation envelopes; Unity rendering remains unverified'},indent=2))
root.matrix_world=to_camera@Matrix.Translation(position)@base@to_unity
bpy.context.view_layer.update()

bpy.ops.object.camera_add(location=(0,0,0));camera=bpy.context.object;camera.name='Harvest 75-degree perspective study';camera.rotation_euler=(0,0,0)
camera.data.type='PERSP';camera.data.sensor_fit='VERTICAL';camera.data.sensor_height=24
camera.data.lens=24/(2*math.tan(math.radians(pose['VerticalFov']/2)))
camera.data.clip_start=pose['NearClip'];scene.camera=camera
scene.render.resolution_x=1600;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.cycles.device='CPU'
scene.render.threads_mode='FIXED';scene.render.threads=8;scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='JPEG';scene.render.image_settings.color_mode='RGB';scene.render.image_settings.quality=95
world=bpy.data.worlds.new('Neutral view study');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.21,.23,.24,1);world.node_tree.nodes['Background'].inputs[1].default_value=.6;scene.world=world

def material(name,color,emission=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;bs=n.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.85
 if emission:bs.inputs['Emission Color'].default_value=(*color,1);bs.inputs['Emission Strength'].default_value=1
 return m

def cube(name,location,size,mat):
 bpy.ops.mesh.primitive_cube_add(size=1,location=location);o=bpy.context.object;o.name=name;o.scale=size;o.data.materials.append(mat);return o
floor=material('Neutral ground',(.095,.10,.085));block=material('Scale blocks',(.22,.23,.21));mark=material('Review marker',(.85,.84,.76),True)
cube('Neutral ground',(0,-1.72,-10),(200,.04,200),floor)
for x,z in [(-2,-6),(2.2,-8),(-3.2,-12)]:cube('One metre scale block',(x,-1.2,z),(1,1,1),block)
for name,loc,power,size in [('Camera left softbox',(-1,1,-.3),70,2),('Camera right fill',(1,.5,-.6),35,1.8),('Forward edge light',(0,1,-2),65,1.4)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.size=size;o.rotation_euler=(Vector((.25,-.3,-.9))-o.location).to_track_quat('-Z','Y').to_euler()
for scale in [(.012,.0013,.0002),(.0013,.012,.0002)]:cube('Centre reference',(0,0,-.8),scale,mark)
bpy.ops.object.text_add(location=(-2.34,1.30,-2));text=bpy.context.object;text.data.body='CAMERA FIT STUDY  /  75 deg vertical  /  16:9';text.data.size=.045;text.data.materials.append(mark)
scene.render.filepath=str(ROOT/'Review'/'FirstPerson.jpg');bpy.ops.render.render(write_still=True)
print(json.dumps(records),flush=True)
