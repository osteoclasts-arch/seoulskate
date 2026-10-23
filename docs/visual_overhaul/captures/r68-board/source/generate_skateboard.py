import math
import os
import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUTPUT = os.path.join(ROOT, 'Assets/SkaterSeoul/Art/Blender/SeoulBoard.fbx')
os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def mat(name, color, roughness=.55, metallic=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF') or m.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    out = m.node_tree.nodes.get('Material Output') or m.node_tree.nodes.new('ShaderNodeOutputMaterial')
    if not any(link.from_node == bsdf and link.to_node == out for link in m.node_tree.links):
        m.node_tree.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    bsdf.inputs['Base Color'].default_value = (*color, 1.0)
    bsdf.inputs['Roughness'].default_value = roughness
    bsdf.inputs['Metallic'].default_value = metallic
    return m


MAPLE = mat('Seoul_Board_Maple', (.56, .32, .16), .5)
PRINT = mat('Hangang_Board_Print', (.90, .37, .19), .48)
GRIP = mat('Seoul_Board_Grip', (.055, .075, .08), .86)
TRUCK = mat('Seoul_Board_Hardware', (.43, .49, .48), .3, .82)
WHEEL = mat('Seoul_Board_Wheel', (.83, .77, .65), .42)
BEARING = mat('Seoul_Board_Bearing', (.12, .15, .16), .3, .78)
BUSHING = mat('Seoul_Board_Bushing', (.12, .17, .18), .72, .0)
BOARD_HALF_LENGTH = .404
BOARD_HALF_WIDTH = .1048
TRUCK_CENTERS = (-.180, .180)
WHEEL_SIDES = (-.0895, .0895)
WHEEL_RADIUS = .027
WHEEL_WIDTH = .031


def deck_point(nx, ny, top=True):
    width = BOARD_HALF_WIDTH * (.78 + .22 * math.sqrt(max(.0, 1.0 - nx * nx)))
    y = ny * width
    edge_cup = .006 * ny * ny
    kick = .034 * (max(0.0, (abs(nx) - .68) / .32) ** 2.0)
    center_camber = .0025 * math.cos(nx * math.pi * .5)
    z = edge_cup + kick + center_camber + (.005 if top else -.005)
    return (nx * BOARD_HALF_LENGTH, y, z)


# A continuous, rounded maple shell with raised nose/tail and concave rails.
length_steps, width_steps = 36, 12
verts, faces, material_ids, uv_data = [], [], [], []
stride = width_steps + 1
for layer in (0, 1):
    for i in range(length_steps + 1):
        nx = 2 * i / length_steps - 1
        for j in range(width_steps + 1):
            ny = 2 * j / width_steps - 1
            verts.append(deck_point(nx, ny, layer == 0))
            # Fit the existing .137–.793 print silhouette to a 3 mm side margin.
            artwork_v = .127 + .676 * j / width_steps if layer == 1 else j / width_steps
            uv_data.append((i / length_steps, artwork_v))
for layer in (0, 1):
    base = layer * stride * (length_steps + 1)
    for i in range(length_steps):
        for j in range(width_steps):
            a = base + i * stride + j
            b, c, d = a + stride, a + 1, a + stride + 1
            if layer == 0:
                faces.append((a, b, d, c)); material_ids.append(0)
            else:
                faces.append((a, c, d, b)); material_ids.append(1)
# Rounded perimeter wall, with maple side ply visible around the deck edge.
base = stride * (length_steps + 1)
perimeter = []
perimeter.extend([j for j in range(stride)])
perimeter.extend([i * stride + width_steps for i in range(1, length_steps + 1)])
perimeter.extend([length_steps * stride + j for j in range(width_steps - 1, -1, -1)])
perimeter.extend([i * stride for i in range(length_steps - 1, 0, -1)])
for i, a0 in enumerate(perimeter):
    b0 = perimeter[(i + 1) % len(perimeter)]
    faces.append((a0, b0, base + b0, base + a0)); material_ids.append(0)
mesh = bpy.data.meshes.new('Concave 8 inch maple skateboard deck')
mesh.from_pydata(verts, [], faces)
mesh.materials.append(MAPLE)
mesh.materials.append(PRINT)
uv = mesh.uv_layers.new(name='Deck print UV')
for poly, material_index in zip(mesh.polygons, material_ids):
    poly.material_index = material_index
    for loop_index in poly.loop_indices:
        vertex_index = mesh.loops[loop_index].vertex_index
        if vertex_index < len(uv_data): uv.data[loop_index].uv = uv_data[vertex_index]
obj = bpy.data.objects.new('Concave maple deck with printed underside', mesh)
bpy.context.collection.objects.link(obj)

# Grippy top sheet follows the deck's concave surface and leaves a clean maple edge.
gv, gf = [], []
for i in range(length_steps + 1):
    nx = 2 * i / length_steps - 1
    for j in range(width_steps + 1):
        ny = (2 * j / width_steps - 1) * .91
        x, y, z = deck_point(nx * .98, ny, True)
        gv.append((x, y, z + .002))
for i in range(length_steps):
    for j in range(width_steps):
        a = i * stride + j
        gf.append((a, a + stride, a + stride + 1, a + 1))
gmesh = bpy.data.meshes.new('Inset griptape following deck concave')
gmesh.from_pydata(gv, [], gf)
gmesh.materials.append(GRIP)
gobj = bpy.data.objects.new('Fine-grain grip tape', gmesh)
bpy.context.collection.objects.link(gobj)


def cube_part(name, location, dimensions, material, bevel=.005):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    o = bpy.context.object
    o.name = name
    o.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material)
    if bevel:
        b = o.modifiers.new('Machined edge', 'BEVEL')
        b.width = bevel
        b.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=b.name)
    return o


def cylinder_part(name, location, radius, depth, material, axis='Y', vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    o = bpy.context.object
    o.name = name
    if axis == 'Y': o.rotation_euler[0] = math.pi / 2
    elif axis == 'X': o.rotation_euler[1] = math.pi / 2
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    o.data.materials.append(material)
    for p in o.data.polygons: p.use_smooth = True
    return o


def strut_part(name, start, end, radius, material):
    direction = Vector(end) - Vector(start)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius, depth=direction.length,
                                        location=(Vector(start) + Vector(end)) * .5)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = direction.to_track_quat('Z', 'Y')
    obj.data.materials.append(material)
    bevel = obj.modifiers.new('Rounded truck edge', 'BEVEL')
    bevel.width = radius * .3
    bevel.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    bpy.ops.object.modifier_apply(modifier='Weighted normals')
    return obj


for truck_index, x in enumerate(TRUCK_CENTERS):
    cube_part('Truck baseplate ' + str(truck_index), (x, 0.0, -.010), (.084, .132, .014), TRUCK, .007)
    cylinder_part('Truck pivot cup ' + str(truck_index), (x, 0.0, -.024), .012, .018, BEARING, 'Z', 16)
    cylinder_part('Truck upper bushing ' + str(truck_index), (x, 0.0, -.034), .014, .012, BUSHING, 'Z', 16)
    cylinder_part('Truck lower bushing ' + str(truck_index), (x, 0.0, -.047), .012, .012, BUSHING, 'Z', 16)
    cube_part('Truck hanger body ' + str(truck_index), (x, 0.0, -.058), (.047, .088, .028), TRUCK, .010)
    for side in (-1, 1):
        strut_part('Truck hanger arm ' + str(truck_index) + ' ' + str(side),
                   (x, 0.0, -.055), (x, side * .079, -.069), .009, TRUCK)
    cylinder_part('Truck axle ' + str(truck_index), (x, 0.0, -.069), .008, .211, TRUCK, 'Y', 20)
    cylinder_part('Truck kingpin ' + str(truck_index), (x, 0.0, -.046), .006, .042, TRUCK, 'Z', 12)
    for bolt_x in (-.027, .027):
        for bolt_y in (-.043, .043):
            cylinder_part('Truck mounting bolt', (x + bolt_x, bolt_y, .0085), .004, .002, BEARING, 'Z', 12)

# Each wheel's object origin is the axle centre so the Unity ride rig can spin the wheel in place.
wheels = []
for x_index, x in enumerate(TRUCK_CENTERS):
    for y_index, y in enumerate(WHEEL_SIDES):
        wheel = cylinder_part('Wheel_' + str(x_index) + '_' + str(y_index), (x, y, -.069), WHEEL_RADIUS, WHEEL_WIDTH, WHEEL, 'Y', 32)
        wheel_bevel = wheel.modifiers.new('Urethane rounded shoulder', 'BEVEL')
        wheel_bevel.width = .003
        wheel_bevel.segments = 3
        bpy.context.view_layer.objects.active = wheel
        bpy.ops.object.modifier_apply(modifier=wheel_bevel.name)
        wheel.modifiers.new('Urethane weighted normals', 'WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier='Urethane weighted normals')
        wheels.append(wheel)
        for side in (-1, 1):
            hub = cylinder_part('Bearing_' + str(x_index) + '_' + str(y_index) + '_' + str(side),
                                (x, y + side * .016, -.069), .0105, .002, BEARING, 'Y', 20)
            hub.parent = wheel
            hub.matrix_parent_inverse = wheel.matrix_world.inverted()
            nut = cylinder_part('Axle nut ' + str(x_index) + '_' + str(y_index) + '_' + str(side),
                                (x, y + side * .020, -.069), .0055, .006, TRUCK, 'Y', 8)
            nut.parent = wheel
            nut.matrix_parent_inverse = wheel.matrix_world.inverted()

assert abs(2 * BOARD_HALF_LENGTH - .8077) < .002
assert abs(2 * BOARD_HALF_WIDTH - .2096) < .001
assert abs(TRUCK_CENTERS[1] - TRUCK_CENTERS[0] - .360) < .001
assert len(wheels) == 4 and all(abs(w.dimensions.x - .054) < .004 and
    abs(w.dimensions.y - WHEEL_WIDTH) < .003 for w in wheels)
assert all(abs(abs(w.location.x) - .180) < .001 and
    abs(abs(w.location.y) - .0895) < .001 for w in wheels)
# Keep wheel transforms separately named; deck, grip and hardware remain static children of the Unity board pivot.
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects: o.select_set(True)
bpy.context.view_layer.objects.active = obj
bpy.ops.export_scene.fbx(filepath=OUTPUT, use_selection=True, object_types={'MESH'},
                         apply_unit_scale=True, use_space_transform=True,
                         axis_forward='-Z', axis_up='Y', bake_space_transform=False,
                         path_mode='AUTO', use_mesh_modifiers=True,
                         mesh_smooth_type='FACE', use_custom_props=False)
print('Exported skateboard:', OUTPUT)
