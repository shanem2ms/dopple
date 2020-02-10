#version 150 compatibility
uniform float depthImageBlend;
uniform vec3 meshColor;
uniform int colorMode;
uniform float opacity;
in vec3 vNormal;
in vec3 vTexCoord;
void main()
{
	if (colorMode ==  0) // pick
	{
		gl_FragColor = vec4(vTexCoord, 1);
	}
	else if (colorMode == 1) // mesh color
	{
		float val = (dot(vec3(1, 0, 0), vNormal.xyz) + 1) * 0.5;
		vec3 lightColor = meshColor * val;
		gl_FragColor = vec4(lightColor, 1) * opacity;
	}
	else if (colorMode == 2) // nrm color
	{
		vec3 lightColor = (vNormal.xyz + vec3(1.0, 1.0, 1.0)) * 0.5;
		gl_FragColor = vec4(lightColor, 1) * opacity;
	}
	else // vtx
	{
		float val = (dot(vec3(1, 0, 0), vNormal.xyz) + 1) * 0.5;
		vec3 lightColor = vTexCoord;
		gl_FragColor = vec4(lightColor, 1) * opacity;
	}
}