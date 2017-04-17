#version 330 core

// Ouput data
layout(location = 0) out float fragmentdepth;

void main()
{
    // Red Texture -> Depth is stored in the Red Channel of the Texture
	fragmentdepth = gl_FragCoord.z;
}