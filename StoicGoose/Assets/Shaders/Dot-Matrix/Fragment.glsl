void main()
{
    vec4[numSamplers] outputColors;
    vec4 outputColor = vec4(0);
    vec2 gridStep = vec2(floor(outputViewport.z / inputViewport.z), floor(outputViewport.w / inputViewport.w));

    for (int i = 0; i < numSamplers; i++) outputColors[i] = texture(textureSamplers[i], texCoord);
    for (int i = 0; i < numSamplers; i++) outputColor += outputColors[i] * (1.0 / float(numSamplers));

    // Basic grid (horizontal, then vertical)
    if (gridStep.x > 1.0 && gridStep.y > 1.0)
    {
        outputColor = vec4(clamp(mod(gl_FragCoord.x - outputViewport.x, gridStep.x), 0.8, 1.0) * outputColor.rgb, 1.0);
        outputColor = vec4(clamp(mod(gl_FragCoord.y - outputViewport.y, gridStep.y), 0.8, 1.0) * outputColor.rgb, 1.0);
    }

    // Gamma correction
    float gamma = 1.2;
    outputColor.rgb = pow(outputColor.rgb, vec3(1.0 / gamma));

    // Reduce to grayscale & tint
    outputColor.rgb = vec3(dot(outputColor.rgb, vec3(0.299, 0.587, 0.114))) * vec3(0.878, 0.973, 0.816);

    fragColor = outputColor;
}
