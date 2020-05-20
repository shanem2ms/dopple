#version 150 compatibility
uniform vec4 pickColor;
in vec3 vWsPos;
void main()
{
	gl_FragColor = vec4(gl_FragCoord.z);
}