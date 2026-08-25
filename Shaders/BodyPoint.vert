#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColorSize;

uniform mat4 uViewProjection;
uniform float uPixelScale;
uniform float uGlobalAlpha;

out vec3 vColor;
out float vAlpha;

void main()
{
    vec4 clip = vec4(aPosition, 1.0) * uViewProjection;
    gl_Position = clip;

    float dist = max(length(aPosition), 1e-10);
    float size = (aColorSize.a * uPixelScale) / (dist * 1.8 + 0.002);
    gl_PointSize = clamp(size, 1.0, 8.0);

    vColor = aColorSize.rgb;
    vAlpha = aColorSize.a * uGlobalAlpha;
}
