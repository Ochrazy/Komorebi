#version 330 core

in vec2 o_texcoord;
 
uniform sampler2D fboTexture;
uniform sampler2D depthTexture;
uniform vec2 uShift;
 
const int gaussRadiusHalf = 7;
const float gaussFilter[8] = float[8](
	0.14446445, 0.13543542, 0.11153505, 0.08055309, 0.05087564, 0.02798160, 0.01332457, 0.00545096
);

out vec4 resultColor;



const float n = 0.1;                               // the near plane
const float f = 100.0;                              // the far plane

float getLinearDepth(float depth)
{
	return depth;  // convert to linear values (0..1)
}
 
void main() 
{
	vec2 texCoord = o_texcoord.xy - float(gaussRadiusHalf) * uShift;
	vec3 accumResult = vec3(0.0, 0.0, 0.0); 
	float accumWeight = 0.0; 
	float weight = 0.0; 
	vec3 kernelSample = vec3(0.0, 0.0, 0.0); 

	float depth = 0.0;
	float depthDiff = 0.0;

	float centerDepth = getLinearDepth(texture(depthTexture, o_texcoord.xy).r); 

	for (int i = -gaussRadiusHalf; i <= gaussRadiusHalf; i++) 
	{ 
		kernelSample = texture(fboTexture, texCoord).xyz;
		
		// Get Depth
		float kernelDepth = getLinearDepth(texture(depthTexture, texCoord).r); // fetch the z-value from our depth texture

		// Depth-aware filtering
		depthDiff = kernelDepth - centerDepth;
		float r2 = depthDiff * sign(depthDiff);
		r2 = r2 * 1000.0; // Blur Depth Falloff
		float g = exp (-r2 * r2);
		
		int j = i * sign(i);
		weight = g * gaussFilter[j];

		accumResult += weight * kernelSample;
		accumWeight += weight;

		texCoord += uShift;
	}

	resultColor = vec4(accumResult/accumWeight, 1.0);
}