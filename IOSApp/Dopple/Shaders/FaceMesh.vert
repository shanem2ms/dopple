#version 100

uniform mat4 uMVP;
attribute vec3 aPosition;
attribute vec3 aTexCoord;
varying vec3 vTexCoord;
void main() {
	vec4 projPos = uMVP * vec4(aPosition.xyz, 1.0); 
    vTexCoord = aTexCoord;
    gl_Position = projPos;
}