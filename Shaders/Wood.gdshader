shader_type spatial;
render_mode skip_vertex_transform;

void vertex() {
	VERTEX = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	// Called for every vertex the material is visible on.
}

void fragment() {
	float light = dot(NORMAL, normalize(vec3(1, 3, 1)));
	ALBEDO = mix(vec3(0.2, 0.08, 0.0), vec3(0.4,0.2,0.05), light);
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
