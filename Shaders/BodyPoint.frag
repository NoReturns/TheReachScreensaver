#version 330 core

in vec3 vColor;
in float vAlpha;
out vec4 FragColor;

void main()
{
    vec2 uv = gl_PointCoord * 2.0 - 1.0;
    float r2 = dot(uv, uv);
    if (r2 > 1.0)
        discard;

    float core = exp(-r2 * 3.5);
    float glow = exp(-r2 * 1.2) * 0.35;
    float alpha = (core + glow) * vAlpha;
    if (alpha < 0.01)
        discard;

    FragColor = vec4(vColor * (core + glow * 0.5), alpha);
}
