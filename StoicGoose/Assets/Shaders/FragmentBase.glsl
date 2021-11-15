#version 460

in vec2 texCoord;

out vec4 fragColor;

uniform sampler2D textureSamplers[8];

uniform vec4 outputViewport;
uniform vec4 inputViewport;

uniform int renderMode = 0;

vec4 display();
vec4 icons();

void main()
{
	if (renderMode == 1)
		fragColor = icons();
	else
		fragColor = display();
}
