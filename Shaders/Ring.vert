#version 330 core

layout (location = 0) in vec3 aPosition;

uniform mat4 uViewProjection;
uniform vec3 uCenter;
uniform vec3 uPole;
uniform float uPlanetRadius;

out vec3 vWorldPos;
out vec3 vLocal;
out vec3 vNormal;

void main()
{
    vec3 pole = normalize(uPole);
    vec3 along = normalize(cross(pole, abs(pole.y) > 0.9 ? vec3(1.0, 0.0, 0.0) : vec3(0.0, 1.0, 0.0)));
    vec3 across = cross(pole, along);
    vec3 local = along * (aPosition.x * uPlanetRadius)
               + pole * (aPosition.y * uPlanetRadius)
               + across * (aPosition.z * uPlanetRadius);
    vLocal = vec3(aPosition.x * uPlanetRadius, aPosition.y * uPlanetRadius, aPosition.z * uPlanetRadius);
    vec3 world = uCenter + local;
    vWorldPos = world;
    vNormal = pole;
    gl_Position = vec4(world, 1.0) * uViewProjection;
}
