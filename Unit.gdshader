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
varying vec3 emit_color;


void vertex() {
	int index = int(UV2.x);
	int lower = int(INSTANCE_CUSTOM.r);
	int upper = int(INSTANCE_CUSTOM.g);
	emit = index >= 24 ?
		((upper / int(exp2(float(index - 24))) % 2) != 0 ? 1.0 : 0.0) :
		((lower / int(exp2(float(index))) % 2) != 0 ? 1.0 : 0.0);

	int color = int(INSTANCE_CUSTOM.b);
	emit_color.r = float(color / 1 % 256) / 255.0;
	emit_color.g = float(color / 256 % 256) / 255.0;
	emit_color.b = float(color / 65536 % 256) / 255.0;
}




void fragment() {
	vec2 base_uv = UV;
	METALLIC = metallic;
	ROUGHNESS = roughness;
	SPECULAR = specular;
	EMISSION = emit != 0.0 ? emit_color : vec3(0.0);
	ALBEDO = emit != 0.0 ? vec3(0.0) : vec3(0.2);
}
