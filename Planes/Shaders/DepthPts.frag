#version 150 compatibility
uniform vec3 meshColor;
uniform float ambient;
uniform vec3 lightPos;
uniform float opacity;
uniform sampler2D ySampler;
uniform sampler2D uvSampler;

in vec3 vNormal;
in vec3 vWsPos;
in vec3 vTexCoord;
in vec4 vCamPos;
void main()
{
	vec3 lightVec = normalize(vWsPos - lightPos);
	float lit = abs(dot(lightVec, vNormal));
	vec2 vidtex = vCamPos.xy / vCamPos.w;
	//vidtex = vec2(0.5 - vidtex.x / 640, 0.5 - vidtex.y / 480);
	vec3 yuv;
	yuv.x = texture2D(ySampler, vidtex).r;
	yuv.yz = texture2D(uvSampler, vidtex).rg - vec2(0.5, 0.5);
	vec3 rgb = mat3(1,1,1,  
			0, -0.18732, 1.8556,  
			1.57481, -0.46813, 0) * yuv;
	rgb = vTexCoord;
	gl_FragColor = vec4(rgb * (lit * (1 - ambient) + ambient), 1) * opacity;
}