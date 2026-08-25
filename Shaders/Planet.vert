#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;

uniform mat4 uViewProjection;
uniform vec3 uCenter;
uniform float uRadius;
uniform mat3 uRotation;

out vec3 vWorldPos;
out vec3 vNormal;
out vec3 vLocalNormal;

void main()
{
    vec3 local = aPosition;
    vec3 rotated = uRotation * local;
    vec3 world = rotated * uRadius + uCenter;
    vWorldPos = world;
    vLocalNormal = aNormal;
    vNormal = uRotation * aNormal;
    gl_Position = vec4(world, 1.0) * uViewProjection;
}
