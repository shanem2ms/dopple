#version 100

varying highp vec3 vTexCoord;
void main()
{
	gl_FragColor = vec4(vTexCoord.xy,1,1);
}