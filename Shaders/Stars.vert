#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColorSize;

uniform mat4 uViewProjection;
uniform vec3 uCameraPosition;
uniform vec3 uWrapSize;
uniform float uPixelScale;

out vec3 vColor;
out float vSparkle;

vec3 wrapToCamera(vec3 world)
{
    vec3 rel = world - uCameraPosition;
    rel = rel - uWrapSize * floor(rel / uWrapSize + 0.5);
    return uCameraPosition + rel;
}

void main()
{
    vec3 world = wrapToCamera(aPosition);
    vec4 clip = vec4(world, 1.0) * uViewProjection;
    gl_Position = clip;

    float dist = max(length(world - uCameraPosition), 0.35);
    float size = (aColorSize.a * uPixelScale) / (dist * 0.55 + 8.0);
    gl_PointSize = clamp(size, 1.0, 14.0);

    float distanceFade = mix(1.0, 0.35, smoothstep(30.0, uWrapSize.z * 0.55, dist));
    vColor = aColorSize.rgb * distanceFade;
    vSparkle = aColorSize.a * distanceFade;
}
