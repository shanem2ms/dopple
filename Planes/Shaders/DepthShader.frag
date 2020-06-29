#version 150 compatibility
in vec3 vTexCoord;
uniform vec2 depthScale;
uniform vec2 depthOffset;
uniform highp sampler2D depthSampler;
uniform highp sampler2D markerTex;
             
uniform float imageDepthMix = 0;
uniform vec2 depthRange;              
uniform bool hasDepth;
void main()
{
	vec3 yuv;
	vec2 imgTexCoord = vTexCoord.xy;
	float dv = texture2D(depthSampler, imgTexCoord).r;
	vec3 rgb = vec3(0,0,0);
	float a = sign(dv);
	if (isnan(dv) || dv == 0 || isinf(dv)) 
	{
		rgb = vec3(0,0,0);
	}
	else
	{
		dv = (dv - depthRange.x) / (depthRange.y - depthRange.x);

		if (dv < 0.25)
			rgb = mix(vec3(0,0,0), vec3(0,1,0), dv / 0.25);
		else if (dv < 0.5)
			rgb = mix(vec3(0,1,0), vec3(1,0,0), (dv - 0.25) / 0.25);
		else if (dv < 0.75)
			rgb = mix(vec3(1,0,0), vec3(1,0,1), (dv - 0.5) / 0.25);
		else
			rgb = mix(vec3(1,0,1), vec3(1,1,0), (dv - 0.75) / 0.25);

		gl_FragColor = vec4(rgb, 1);
	}
}