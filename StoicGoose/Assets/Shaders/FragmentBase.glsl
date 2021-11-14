#version 460

in vec2 texCoord;

out vec4 fragColor;

uniform sampler2D textureSamplers[8];

uniform vec4 outputViewport;
uniform vec4 inputViewport;
