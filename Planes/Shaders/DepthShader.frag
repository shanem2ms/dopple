#version 150 compatibility
in vec3 vTexCoord;
uniform vec2 depthScale;
uniform vec2 depthOffset;
uniform highp sampler2D depthSampler;
uniform highp sampler2D markerTex;
             
uniform float imageDepthMix = 0;
uniform vec2 depthVals;              
uniform bool hasDepth;
void main()
{
	vec3 yuv;
	vec2 imgTexCoord = vTexCoord.xy;
	float dv = texture2D(depthSampler, imgTexCoord).r;
	if (isnan(dv) || dv == 0 || isinf(dv)) 
		dv = 0;
	else
		dv = (dv - depthVals.x) / (depthVals.y - depthVals.x);

	vec3 rgbdepth = vec3(dv, dv, dv);
	gl_FragColor = vec4(rgbdepth, 1);
}