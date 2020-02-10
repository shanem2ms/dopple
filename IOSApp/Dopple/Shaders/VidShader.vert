#version 100 

uniform mat4 uMVP;
attribute vec3 aPosition;
attribute vec3 aTexCoord;
varying vec3 vTexCoord;

void main() {
	gl_Position = uMVP * vec4(aPosition, 1.0);
	vTexCoord = aTexCoord;
}