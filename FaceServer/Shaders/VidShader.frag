#version 150 compatibility
in vec3 vTexCoord;
uniform vec2 depthScale;
uniform vec2 depthOffset;
uniform sampler2D ySampler;
uniform sampler2D uvSampler;
uniform highp sampler2D depthSampler;
             
uniform float imageDepthMix = 0;
uniform vec2 depthVals;              
uniform bool hasDepth;
void main()
{
	vec3 yuv;
	vec2 imgTexCoord = vTexCoord.xy;
	imgTexCoord.y = 1 - imgTexCoord.y;
	yuv.x = texture2D(ySampler, imgTexCoord).r;
	yuv.yz = texture2D(uvSampler, imgTexCoord).rg - vec2(0.5, 0.5);
	vec3 rgb = mat3(1,1,1,  
			0, -0.18732, 1.8556,  
			1.57481, -0.46813, 0) * yuv;
	if (hasDepth)
	{
		vec2 depthTexCoord = vTexCoord.xy;
		float accd = 0;
		float invMin = 1.0 / depthVals.x;
		float invMax = 1.0 / depthVals.y;
		depthTexCoord.y = 1 - depthTexCoord.y;
		float d = texture2D(depthSampler, depthTexCoord).r;
		float dd = 0;
		if (d > 0)
		{
			float invd = 1.0 / d;
			dd = (invd - invMax) / (invMin - invMax);
			dd *= 6;
		}
		vec3 drgb = vec3(clamp(dd, 0.0, 1.0), clamp(dd - 1.0, 0.0, 1.0), clamp(dd - 2.0, 0.0, 1.0));
		vec3 imgdepth = mix(rgb, drgb, imageDepthMix);
		gl_FragColor = vec4(imgdepth, 1);
	}
	else
	{
		gl_FragColor = vec4(rgb,1);
	}
}