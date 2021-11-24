#version 460

const int renderModeDisplay = 0;
const int renderModeIcons = 1;

in vec2 texCoord;

out vec4 fragColor;

uniform int renderMode = 0;

uniform sampler2D textureSamplers[8];

uniform float displayBrightness;
uniform float displayContrast;
uniform float displaySaturation;

uniform vec4 outputViewport;
uniform vec4 inputViewport;

vec4 renderDisplay();
vec4 renderIcons();

vec4 applyAdjustments(vec4 color);

void main()
{
    vec4 outputColor = vec4(0.0);

    switch (renderMode)
    {
        case renderModeDisplay:
            outputColor = renderDisplay();
            break;

        case renderModeIcons:
            outputColor = renderIcons();
            break;
    }

    fragColor = applyAdjustments(outputColor);
}

// https://www.shadertoy.com/view/XdcXzn
mat4 getBrightnessMatrix(float brightness)
{
    return mat4(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        brightness, brightness, brightness, 1);
}

mat4 getContrastMatrix(float contrast)
{
    float t = (1.0 - contrast) / 2.0;

    return mat4(
        contrast, 0, 0, 0,
        0, contrast, 0, 0,
        0, 0, contrast, 0,
        t, t, t, 1);
}

mat4 getSaturationMatrix(float saturation)
{
    vec3 luminance = vec3(0.3086, 0.6094, 0.0820);

    float oneMinusSat = 1.0 - saturation;

    vec3 red = vec3(luminance.r * oneMinusSat);
    red += vec3(saturation, 0, 0);

    vec3 green = vec3(luminance.g * oneMinusSat);
    green += vec3(0, saturation, 0);

    vec3 blue = vec3(luminance.b * oneMinusSat);
    blue += vec3(0, 0, saturation);

    return mat4(
        red, 0,
        green, 0,
        blue, 0,
        0, 0, 0, 1);
}

vec4 applyAdjustments(vec4 color)
{
    return
        getBrightnessMatrix(displayBrightness) *
        getContrastMatrix(displayContrast) * 
        getSaturationMatrix(displaySaturation) *
        color;
}
