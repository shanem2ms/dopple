#version 100

precision highp float;
bool isnan(float val)
{
    return (val <= 0.0 || 0.0 <= val) ? false : true;
}

varying highp vec3 vTexCoord;
uniform highp vec2 imgScale;
uniform highp vec2 imgOffset;
uniform sampler2D ySampler;
uniform sampler2D uvSampler;
uniform highp sampler2D depthSampler;
             
uniform highp float imageDepthMix;
uniform highp vec2 depthVals;              
uniform bool hasDepth;
void main()
{
	highp vec3 yuv;
	highp vec2 imgTexCoord = (vTexCoord.xy - vec2(0.5, 0.5)) * imgScale + vec2(0.5, 0.5) - imgOffset;
	yuv.x = texture2D(ySampler, imgTexCoord).r;
	yuv.yz = texture2D(uvSampler, imgTexCoord).rg - vec2(0.5, 0.5);
	highp vec3 rgb = mat3(1.0, 1.0, 1.0,  
			0.0, -0.18732, 1.8556,  
			1.57481, -0.46813, 0.0) * yuv;

	if (hasDepth)
	{
		vec2 depthTexCoord = vTexCoord.xy;
		float accd = 0.0;
		float invMin = 1.0 / depthVals.x;
		float invMax = 1.0 / depthVals.y;
		float d = texture2D(depthSampler, depthTexCoord).r;
		float maskVal = (d < 0.75 && d > 0.01) ? 1.0 : 0.0;
		gl_FragColor = vec4(d, d, d, 1);
	}
	else
	{
		gl_FragColor = vec4(rgb,1.0);
	}
}