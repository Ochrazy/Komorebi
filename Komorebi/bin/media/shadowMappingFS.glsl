#version 330 core

in vec3 normal;
in vec3 position;
in vec2 o_texcoord;
in vec4 shadowCoord;

out vec4 resultColor;

uniform sampler2DShadow shadowMap;
uniform sampler2D modelTexture;

uniform vec3 diffuse;
uniform vec3 ambient;
uniform vec3 specular;
uniform float shininess;

uniform vec3 l_dir;    // camera space

uniform float useStratifiedPoissonSampling;
uniform float useModelTexture;

// Poisson Disc
vec2 poissonDisk[16] = vec2[]( 
   vec2( -0.94201624, -0.39906216 ), 
   vec2( 0.94558609, -0.76890725 ), 
   vec2( -0.094184101, -0.92938870 ), 
   vec2( 0.34495938, 0.29387760 ), 
   vec2( -0.91588581, 0.45771432 ), 
   vec2( -0.81544232, -0.87912464 ), 
   vec2( -0.38277543, 0.27676845 ), 
   vec2( 0.97484398, 0.75648379 ), 
   vec2( 0.44323325, -0.97511554 ), 
   vec2( 0.53742981, -0.47373420 ), 
   vec2( -0.26496911, -0.41893023 ), 
   vec2( 0.79197514, 0.19090188 ), 
   vec2( -0.24188840, 0.99706507 ), 
   vec2( -0.81409955, 0.91437590 ), 
   vec2( 0.19984126, 0.78641367 ), 
   vec2( 0.14383161, -0.14100790 ) 
);

// Returns a random number based on a vec3 and an int.
float random(vec3 seed, int i)
{
    vec4 seed4 = vec4(seed,i);
    float dot_product = dot(seed4, vec4(12.9898,78.233,45.164,94.673));
    return fract(sin(dot_product) * 43758.5453);
}

void main()
{
    vec3 L = normalize(l_dir);// - position);
    vec3 E = normalize(-position); // we are in Camera Coordinates, so EyePos is (0,0,0)
    vec3 R = normalize(-reflect(L, normal));

	// Calculate Bias
    //float bias = 0.050;
    //float cosTheta = dot(normalize(normal), normalize(L));
    //cosTheta = clamp(cosTheta, 0,1);
    //float bias = 0.005*tan(acos(cosTheta));
    //bias = clamp(bias, 0.0, 0.0);//0.005); // we use normal offsets
	float bias = 0.0; // we use normal offsets

    // Sample the shadow map 4 times
	float visibility = 1.0;
	int index = 0;
	for (int i=0;i<4;i++)
	{
		if(useStratifiedPoissonSampling < 0.5)
			index = i;
		else index = int(16.0*random(floor(position.xyz*1000.0), i))%16;

		// being fully in the shadow will eat up 4*0.2 = 0.8. 0.2 potentially remain.
		float numberOfTestsSucceeded = texture( shadowMap, vec3(shadowCoord.xy + (poissonDisk[index]/1600.0),  (shadowCoord.z/shadowCoord.w) - bias));
		visibility -= 0.2 * (1.0 - numberOfTestsSucceeded);
	}

    //calculate Ambient Term:
    vec3 Iamb = ambient;
    
    //calculate Diffuse Term:
	vec3 Idiff = visibility * diffuse * clamp(dot(normal,L), 0.0, 1.0);
    
    // calculate Specular Term:
	vec3 Ispec = visibility * specular * pow(clamp(dot(R,E),0.0, 1.0), shininess);

	if(useModelTexture > 0.5)
		resultColor = vec4(vec3((Iamb + Idiff) * texture(modelTexture, o_texcoord).xyz) + Ispec, 1.0);
	else resultColor = vec4(Iamb + Idiff + Ispec, 1.0);
}
