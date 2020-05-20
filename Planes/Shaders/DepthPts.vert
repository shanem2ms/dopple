#version 150 compatibility
uniform mat4 uMVP;
uniform mat4 uWorldInvTranspose;
uniform mat4 uWorld;
uniform mat4 uCamMat;
in vec3 aPosition;
in vec3 aTexCoord0;
in vec3 aNormal;
out vec3 vTexCoord;
out vec3 vWsPos;
out vec3 vNormal;
out vec4 vCamPos;
void main() {
    gl_Position = uMVP * vec4(aPosition, 1.0);
    vCamPos = uCamMat * vec4(aPosition, 1.0);
    vTexCoord = aTexCoord0;
    vec4 norm = uWorldInvTranspose * vec4(aNormal, 0);
    vWsPos = (uWorld * vec4(aPosition, 1.0)).xyz;
    vNormal = normalize(norm.xyz);
}
