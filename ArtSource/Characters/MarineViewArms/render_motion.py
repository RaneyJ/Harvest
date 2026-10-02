"""Render actual baked-arm animation at the service rifle's existing 1.8-second reload duration."""
import bpy,math
from pathlib import Path
ROOT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'MarineServiceRifleReview.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['MarineRifleRig'];scene.render.resolution_x=960;scene.render.resolution_y=540
scene.cycles.samples=12;scene.render.image_settings.file_format='PNG'
frames=ROOT/'Review'/'MotionFrames';frames.mkdir(exist_ok=True)
# 20 fps: quiet hold, a single firing cycle, then reload scaled to 1.8 seconds.
poses=[('Idle',i*2) for i in range(12)]+[('Fire',i*1.5) for i in range(5)]+[('Idle',0)]*6+[('Reload',i*2) for i in range(37)]+[('Idle',i*2) for i in range(12)]
for i,(action,frame) in enumerate(poses):
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(int(frame),subframe=frame-int(frame))
 # Match the existing rifle's separate presentation kick for the firing preview.
 base=rig.matrix_world.copy()
 if action=='Fire':
  # Source clip animates fingers/bolt; runtime supplies the existing root recoil.
  from mathutils import Matrix,Vector
  t=frame/30;rig.matrix_world=Matrix.Translation((0,0,.045*math.exp(-18*t)))@base@Matrix.Rotation(math.radians(3*math.exp(-18*t)),4,'X')
 scene.render.filepath=str(frames/f'{i:04d}.png');bpy.ops.render.render(write_still=True);rig.matrix_world=base
print('MOTION_FRAMES',len(poses),flush=True)
