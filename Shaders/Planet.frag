#version 330 core

in vec3 vWorldPos;
in vec3 vNormal;
in vec3 vLocalNormal;
out vec4 FragColor;

uniform vec3 uAlbedo;
uniform vec3 uLightDir;
uniform vec3 uCameraPos;
uniform int uStyle;
uniform float uAlpha;
uniform sampler2D uAlbedoTexture;
uniform int uUseTexture;
uniform float uTextureLongitudeOffset;

float hash31(vec3 p)
{
    return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

float mottling(vec3 n)
{
    return sin(n.x * 9.1 + n.z * 4.2) * sin(n.y * 7.3 - n.x * 2.8)
         + 0.45 * sin(n.z * 11.0 + n.y * 5.1);
}

vec2 sphereUv(vec3 n)
{
    n = normalize(n);
    float longitude = atan(n.z, n.x) + uTextureLongitudeOffset;
    float latitude = asin(clamp(n.y, -1.0, 1.0));
    float u = longitude / (2.0 * 3.14159265) + 0.5;
    float v = 0.5 - latitude / 3.14159265;
    return vec2(u, v);
}

vec3 earthAlbedo(vec3 n)
{
    float land = sin(n.x * 3.8 + n.z * 1.6) * sin(n.y * 2.7 + n.x * 0.9);
    land += 0.35 * sin(n.z * 5.1 - n.y * 2.2);
    vec3 ocean = vec3(0.05, 0.14, 0.38);
    vec3 continent = vec3(0.18, 0.32, 0.14);
    vec3 ice = vec3(0.86, 0.90, 0.94);
    vec3 ground = mix(ocean, continent, smoothstep(0.12, 0.42, land));
    float polar = smoothstep(0.72, 0.92, abs(n.y));
    return mix(ground, ice, polar);
}

vec3 jupiterAlbedo(vec3 n)
{
    float lat = n.y;
    float bands = 0.5 + 0.5 * sin(lat * 16.0 + 0.4 * sin(lat * 7.0));
    vec3 cream = vec3(0.86, 0.74, 0.52);
    vec3 rust = vec3(0.62, 0.38, 0.20);
    vec3 brown = vec3(0.42, 0.24, 0.14);
    vec3 color = mix(cream, rust, bands);
    color = mix(color, brown, smoothstep(0.65, 1.0, abs(lat)));
    vec2 spot = vec2(n.x - 0.42, n.y + 0.22);
    float redSpot = exp(-dot(spot, spot) * 38.0);
    color = mix(color, vec3(0.72, 0.22, 0.12), redSpot * 0.85);
    return color;
}

// Tuned from Cassini natural-color Saturn (pale butter / soft ochre banding; no fictional map).
vec3 saturnAlbedo(vec3 n)
{
    float lat = n.y;
    float bands = 0.5 + 0.5 * sin(lat * 9.5 + 0.18 * sin(lat * 4.2));
    float fine = 0.5 + 0.5 * sin(lat * 28.0);
    vec3 pale = vec3(0.93, 0.86, 0.70);
    vec3 butter = vec3(0.86, 0.76, 0.55);
    vec3 ochre = vec3(0.74, 0.62, 0.42);
    vec3 color = mix(pale, butter, bands * 0.55);
    color = mix(color, ochre, fine * 0.12 * (1.0 - abs(lat)));
    color = mix(color, vec3(0.82, 0.74, 0.58), smoothstep(0.58, 0.96, abs(lat)) * 0.28);
    return color;
}

// Tuned from Voyager 2 true-color Uranus (PIA00032): nearly featureless pale cyan-green.
vec3 uranusAlbedo(vec3 n)
{
    float lat = n.y;
    float soft = 0.5 + 0.5 * sin(lat * 3.5);
    vec3 pale = vec3(0.66, 0.84, 0.86);
    vec3 mint = vec3(0.52, 0.76, 0.78);
    vec3 color = mix(pale, mint, abs(lat) * 0.18 + soft * 0.08);
    color = mix(color, vec3(0.72, 0.88, 0.88), (1.0 - smoothstep(0.05, 0.45, abs(lat))) * 0.12);
    return color;
}

// Tuned from Voyager 2 Neptune color: deep azure with restrained banding / storm.
vec3 neptuneAlbedo(vec3 n)
{
    float lat = n.y;
    float bands = 0.5 + 0.5 * sin(lat * 8.0 + 0.22 * sin(lat * 3.5));
    vec3 deep = vec3(0.05, 0.16, 0.52);
    vec3 rich = vec3(0.12, 0.34, 0.78);
    vec3 color = mix(deep, rich, bands * 0.40 + 0.28);
    vec2 storm = vec2(n.x + 0.30, n.y - 0.16);
    float dark = exp(-dot(storm, storm) * 36.0);
    color = mix(color, vec3(0.03, 0.07, 0.24), dark * 0.55);
    return color;
}

vec3 plutoAlbedo(vec3 n)
{
    float patch = mottling(n);
    vec3 tan = vec3(0.62, 0.52, 0.42);
    vec3 grey = vec3(0.48, 0.47, 0.46);
    vec3 heart = vec3(0.78, 0.62, 0.55);
    vec3 color = mix(tan, grey, smoothstep(-0.35, 0.45, patch));
    vec2 tombaugh = vec2(n.x - 0.22, n.z - 0.15);
    float cap = exp(-dot(tombaugh, tombaugh) * 14.0) * (0.55 + 0.45 * n.y);
    color = mix(color, heart, clamp(cap, 0.0, 1.0) * 0.65);
    color = mix(color, vec3(0.82, 0.84, 0.86), smoothstep(0.82, 0.96, abs(n.y)) * 0.4);
    return color;
}

vec3 marsAlbedo(vec3 n)
{
    float land = sin(n.x * 4.2 + n.z * 1.4) * sin(n.z * 3.1 - n.y * 2.0);
    land += 0.4 * sin(n.y * 5.0 + n.x * 2.2);
    vec3 rust = vec3(0.66, 0.30, 0.14);
    vec3 dark = vec3(0.34, 0.16, 0.10);
    vec3 ice = vec3(0.84, 0.86, 0.88);
    vec3 color = mix(rust, dark, smoothstep(-0.15, 0.48, land));
    color = mix(color, ice, smoothstep(0.78, 0.94, abs(n.y)) * 0.9);
    return color;
}

vec3 rockyAlbedo(vec3 n, vec3 base)
{
    float speckle = mottling(n);
    float crater = hash31(floor(n * 18.0 + 4.0));
    vec3 color = base * (0.78 + 0.28 * speckle);
    color *= mix(1.0, 0.72, smoothstep(0.62, 0.92, crater));
    return color;
}

void main()
{
    vec3 nWorld = normalize(vNormal);
    vec3 nLocal = normalize(vLocalNormal);
    vec3 albedo = uAlbedo;

    if (uUseTexture != 0)
    {
        albedo = texture(uAlbedoTexture, sphereUv(nLocal)).rgb;
    }
    else if (uStyle == 1)
        albedo = earthAlbedo(nLocal);
    else if (uStyle == 2)
        albedo = jupiterAlbedo(nLocal);
    else if (uStyle == 3)
        albedo = saturnAlbedo(nLocal);
    else if (uStyle == 4)
        albedo = uranusAlbedo(nLocal);
    else if (uStyle == 5)
        albedo = neptuneAlbedo(nLocal);
    else if (uStyle == 6)
        albedo = plutoAlbedo(nLocal);
    else if (uStyle == 7)
        albedo = marsAlbedo(nLocal);
    else
        albedo = rockyAlbedo(nLocal, uAlbedo);

    vec3 light = normalize(uLightDir);
    vec3 view = normalize(uCameraPos - vWorldPos);
    float ndl = max(dot(nWorld, light), 0.0);
    float wrap = ndl * 0.78 + 0.22;
    vec3 color = albedo * wrap;

    float rim = pow(1.0 - max(dot(nWorld, view), 0.0), 3.2);
    if (uStyle == 1)
        color += vec3(0.25, 0.45, 0.85) * rim * 0.55;
    else if (uStyle == 4)
        color += vec3(0.45, 0.75, 0.80) * rim * 0.18;
    else if (uStyle == 5)
        color += vec3(0.20, 0.40, 0.90) * rim * 0.28;
    else
        color += albedo * rim * 0.12;

    FragColor = vec4(color, uAlpha);
}
