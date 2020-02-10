#version 150 compatibility
uniform float depthImageBlend;
uniform vec3 meshColor;
in vec3 vNormal;
in vec3 vTexCoord;
void main()
{
	float val = (dot(vec3(1, 0, 0), vNormal.xyz) + 1);
	vec3 lightColor = meshColor;                
	gl_FragColor = vec4(vTexCoord, 1);
}