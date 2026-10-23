import math
import os
import bpy

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SOURCE = os.path.join(ROOT, 'Assets/SkaterSeoul/ThirdParty/QuaterniusUltimateModularMen/Casual_Hoodie.fbx')
OUTPUT = os.path.join(ROOT, 'Assets/SkaterSeoul/Art/Blender/SeoulSkater.fbx')
os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=SOURCE)
armature = next(obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE')
meshes = {obj.name: obj for obj in bpy.context.scene.objects if obj.type == 'MESH'}
for obj in list(bpy.context.scene.objects):
    if obj.type == 'MESH' and obj.name not in ('Casual_Head', 'Casual_Feet', 'Casual_Legs', 'Casual_Body'):
        bpy.data.objects.remove(obj, do_unlink=True)


def make_material(name, rgba, roughness=0.76):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get('Principled BSDF') or nodes.new('ShaderNodeBsdfPrincipled')
    output = nodes.get('Material Output') or nodes.new('ShaderNodeOutputMaterial')
    if not any(link.from_node == bsdf and link.to_node == output for link in mat.node_tree.links):
        mat.node_tree.links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Roughness'].default_value = roughness
    return mat


hoodie = make_material('SeoulSkater_Hoodie_DeepTeal', (0.12, 0.25, 0.30, 1.0))
hoodie_trim = make_material('SeoulSkater_Hoodie_Trim', (0.20, 0.33, 0.35, 1.0))
zipper = make_material('SeoulSkater_BrushedSteel_Zipper', (0.48, 0.52, 0.49, 1.0), 0.42)
denim = make_material('SeoulSkater_Indigo_Denim', (0.07, 0.15, 0.28, 1.0))
denim_stitch = make_material('SeoulSkater_Denim_Stitch', (0.30, 0.43, 0.57, 1.0))
shoe_upper = make_material('SeoulSkater_Skate_Shoe', (0.12, 0.16, 0.18, 1.0), 0.82)
shoe_sole = make_material('SeoulSkater_Skate_Sole', (0.77, 0.74, 0.66, 1.0), 0.68)
hair = make_material('SeoulSkater_Hair_Charcoal', (0.025, 0.032, 0.04, 1.0), 0.88)

body = meshes['Casual_Body']
body.data.materials[0] = hoodie
hoodie_vertices = {index for polygon in body.data.polygons if polygon.material_index == 0 for index in polygon.vertices}
for index in hoodie_vertices:
    vertex = body.data.vertices[index]
    if 1.04 <= vertex.co.z <= 1.48 and abs(vertex.co.x) < .22:
        fullness = math.sin(math.pi * (vertex.co.z - 1.04) / .44)
        vertex.co.x *= 1.0 + .30 * fullness
        vertex.co.y *= 1.0 + .22 * fullness
    elif 1.04 <= vertex.co.z <= 1.48 and abs(vertex.co.x) >= .22:
        vertex.co.x *= 1.19
        vertex.co.y *= 1.10
    if vertex.co.z > 1.42 and vertex.co.y > -.015 and abs(vertex.co.x) < .29:
        vertex.co.x *= .62
        vertex.co.y = -.015 + (vertex.co.y + .015) * .62
head = meshes['Casual_Head']
head.data.materials[1] = hair
head.data.materials[3] = hair
for vertex in head.data.vertices:
    vertex.co.x *= .85
    vertex.co.y = -.043 + (vertex.co.y + .043) * .9
    vertex.co.z = 1.715 + (vertex.co.z - 1.715) * .84
hair_vertices = {index for polygon in head.data.polygons if polygon.material_index == 3 for index in polygon.vertices}
for index in hair_vertices:
    vertex = head.data.vertices[index]
    vertex.co.x *= .94
    vertex.co.y = -.043 + (vertex.co.y + .043) * .94
    vertex.co.z = 1.70 + (vertex.co.z - 1.70) * .82
feet = meshes['Casual_Feet']
feet.data.materials[0] = shoe_upper
feet.data.materials[1] = shoe_sole
legs = meshes['Casual_Legs']
bpy.data.objects.remove(legs, do_unlink=True)


def weighted_mesh(name, vertices, faces, material, weight_fn, smooth=True):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.parent = armature
    for vertex in vertices:
        for bone_name in weight_fn(vertex[2]):
            if armature.data.bones.get(bone_name) and not obj.vertex_groups.get(bone_name):
                obj.vertex_groups.new(name=bone_name)
    group_map = {group.name: group for group in obj.vertex_groups}
    for index, vertex in enumerate(vertices):
        for bone_name, weight in weight_fn(vertex[2]).items():
            if weight > 0 and bone_name in group_map:
                group_map[bone_name].add([index], weight, 'REPLACE')
    mod = obj.modifiers.new('Bind to skater armature', 'ARMATURE')
    mod.object = armature
    mod.use_deform_preserve_volume = True
    for poly in mesh.polygons:
        poly.use_smooth = smooth
    return obj


def ellipsoid_mesh(name, center, radii, material, bone_names, segments=16, rings=10):
    vertices, faces = [], []
    for r in range(rings + 1):
        phi = -math.pi / 2 + math.pi * r / rings
        for s in range(segments):
            theta = math.tau * s / segments
            vertices.append((center[0] + radii[0] * math.cos(phi) * math.cos(theta),
                             center[1] + radii[1] * math.cos(phi) * math.sin(theta),
                             center[2] + radii[2] * math.sin(phi)))
    for r in range(rings):
        for s in range(segments):
            a = r * segments + s
            b = r * segments + (s + 1) % segments
            c = (r + 1) * segments + (s + 1) % segments
            d = (r + 1) * segments + s
            faces.append((a, b, c, d))
    weights = {bone_names[0]: 1.0} if len(bone_names) == 1 else {bone_names[0]: .7, bone_names[1]: .3}
    return weighted_mesh(name, vertices, faces, material, lambda z: weights)


def tube_curve(name, points, radius, material, bone_names, resolution=6):
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.resolution_u = 1
    curve.bevel_depth = radius
    curve.bevel_resolution = 2
    spline = curve.splines.new('POLY')
    spline.points.add(len(points) - 1)
    for p, point in zip(spline.points, points):
        p.co = (*point, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
    obj = bpy.context.object
    obj.parent = armature
    for bone_name in bone_names:
        obj.vertex_groups.new(name=bone_name)
    if len(bone_names) == 1:
        for vertex in range(len(obj.data.vertices)):
            obj.vertex_groups[bone_names[0]].add([vertex], 1.0, 'REPLACE')
    else:
        obj.vertex_groups[bone_names[0]].add(list(range(len(obj.data.vertices))), .7, 'REPLACE')
        obj.vertex_groups[bone_names[1]].add(list(range(len(obj.data.vertices))), .3, 'REPLACE')
    mod = obj.modifiers.new('Bind jacket piping', 'ARMATURE')
    mod.object = armature
    return obj


def loose_denim_leg(side, x, side_sign):
    rings = [(.06, .088, .096), (.10, .096, .105), (.14, .094, .104), (.20, .105, .108),
             (.29, .122, .118), (.39, .132, .126), (.49, .138, .131), (.58, .139, .134),
             (.68, .138, .132), (.78, .138, .136), (.88, .136, .132), (.96, .128, .125), (1.02, .118, .120)]
    vertices, faces = [], []
    segments = 24
    for z, radius_x, radius_y in rings:
        for segment in range(segments):
            angle = math.tau * segment / segments
            vertices.append((x + radius_x * math.cos(angle), -.055 + radius_y * math.sin(angle), z))
    for ring in range(len(rings) - 1):
        for segment in range(segments):
            a = ring * segments + segment
            b = ring * segments + (segment + 1) % segments
            c = (ring + 1) * segments + (segment + 1) % segments
            d = (ring + 1) * segments + segment
            faces.append((a, b, c, d))

    def weights(z):
        upper, lower = 'UpperLeg.' + side, 'LowerLeg.' + side
        if z < .45: return {lower: 1.0}
        if z < .61:
            upper_weight = (z - .45) / .16
            return {lower: 1.0 - upper_weight, upper: upper_weight}
        if z < .77: return {upper: 1.0}
        t = max(0.0, min(1.0, (z - .77) / .25))
        hips_weight = .65 * t * t * (3.0 - 2.0 * t)
        return {upper: 1.0 - hips_weight, 'Hips': hips_weight}

    assert all(abs(sum(weights(z).values()) - 1.0) < 1e-6 for z in (.06, .533, .70, .82, .94, 1.02))
    def surface_point(z, angle=-.70):
        ring_index = next(i for i in range(len(rings) - 1) if rings[i][0] <= z <= rings[i + 1][0])
        lower, upper = rings[ring_index], rings[ring_index + 1]
        blend = (z - lower[0]) / (upper[0] - lower[0])
        radius_x = lower[1] + (upper[1] - lower[1]) * blend
        radius_y = lower[2] + (upper[2] - lower[2]) * blend
        return (x + side_sign * radius_x * math.cos(angle), -.055 + radius_y * math.sin(angle), z)

    leg = weighted_mesh('Loose denim leg ' + side, vertices, faces, denim, weights)
    tube_curve('Denim stitch lower seam ' + side, [surface_point(z) for z in (.14, .32, .49)],
               .0025, denim_stitch, ('LowerLeg.' + side,))
    tube_curve('Denim stitch upper seam ' + side, [surface_point(z) for z in (.56, .66, .75)],
               .0025, denim_stitch, ('UpperLeg.' + side,))
    return leg


def denim_hip():
    # A flatter, tapered seat avoids the spherical silhouette while keeping the loose upper-leg volume.
    rings = [(.71, .18, .086, .055), (.75, .215, .103, .065), (.82, .237, .109, .072),
             (.89, .226, .102, .067), (.95, .204, .087, .058), (.985, .185, .073, .050)]
    assert all(rings[i][0] < rings[i + 1][0] for i in range(len(rings) - 1))
    vertices, faces = [], []
    segments = 24
    for z, radius_x, front_depth, back_depth in rings:
        for segment in range(segments):
            angle = math.tau * segment / segments
            sin_angle = math.sin(angle)
            depth = front_depth if sin_angle >= 0 else back_depth
            vertices.append((radius_x * math.cos(angle), -.015 - depth * sin_angle, z))
    for ring in range(len(rings) - 1):
        for segment in range(segments):
            a = ring * segments + segment
            b = ring * segments + (segment + 1) % segments
            c = (ring + 1) * segments + (segment + 1) % segments
            d = (ring + 1) * segments + segment
            faces.append((a, b, c, d))
    return weighted_mesh('Tailored denim hip and waistband', vertices, faces, denim, lambda z: {'Hips': 1.0})


for side, sign in [('L', 1.0), ('R', -1.0)]:
    center_x = sign * .121
    loose_denim_leg(side, center_x, sign)
denim_hip()

hood_rings = [(1.35, .060, .022), (1.39, .075, .032), (1.43, .080, .038), (1.47, .073, .034), (1.50, .060, .025)]
hood_vertices, hood_faces = [], []
hood_segments = 12
for z, radius_x, radius_y in hood_rings:
    for segment in range(hood_segments + 1):
        angle = math.pi * segment / hood_segments
        hood_vertices.append((radius_x * math.cos(angle), .015 + radius_y * math.sin(angle), z))
for ring in range(len(hood_rings) - 1):
    for segment in range(hood_segments):
        a = ring * (hood_segments + 1) + segment
        b = a + 1
        c = b + hood_segments + 1
        d = a + hood_segments + 1
        hood_faces.append((a, b, c, d))

def hood_weights(z):
    head_weight = max(0.0, min(1.0, (z - 1.46) / .20))
    return {'Neck': 1.0 - head_weight, 'Head': head_weight}

weighted_mesh('Attached zip hoodie back hood', hood_vertices, hood_faces, hoodie, hood_weights)

cap_vertices, cap_faces = [], []
cap_segments, cap_rings = 20, 8
for ring in range(cap_rings + 1):
    phi = math.pi * .5 * ring / cap_rings
    for segment in range(cap_segments):
        theta = math.tau * segment / cap_segments
        cap_vertices.append((.115 * math.cos(phi) * math.cos(theta),
                             -.043 + .105 * math.cos(phi) * math.sin(theta),
                             1.705 + .115 * math.sin(phi)))
for ring in range(cap_rings):
    for segment in range(cap_segments):
        a = ring * cap_segments + segment
        b = ring * cap_segments + (segment + 1) % cap_segments
        c = (ring + 1) * cap_segments + (segment + 1) % cap_segments
        d = (ring + 1) * cap_segments + segment
        cap_faces.append((a, b, c, d))
weighted_mesh('Short textured crop hairstyle', cap_vertices, cap_faces, hair, lambda z: {'Head': 1.0})
zipper_x = 0.0
tube_curve('Metal jacket center zip', [(zipper_x, -.154, 1.46), (zipper_x, -.184, 1.31),
                                       (zipper_x, -.198, 1.13), (zipper_x, -.188, 1.03)],
           .003, zipper, ('Torso',))
for side in (-1, 1):
    tube_curve('Hoodie zip placket seam ' + str(side),
               [(side * .022, -.156, 1.44), (side * .022, -.176, 1.25),
                (side * .020, -.194, 1.06)], .0025, hoodie_trim, ('Torso',))
for side in (-1, 1):
    tube_curve('Hoodie drawstring ' + str(side),
               [(side * .045, -.156, 1.44), (side * .05, -.159, 1.40), (side * .058, -.164, 1.36)],
               .0025, hoodie_trim, ('Torso',))
    tube_curve('Zip hoodie pocket seam ' + str(side),
               [(side * .035, -.190, 1.16), (side * .105, -.174, 1.13), (side * .17, -.134, 1.16)],
               .0025, hoodie_trim, ('Torso',))

# Thicker skate shoes: a light sole and a dark suede/leather upper, each following the foot bone.
for side, x in [('L', .121), ('R', -.121)]:
    ellipsoid_mesh('Skate shoe vulcanized sole ' + side, (x, -.145, -.028), (.123, .205, .052), shoe_sole,
                   ('Foot.' + side,), 20, 8)
    ellipsoid_mesh('Skate shoe dark upper ' + side, (x, -.145, .018), (.112, .19, .076), shoe_upper,
                   ('Foot.' + side,), 20, 10)
    tube_curve('Skate shoe side stripe ' + side,
               [(x + (.075 if x > 0 else -.075), -.24, .035),
                (x + (.085 if x > 0 else -.085), -.16, .04),
                (x + (.075 if x > 0 else -.075), -.08, .035)],
               .008, shoe_sole, ('Foot.' + side,))

# Remove imported default helpers and export the existing skeleton plus the replacement clothing.
for obj in list(bpy.context.scene.objects):
    if obj.type not in {'ARMATURE', 'MESH'}:
        bpy.data.objects.remove(obj, do_unlink=True)
assert armature.data.bones.get('Hips') and armature.data.bones.get('UpperLeg.L') and armature.data.bones.get('Foot.R')
assert all(obj.parent == armature for obj in bpy.context.scene.objects if obj.type == 'MESH')
assert len([obj for obj in bpy.context.scene.objects if obj.name.startswith('Loose denim leg')]) == 2
assert any(obj.name == 'Attached zip hoodie back hood' for obj in bpy.context.scene.objects)
assert any(obj.name == 'Short textured crop hairstyle' for obj in bpy.context.scene.objects)
bpy.ops.object.select_all(action='DESELECT')
armature.select_set(True)
for obj in bpy.context.scene.objects:
    if obj.type == 'MESH': obj.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.fbx(filepath=OUTPUT, use_selection=True, object_types={'ARMATURE', 'MESH'},
                         apply_unit_scale=True, use_space_transform=True,
                         axis_forward='-Z', axis_up='Y', bake_space_transform=False,
                         add_leaf_bones=False, path_mode='AUTO', use_mesh_modifiers=True,
                         mesh_smooth_type='FACE', use_custom_props=False, bake_anim=False)
print('Exported skater:', OUTPUT)
