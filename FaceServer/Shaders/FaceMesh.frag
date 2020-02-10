#version 150 compatibility
in vec3 vTexCoord;
void main()
{
	gl_FragColor = vec4(vTexCoord.xy,1,1) * 0.5;
}