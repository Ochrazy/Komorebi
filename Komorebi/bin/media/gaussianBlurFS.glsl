#version 330 core

in vec2 o_texcoord;
 
uniform sampler2D fboTexture;
uniform vec2 uShift;
 
const int gaussRadiusHalf = 7;
const float gaussFilter[8] = float[8](
	0.14446445, 0.13543542, 0.11153505, 0.08055309, 0.05087564, 0.02798160, 0.01332457, 0.00545096
);

out vec4 resultColor;
 
void main() 
{
	vec2 texCoord = o_texcoord.xy - float(gaussRadiusHalf) * uShift;
	vec3 color = vec3(0.0, 0.0, 0.0); 

	for (int i = -gaussRadiusHalf; i <= gaussRadiusHalf; i++) 
	{ 
		int j = i * sign(i);
		color += gaussFilter[j] * texture(fboTexture, texCoord).xyz;
		texCoord += uShift;
	}

	resultColor = vec4(color, 1.0);
}