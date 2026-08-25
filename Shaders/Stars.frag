#version 330 core

in vec3 vColor;
in float vSparkle;
out vec4 FragColor;

void main()
{
    vec2 p = gl_PointCoord * 2.0 - 1.0;
    float r = length(p);
    if (r > 1.0)
        discard;

    float core = exp(-r * r * 5.4);
    float halo = exp(-r * r * 1.15) * 0.28;
    float alpha = (core + halo) * (0.45 + 0.55 * vSparkle);

    vec3 color = vColor * (0.62 + 0.38 * core);
    FragColor = vec4(color, alpha);
}
