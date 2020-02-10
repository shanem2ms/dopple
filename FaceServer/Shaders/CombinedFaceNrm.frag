#version 150 compatibility
uniform float depthImageBlend;
uniform vec3 meshColor;
in vec3 vNormal;
in vec3 vTexCoord;
void main()
{
	vec3 lightColor = (vNormal.xyz + vec3(1.0, 1.0, 1.0)) * 0.5;
	gl_FragColor = vec4(lightColor, 1);
}