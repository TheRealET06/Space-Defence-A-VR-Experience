Verdant World - texture maps
========================================

These are the SAME maps already embedded in Verdant_World.glb.
They are here so the .fbx can be textured by hand: FBX has no standard
metallic-roughness slot, so it cannot carry a full PBR set.
If you use the .glb you do not need these files.

  pln_verdant_world_basecolor.png                sRGB colour
  pln_verdant_world_metallic_roughness.png       LINEAR: G=roughness, B=metallic
  pln_verdant_world_normal.png                   LINEAR tangent-space normal

Set the normal map's type to Normal Map in your engine, and make sure the
normal and metallic-roughness maps are LINEAR, not sRGB.
