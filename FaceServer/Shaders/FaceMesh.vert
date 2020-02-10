#version 150 compatibility
uniform mat4 uMVP;
uniform vec2 faceOffset;
uniform vec2 faceScale;
in vec3 aPosition;
in vec3 aTexCoord;
out vec3 vTexCoord;
void main() {
	vec4 projPos = uMVP * vec4(aPosition.xyz, 1.0); 
    vTexCoord = aTexCoord;
    gl_Position = projPos;
}