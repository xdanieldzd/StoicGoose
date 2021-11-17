vec3 accumulateTextures();
vec3 grid(vec3 color);
vec3 gamma(vec3 color);
vec3 reduceAndTint(vec3 color);

// Main function for display
vec4 display()
{
    vec4 outputColor = vec4(accumulateTextures(), 1.0);

    outputColor.rgb = grid(outputColor.rgb);
    outputColor.rgb = gamma(outputColor.rgb);
    outputColor.rgb = reduceAndTint(outputColor.rgb);

    return outputColor;
}

// Main function for system icons
vec4 icons()
{
    vec4 outputColor = texture(textureSamplers[0], texCoord);

    outputColor.rgb = gamma(outputColor.rgb);
    outputColor.rgb = reduceAndTint(outputColor.rgb);

    return outputColor;
}

// Accumulate pixels from all texture samplers
vec3 accumulateTextures()
{
    vec3[numSamplers] outputColors;
    vec3 outputColor = vec3(0);

    for (int i = 0; i < numSamplers; i++) outputColors[i] = texture(textureSamplers[i], texCoord).rgb;
    for (int i = 0; i < numSamplers; i++) outputColor += outputColors[i] * (1.0 / float(numSamplers));

    return outputColor;
}

// Apply basic grid pattern (horizontal, then vertical)
vec3 grid(vec3 color)
{
    vec3 outputColor = vec3(0);
    vec2 gridStep = vec2(floor(outputViewport.z / inputViewport.z), floor(outputViewport.w / inputViewport.w));

    if (gridStep.x > 1.0 && gridStep.y > 1.0)
    {
        outputColor = vec3(clamp(mod(gl_FragCoord.x - outputViewport.x, gridStep.x), 0.8, 1.0) * clamp(mod(gl_FragCoord.y - outputViewport.y, gridStep.y), 0.8, 1.0) * color);
    }

    return outputColor;
}

// Apply gamma correction
vec3 gamma(vec3 color)
{
    float gamma = 1.2;
    return pow(color, vec3(1.0 / gamma));
}

// Reduce to grayscale & tint
vec3 reduceAndTint(vec3 color)
{
    return vec3(dot(color, vec3(0.299, 0.587, 0.114))) * vec3(0.875, 0.921, 0.886);
}
