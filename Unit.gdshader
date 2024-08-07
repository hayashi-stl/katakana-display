// NOTE: Shader automatically converted from Godot Engine 3.5.stable.mono's SpatialMaterial.

shader_type spatial;
render_mode async_visible,blend_mix,depth_draw_opaque,cull_back,diffuse_burley,specular_schlick_ggx;
uniform vec4 albedo : hint_color;
uniform float specular;
uniform float metallic;
uniform float roughness : hint_range(0,1);
uniform float point_size : hint_range(0,128);
uniform vec3 uv1_scale;
uniform vec3 uv1_offset;
uniform vec3 uv2_scale;
uniform vec3 uv2_offset;
varying float emit;


void vertex() {
	int index = int(round(UV2.x));
	int lower = int(round(INSTANCE_CUSTOM.r));
	int upper = int(round(INSTANCE_CUSTOM.g));
	emit = index >= 24 ?
		((upper / int(exp2(float(index - 24))) % 2) != 0 ? 1.0 : 0.0) :
		((lower / int(exp2(float(index))) % 2) != 0 ? 1.0 : 0.0);
}




void fragment() {
	vec2 base_uv = UV;
	METALLIC = metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
	EMISSION = emit != 0.0 ? vec3(0.5, 0.5, 1.0) : vec3(0.0);
	ALBEDO = emit != 0.0 ? vec3(0.0) : vec3(0.2);
}
