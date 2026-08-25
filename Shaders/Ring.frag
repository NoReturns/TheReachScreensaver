#version 330 core

in vec3 vWorldPos;
in vec3 vLocal;
in vec3 vNormal;
out vec4 FragColor;

uniform vec3 uLightDir;
uniform vec3 uCameraPos;
uniform vec3 uRingColor;
uniform float uInner;
uniform float uOuter;
uniform float uOpacity;

void main()
{
    float r = length(vLocal.xz);
    float t = (r - uInner) / max(uOuter - uInner, 0.0001);
    if (t < 0.0 || t > 1.0)
        discard;

    float edge = smoothstep(0.0, 0.02, t) * smoothstep(0.0, 0.03, 1.0 - t);
    float cassini = 1.0 - 0.85 * smoothstep(0.57, 0.61, t) * (1.0 - smoothstep(0.67, 0.71, t));
    float bands = 0.62 + 0.38 * sin(t * 38.0) * sin(t * 9.5 + 0.35);
    bands *= cassini;
    float alpha = edge * mix(0.55, 1.0, bands) * uOpacity;
    if (alpha < 0.02)
        discard;

    vec3 n = normalize(vNormal);
    vec3 light = normalize(uLightDir);
    vec3 view = normalize(uCameraPos - vWorldPos);
    if (dot(n, view) < 0.0)
        n = -n;
    float ndl = abs(dot(n, light));
    float wrap = ndl * 0.40 + 0.60;
    vec3 color = uRingColor * wrap;
    FragColor = vec4(color, alpha);
}
