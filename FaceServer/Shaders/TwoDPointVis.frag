#version 150 compatibility
in vec3 vTexCoord;
void main()
{
	gl_FragColor = vec4(vTexCoord, 1);
}