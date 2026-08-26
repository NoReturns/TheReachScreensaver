#version 330 core

uniform float uIntensity;
uniform vec2 uAnchorVirtual;
uniform vec4 uPaneVirtual; // left, top, width, height (Windows top-left virtual desktop)
uniform vec4 uPaneFramebuffer; // x, y, width, height in framebuffer (GL bottom-left origin)
uniform float uTime;

out vec4 FragColor;

float hash21(vec2 p)
{
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void main()
{
    if (uIntensity <= 0.001)
        discard;

    // Map this fragment into the shared virtual-desktop coordinate system.
    float localX = (gl_FragCoord.x - uPaneFramebuffer.x) / max(uPaneFramebuffer.z, 1.0);
    float localY = (gl_FragCoord.y - uPaneFramebuffer.y) / max(uPaneFramebuffer.w, 1.0);
    float virtX = uPaneVirtual.x + localX * uPaneVirtual.z;
    // Virtual Y is top-down; GL framebuffer Y within the pane is bottom-up.
    float virtY = (uPaneVirtual.y + uPaneVirtual.w) - localY * uPaneVirtual.w;

    vec2 delta = vec2(virtX - uAnchorVirtual.x, virtY - uAnchorVirtual.y);
    float refSpan = max(max(uPaneVirtual.z, uPaneVirtual.w), 1.0);
    vec2 uv = delta / refSpan;
    float r = length(uv);
    float angle = atan(uv.y, uv.x);

    // Radial streak filaments (shared center across all monitors).
    float spokes = abs(sin(angle * 48.0 + uTime * 9.0));
    float filament = pow(spokes, 10.0);
    float streak = filament * smoothstep(0.02, 0.55, r) * (1.0 - smoothstep(0.75, 1.35, r));

    // Subtle noise sparkle along rays.
    float noise = hash21(floor(uv * 90.0 + vec2(uTime * 18.0, -uTime * 11.0)));
    float spark = step(0.82, noise) * filament * 0.55;

    // Center brightening + brief tunnel compression feel.
    float core = exp(-r * r * 18.0);
    float tunnel = exp(-pow(max(r - 0.08, 0.0) * 3.2, 2.0)) * (0.35 + 0.65 * filament);

    vec3 cool = vec3(0.55, 0.72, 1.0);
    vec3 hot = vec3(0.92, 0.96, 1.0);
    vec3 color = mix(cool, hot, core * 0.85);
    color *= (streak * 0.95 + spark * 0.55 + core * 0.55 + tunnel * 0.35);

    float alpha = clamp(uIntensity, 0.0, 1.0) * (0.22 + 0.78 * (streak + core * 0.65));
    FragColor = vec4(color * alpha, alpha);
}
