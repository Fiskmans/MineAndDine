shader_type spatial;
render_mode skip_vertex_transform;

uniform float Size;
uniform ivec3 Offset;

uniform sampler3D Color;

varying vec3 WorldNormal;
varying vec3 WorldPosition;
varying vec3 UV3;

vec2 random(vec2 uv)
{
    uv = vec2( dot(uv, vec2(127.1, 311.7)),
               dot(uv, vec2(269.5, 183.3)));
//			   dot(uv, vec3(211.9, 115.1, 260.7)) );
    return -1.0 + 2.0 * fract(sin(uv) * 43758.5453123);
}

float perlinNoise(vec3 uv)
{
    vec2 uv_index = floor(uv).xz;
    vec2 uv_fract = fract(uv).xz;

    vec2 blur = smoothstep(0.0, 1.0, uv_fract);

    return mix( mix( dot( random(uv_index + vec2(0.0,0.0)), uv_fract - vec2(0.0,0.0)),
                     dot( random(uv_index + vec2(1.0,0.0)), uv_fract - vec2(1.0,0.0)), blur.x),
                mix( dot( random(uv_index + vec2(0.0,1.0)), uv_fract - vec2(0.0,1.0)),
                     dot( random(uv_index + vec2(1.0,1.0)), uv_fract - vec2(1.0,1.0)), blur.x), blur.y) + 0.5;
}

void vertex() {
	UV3 = VERTEX / Size;
	WorldPosition = VERTEX + vec3(Offset) * Size;
	WorldNormal = NORMAL;

	VERTEX = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	NORMAL = (MODELVIEW_MATRIX * vec4(NORMAL, 0.0)).xyz;
	// Called for every vertex the material is visible on.
}

void fragment() {
	ALBEDO = texture(Color, UV3.xyz).rgb;

}

//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
