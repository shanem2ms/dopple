#version 150 compatibility
in vec3 vTexCoord;
uniform vec3 meshColor;
void main()
{
gl_FragColor = vec4(meshColor,1);
}