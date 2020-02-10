#version 100

precision highp float;

varying highp vec3 vTexCoord;
uniform sampler2D ySampler;
uniform sampler2D uvSampler;
uniform highp sampler2D depthSampler;
             
uniform highp float imageDepthMix;
void main()
{
	highp vec3 yuv;
	highp vec2 imgTexCoord = vTexCoord.xy;
	yuv.x = texture2D(ySampler, imgTexCoord).r;
	yuv.yz = texture2D(uvSampler, imgTexCoord).rg - vec2(0.5, 0.5);
	highp vec3 rgb = mat3(1.0, 1.0, 1.0,  
			0.0, -0.18732, 1.8556,  
			1.57481, -0.46813, 0.0) * yuv;

	gl_FragColor = vec4(rgb,1.0);
}