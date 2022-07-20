layout (location = 0) in vec2 inPosition;
layout (location = 1) in vec2 inTexCoord;

out vec2 texCoord;

uniform mat4 projectionMatrix;
uniform mat4 modelviewMatrix;
uniform mat4 textureMatrix;

void main()
{
	texCoord = (textureMatrix * vec4(inTexCoord, 0.0, 1.0)).xy;
	gl_Position = projectionMatrix * modelviewMatrix * vec4(inPosition, 0.0, 1.0);
}
