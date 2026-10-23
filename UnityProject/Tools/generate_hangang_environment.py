import bpy
import math
import random
from mathutils import Vector


random.seed(24)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for data in bpy.data.materials:
    bpy.data.materials.remove(data)


def material(name, color, roughness=0.7, metallic=0.0, emission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
    mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
    shader.inputs['Base Color'].default_value = (*color, 1.0)
    shader.inputs['Roughness'].default_value = roughness
    shader.inputs['Metallic'].default_value = metallic
    if emission:
        shader.inputs['Emission Color'].default_value = (*color, 1.0)
        shader.inputs['Emission Strength'].default_value = emission
    return mat


M = {
    'concrete': material('SC_Concrete', (0.49, 0.51, 0.48)),
    'light_concrete': material('SC_LightConcrete', (0.66, 0.64, 0.57)),
    'edge': material('SC_CopingSteel', (0.22, 0.29, 0.30), 0.3, 0.72),
    'teal': material('SC_DeepTeal', (0.05, 0.28, 0.27), 0.4, 0.18),
    'copper': material('SC_CopperAccent', (0.61, 0.29, 0.14), 0.42, 0.32),
    'paint': material('SC_ParkPaint', (0.79, 0.53, 0.30), 0.72),
    'metal': material('SC_StreetMetal', (0.16, 0.23, 0.24), 0.36, 0.76),
    'wood': material('SC_Teak', (0.28, 0.19, 0.13), 0.56),
    'river': material('SC_River', (0.10, 0.22, 0.25), 0.24, 0.34),
    'glint': material('SC_RiverGlint', (0.72, 0.43, 0.24), 0.35, 0.18),
    'leaf_dark': material('SC_GinkgoDeep', (0.12, 0.23, 0.20)),
    'leaf_light': material('SC_GinkgoLight', (0.43, 0.44, 0.29)),
    'bark': material('SC_GinkgoBark', (0.27, 0.20, 0.14)),
    'lamp': material('SC_LampGlow', (1.0, 0.62, 0.30), 0.25, 0.0, 1.7),
    'bridge': material('SC_BridgeSteel', (0.19, 0.28, 0.31), 0.38, 0.67),
    'hill': material('SC_Hill', (0.27, 0.37, 0.34)),
    'hill_far': material('SC_HillFar', (0.36, 0.43, 0.39)),
    'facade': material('SC_Facade', (0.29, 0.37, 0.40)),
    'facade_warm': material('SC_FacadeWarm', (0.48, 0.38, 0.30)),
    'glass': material('SC_WindowGlass', (0.66, 0.50, 0.34), 0.22, 0.24, 0.18),
}

groups = {}


def register(obj, group, mat=None):
    if mat:
        obj.data.materials.append(mat)
    groups.setdefault(group, []).append(obj)
    return obj


def cube(name, loc, size, mat, group, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    register(obj, group, mat)
    if bevel:
        mod = obj.modifiers.new('Machined radii', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        obj.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.ops.object.modifier_apply(modifier='Weighted corner normals')
    return obj


def tube(name, a, b, radius, mat, group, vertices=12):
    a, b = Vector(a), Vector(b)
    vec = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=vec.length,
                                        location=(a + b) * 0.5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = vec.to_track_quat('Z', 'Y')
    register(obj, group, mat)
    bevel = obj.modifiers.new('Soft machined edge', 'BEVEL')
    bevel.width = radius * 0.22
    bevel.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    bpy.ops.object.modifier_apply(modifier='Weighted normals')
    return obj


def curve(name, points, radius, mat, group):
    data = bpy.data.curves.new(name, 'CURVE')
    data.dimensions = '3D'
    data.resolution_u = 1
    data.bevel_depth = radius
    data.bevel_resolution = 3
    spline = data.splines.new('POLY')
    spline.points.add(len(points) - 1)
    for point, co in zip(spline.points, points):
        point.co = (*co, 1.0)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
    obj = bpy.context.object
    return register(obj, group, mat)


def leaf_cluster(name, x, y, z, scale, group='Ginkgo Trees'):
    verts, faces, leaf_ranges = [], [], []
    for idx in range(7):
        angle = idx * (math.tau / 7.0) + random.uniform(-0.18, 0.18)
        length = scale * random.uniform(0.62, 1.04)
        width = scale * random.uniform(0.32, 0.48)
        base = Vector((x + random.uniform(-0.18, 0.18), y + random.uniform(-0.2, 0.2), z + random.uniform(-0.18, 0.18)))
        axis = Vector((math.sin(angle), random.uniform(-0.26, 0.26), math.cos(angle))).normalized()
        side = Vector((axis.z, 0.0, -axis.x)).normalized()
        start = len(verts)
        first_face = len(faces)
        verts.append(tuple(base))
        for j in range(9):
            t = j / 8.0
            scallop = 1.0 + 0.10 * math.sin(j * math.pi * 1.4)
            spread = math.sin(t * math.pi * 0.74) * width * scallop
            center = base + axis * (length * t)
            verts.extend([tuple(center - side * spread), tuple(center + side * spread)])
        for j in range(8):
            a = start + 1 + j * 2
            c = a + 2
            faces.extend([(start, a, a + 1), (a, c, c + 1, a + 1)])
        leaf_ranges.append((first_face, len(faces), random.choice((0, 0, 0, 1))))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(M['leaf_dark'])
    mesh.materials.append(M['leaf_light'])
    for first, last, material_index in leaf_ranges:
        for poly in mesh.polygons[first:last]:
            poly.material_index = material_index
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    groups.setdefault(group, []).append(obj)
    return obj


def ginkgo(x, y, height=4.0):
    tube('Ginkgo trunk', (x, y, 0.0), (x + 0.1, y, height * 0.75), 0.17, M['bark'], 'Ginkgo Trees', 10)
    for dx, dz in [(-0.75, 2.75), (0.62, 3.0), (-0.18, 3.25), (0.9, 2.75), (-0.9, 3.3)]:
        tube('Ginkgo branch', (x, y, height * 0.58), (x + dx, y, dz), 0.075, M['bark'], 'Ginkgo Trees', 8)
    for dx, dy, dz, scale in [(-0.95, 0.0, 3.35, 0.95), (-0.45, -0.16, 3.8, 0.9),
                              (0.12, 0.03, 4.0, 0.9), (0.72, -0.08, 3.65, 0.92),
                              (1.0, 0.1, 3.25, 0.77), (-0.75, 0.19, 3.05, 0.75),
                              (0.45, 0.23, 3.0, 0.75)]:
        leaf_cluster('Layered fan-shaped ginkgo leaves', x + dx, y + dy, dz, scale)


def extruded_silhouette(name, points, y, depth, mat, group):
    verts = [(x, y, z) for x, z in points] + [(x, y + depth, z) for x, z in points]
    n = len(points)
    faces = [tuple(range(n)), tuple(range(n, 2 * n))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mat)
    for poly in mesh.polygons:
        poly.use_smooth = False
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    groups.setdefault(group, []).append(obj)
    return obj


# Front skate features: durable poured concrete, rounded impact edges, steel coping, and paint.
cube('Gameplay Ledge Visual', (3.2, 0.0, 0.13), (2.3, 0.92, 0.26), M['light_concrete'], 'Skate Obstacles', 0.045)
cube('Ledge continuous steel coping', (3.2, -0.005, 0.282), (2.34, 0.96, 0.035), M['edge'], 'Skate Obstacles', 0.017)
cube('Ledge front teal reveal', (3.2, -0.466, 0.105), (2.18, 0.012, 0.035), M['teal'], 'Skate Obstacles', 0.008)
for x in (2.17, 4.23):
    for z in (0.10, 0.17):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, radius=0.014, location=(x, -0.476, z))
        register(bpy.context.object, 'Skate Obstacles', M['copper'])

# A low manual pad and a rounded street rail deepen the rideable foreground without cluttering the main line.
cube('Manual Pad Visual', (-4.25, 1.05, 0.11), (1.45, 0.82, 0.22), M['concrete'], 'Skate Obstacles', 0.05)
cube('Manual pad top coping', (-4.25, 1.05, 0.235), (1.48, 0.85, 0.025), M['edge'], 'Skate Obstacles', 0.012)
cube('Manual pad painted end mark', (-4.25, 0.63, 0.12), (0.46, 0.014, 0.055), M['paint'], 'Skate Obstacles', 0.008)
rail_offset = -2.4
curve('Street rail with formed ends', [(-1.55 + rail_offset, 1.45, 0.0), (-1.55 + rail_offset, 1.45, 0.25),
                                      (-1.38 + rail_offset, 1.45, 0.52), (1.1 + rail_offset, 1.45, 0.52),
                                      (1.27 + rail_offset, 1.45, 0.25), (1.27 + rail_offset, 1.45, 0.0)],
      0.023, M['edge'], 'Skate Obstacles')
for index, x in enumerate((-1.38, -0.48, 0.42, 1.27)):
    x += rail_offset
    tube('Rail welded upright', (x, 1.45, 0.06), (x, 1.45, 0.28 if index in (0, 3) else 0.42),
         0.021, M['metal'], 'Skate Obstacles', 12)
cube('Concrete curb extension', (14.0, 0.0, 0.12), (2.2, 0.85, 0.24), M['concrete'], 'Skate Obstacles', 0.05)
cube('Curb chamfer stripe', (14.0, -0.43, 0.12), (1.9, 0.014, 0.028), M['copper'], 'Skate Obstacles', 0.006)

# Distinct foreground plaza edge and a quieter riverside promenade behind the playable strip.
cube('Promenade stone ribbon', (16.0, 5.25, -0.05), (120.0, 3.0, 0.15), M['light_concrete'], 'Riverside Walk', 0.025)
cube('Promenade dark expansion joint', (16.0, 3.82, 0.035), (120.0, 0.045, 0.015), M['edge'], 'Riverside Walk', 0.006)
cube('River retaining cap', (16.0, 7.05, 0.04), (120.0, 0.34, 0.22), M['concrete'], 'Riverside Walk', 0.04)

# A narrow, raised river band reads from the side camera without filling the foreground.
cube('River Visual Band', (16.0, 13.5, 0.18), (130.0, 0.04, 0.24), M['river'], 'Han River')
for i in range(13):
    x = -40.0 + i * 8.8
    length = random.uniform(2.2, 5.8)
    z = random.uniform(0.08, 0.28)
    cube('Broken sunset water glint', (x, 13.475, z), (length, 0.012, 0.012), M['glint'], 'Han River', 0.004)

# Promenade railing with repeated posts and deliberate spacing, kept behind the trick lane.
for x in range(-40, 63, 4):
    tube('Riverside baluster', (x, 6.35, 0.10), (x, 6.35, 0.82), 0.036, M['metal'], 'Riverside Railing', 10)
    cube('Railing foot plate', (x, 6.35, 0.10), (0.20, 0.22, 0.04), M['copper'], 'Riverside Railing', 0.015)
for z in (0.38, 0.82):
    tube('Continuous promenade rail', (-42, 6.35, z), (62, 6.35, z), 0.038 if z > 0.6 else 0.03, M['metal'], 'Riverside Railing', 12)
for x in range(-40, 60, 8):
    cube('River path wayfinding marker', (x, 4.0, 0.085), (0.85, 0.16, 0.018), M['teal'], 'Riverside Walk', 0.018)

# Thin Seoul promenade lamps with warm compact luminaires.
for x in (-12.0, 22.0, 39.0):
    tube('Riverside lamp post', (x, 5.9, 0.1), (x, 5.9, 4.7), 0.055, M['metal'], 'Street Lights', 12)
    tube('Lamp offset arm', (x, 5.9, 4.55), (x + 0.48, 5.9, 4.72), 0.043, M['copper'], 'Street Lights', 10)
    cube('Warm LED head', (x + 0.55, 5.9, 4.73), (0.42, 0.22, 0.12), M['lamp'], 'Street Lights', 0.055)
    cube('Lamp shield', (x + 0.55, 5.9, 4.79), (0.51, 0.27, 0.045), M['metal'], 'Street Lights', 0.018)

# Sparse ginkgo trees and planters; individual fan leaves preserve readable silhouettes.
for x, y, h in [(-18.0, 8.0, 3.75), (18.0, 8.2, 3.9), (34.0, 8.1, 4.2), (48.0, 8.4, 3.8)]:
    cube('Ginkgo planter', (x, y, 0.22), (1.25, 0.96, 0.44), M['concrete'], 'Ginkgo Trees', 0.11)
    ginkgo(x, y, h)

# The bridge is a recognizable repeated Hangang truss silhouette against the river skyline.
bridge_y = 17.2
cube('Hangang bridge deck', (15.0, bridge_y, 1.39), (76.0, 0.52, 0.12), M['bridge'], 'Hangang Bridge', 0.025)
cube('Bridge warm parapet line', (15.0, bridge_y - 0.28, 1.48), (76.0, 0.06, 0.07), M['copper'], 'Hangang Bridge', 0.018)
for start in (-22.0, -3.0, 16.0, 35.0):
    end = start + 18.0
    arc = []
    for i in range(17):
        t = i / 16
        arc.append((start + (end - start) * t, bridge_y, 1.48 + 0.48 * math.sin(math.pi * t)))
    curve('Bridge shallow steel arch', arc, 0.045, M['bridge'], 'Hangang Bridge')
    for i in range(1, 8):
        t = i / 8
        x = start + (end - start) * t
        z = 1.48 + 0.48 * math.sin(math.pi * t)
        tube('Bridge vertical hanger', (x, bridge_y, 1.45), (x, bridge_y, z), 0.018, M['bridge'], 'Hangang Bridge', 8)
    tube('Bridge lower truss', (start, bridge_y, 1.24), (end, bridge_y, 1.24), 0.032, M['bridge'], 'Hangang Bridge', 10)
    for x in (start, end):
        cube('Bridge pier', (x, bridge_y, 0.98), (0.16, 0.62, 0.82), M['bridge'], 'Hangang Bridge', 0.025)

# Layered, low skyline contours sit just below the panorama's Seoul horizon.
hill_points = [(-70, 0.92), (-54, 1.04), (-40, 1.38), (-27, 1.12), (-16, 1.52), (-4, 1.30),
               (10, 1.08), (24, 1.60), (38, 1.18), (53, 1.45), (70, 1.10), (70, 0.92), (-70, 0.92)]
extruded_silhouette('Achasan low contour', hill_points, 34.0, 0.55, M['hill'], 'Far Seoul Hills')
far_points = [(-72, 0.96), (-56, 1.12), (-44, 1.62), (-32, 1.22), (-19, 1.72), (-8, 1.28),
              (6, 1.82), (20, 1.30), (35, 1.70), (49, 1.20), (68, 1.62), (72, 1.25), (72, 0.96), (-72, 0.96)]
extruded_silhouette('Namsan distant ridge', far_points, 37.5, 0.45, M['hill_far'], 'Far Seoul Hills')

for i, x in enumerate(range(-52, 61)):
    x += random.uniform(-0.18, 0.18)
    w = random.uniform(0.24, 0.68)
    h = random.uniform(0.30, 0.82)
    depth = random.uniform(0.08, 0.22)
    base = 0.28 + random.uniform(-0.05, 0.05)
    y = 31.0 + random.uniform(-0.4, 0.4)
    mat = M['facade_warm'] if i % 5 == 1 else M['facade']
    cube('Seoul skyline tower', (x, y, base + h * 0.5), (w, depth, h), mat, 'Far Seoul Skyline', 0.05)
    # Sparse lit window bands stay broad enough to read at the side camera distance.
    if i % 3 == 0 and h > 0.62:
        for floor in (0.26, 0.48, 0.70):
            if floor < h - 0.08:
                cube('Warm tower window strip', (x, y - depth * 0.51, base + floor),
                     (w * 0.68, 0.012, 0.025), M['glass'], 'Far Seoul Skyline', 0.004)

# Collapse decorative parts into a handful of material-preserving meshes for mobile draw calls.
for group, objects in groups.items():
    meshes = [obj for obj in objects if obj.type == 'MESH']
    if len(meshes) <= 1:
        continue
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    bpy.context.object.name = group

# Keep the rideable ledge mesh as a distinct named child; Unity supplies the gameplay collider.
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.context.scene.objects:
    if obj.type == 'MESH':
        obj.select_set(True)
bpy.context.view_layer.objects.active = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')

out = bpy.path.abspath('//Assets/SkaterSeoul/Art/Blender/HangangRiversideSet.fbx')
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'MESH'},
                         apply_unit_scale=True, use_space_transform=True,
                         axis_forward='-Z', axis_up='Y', bake_space_transform=False,
                         add_leaf_bones=False, path_mode='AUTO',
                         use_mesh_modifiers=True, mesh_smooth_type='FACE',
                         use_custom_props=False)
print('Exported Hangang environment:', out)
