#version 330
 
in vec2 o_texcoord;
out vec4 resultColor;

uniform float exposure;
uniform float decay;
uniform float density;
uniform float weight;
uniform vec3 lightPositionOnScreen;
uniform sampler2D fboTexture;
const int NUM_SAMPLES = 70;
uniform float noPostProcessing;

void main()
{	
	vec2 textCoo = o_texcoord.xy;
    vec2 deltaTextCoord = vec2( textCoo.xy - lightPositionOnScreen.xy );
	if(lightPositionOnScreen.z < 0.0) deltaTextCoord = -deltaTextCoord; // to handle light in the back
    deltaTextCoord *= 1.0 /  float(NUM_SAMPLES) * density;

    float illuminationDecay = 1.0;
	resultColor = texture(fboTexture, textCoo );
	
    for(int i=0; i < NUM_SAMPLES ; i++)
    {
                textCoo -= deltaTextCoord;
                vec4 currentSample = texture(fboTexture, textCoo );
			
                currentSample *= illuminationDecay * weight;

                resultColor += currentSample;

                illuminationDecay *= decay;
     }
     resultColor *= exposure; 

	 // Turn off the effect if the sun is in the back 
	 // and add some brightness to the scene so it is not THAT obvious
	 if((lightPositionOnScreen.z < 0.0))
	 {
		 vec4 sample = texture(fboTexture, o_texcoord) + vec4(0.1, 0.1, 0.15, 1);
		 resultColor = sample;
	 }
	
	if((noPostProcessing > 0.5))
	{
		// No Post-Processing (for testing etc.)
		vec4 sample = texture(fboTexture, o_texcoord);
		resultColor = sample;
	}
}