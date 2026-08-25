#version 330 core

uniform float uAlpha;
out vec4 FragColor;

void main()
{
    FragColor = vec4(0.0, 0.0, 0.0, uAlpha);
}
