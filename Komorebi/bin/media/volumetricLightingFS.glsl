#version 330

in vec2 o_texcoord;
in vec3 viewRay;
out vec4 resultColor;

uniform sampler2D depthTexture;
uniform sampler2D shadowMapTexture;
uniform sampler2D noiseTexture;

uniform mat4 depthBiasMVP;

uniform vec3 viewPos;
uniform vec2 noiseOffset;

uniform float enumScatteringTechnique; // 0.0 = PointLight; 1.0 = StepLen; 2.0 = OnlyLightShaft; 3.0 = OnlyLightShaftReverseColor;

const float PI = 3.1415926535897931;

const float FLT_MAX = 3.402823466e+38f;

const vec3 ConeApex = vec3(20f, 30f, 25f);
const vec3 camToSunDirection = normalize(vec3(2,0.4,3.5));//normalize(ConeApex - viewPos);
const vec3 ConeAxis = normalize(vec3(0, -1, -0.5));
const float CosAngle = 0.8;

const float G_SCATTERING = 0.98;

// wavelength of used primaries, according to preetham
//const vec3 lambda = vec3(650E-9, 570E-9, 475E-9);
const vec3 lambda = vec3(680E-9, 550E-9, 450E-9);
const vec3 lambdaWhite = vec3(550E-9, 550E-9, 550E-9);

// mie stuff
// K coefficient for the primaries
const vec3 K = vec3(0.686, 0.678, 0.666);
const vec3 KWhite = vec3(0.678f, 0.678f, 0.678f);
const float v = 4.0f;
const float pi = 3.141592653589793238462643383279502884197169;

// Rayleigh
const float n = 1.0003; // refractive index of air
const float N = 2.545E25; // number of molecules per unit volume for air at
						// 288.15K and 1013mb (sea level -45 celsius)
const float pn = 0.035;	// depolatization factor for standard air

const float rayleighZenithLength = 8.4E3;
const float mieZenithLength = 1.25E3;

/**
 * Compute total rayleigh coefficient for a set of wavelengths (usually
 * the tree primaries)
 * @param lambda wavelength in m
 */
vec3 totalRayleigh(vec3 lambda)
{
	return (8 * pow(pi, 3) * pow(pow(n, 2) - 1, 2) * (6 + 3 * pn)) / (3 * N * pow(lambda, vec3(4)) * (6 - 7 * pn));
}

/** Reileight phase function as a function of cos(theta)
 */
float rayleighPhase(float cosTheta)
{
	/**
	 * NOTE: There are a few scale factors for the phase funtion
	 * (1) as given bei Preetham, normalized over the sphere with 4pi sr
	 * (2) normalized to integral = 1
	 * (3) nasa: integrates to 9pi / 4, looks best
	 */
	 
//	return (3.0f / (16.0f*pi)) * (1.0f + pow(cosTheta, 2));
//	return (1.0f / (3.0f*pi)) * (1.0f + pow(cosTheta, 2));
	return (3.0f / 4.0f) * (1.0f + pow(cosTheta, 2));
}

/**
 * total mie scattering coefficient
 * @param lambda set of wavelengths in m
 * @param K corresponding scattering param
 * @param T turbidity, somewhere in the range of 0 to 20
 */
vec3 totalMie(vec3 lambda, vec3 K, float T)
{
	// not the formula given py Preetham.
	float c = (0.2f * T ) * 10E-18;
	return 0.434 * c * pi * pow((2 * pi) / lambda, vec3(v - 2)) * K;
}

/**
 * Henyey-Greenstein approximation as a function of cos(theta)
 * @param cosTheta 
 * @param g goemetric constant that defines the shape of the ellipse.
 */
float hgPhase(float cosTheta, float g)
{
	return (1.0f / (4.0f*pi)) * ((1.0f - pow(g, 2)) / pow(1.0f - 2.0f*g*cosTheta + pow(g, 2), 1.5));
}

// earth shadow hack
const float cutoffAngle = pi/2.0f;
const float steepness = 0.5f;
const float E = 1.0f;

float sunIntensity(float zenithAngleCos)
{
	return E;
//	return E * max(0.0f, 1.0f - exp(-((cutoffAngle - acos(zenithAngleCos))/steepness)));
}

vec4 ToneMap(in vec4 f3Color)
{
    float fExposure = 1.0;//g_PPAttribs.m_fExposure;
    return 1.0 - exp(-fExposure * f3Color);
}

const float mainViewNearDepth = 0.1f;
const float mainViewFarDepth = 100.0f;
const float linearDepthProjectionA = mainViewFarDepth / (mainViewFarDepth - mainViewNearDepth);
const float linearDepthProjectionB = (-mainViewFarDepth * mainViewNearDepth) / (mainViewFarDepth - mainViewNearDepth);

float rayleighCoefficient = 1.0;
float mieCoefficient = 0.053;

void main()
{		
	// Get Noise
	vec2 uv2 = (o_texcoord * vec2(1280, 768) + noiseOffset) / 8;
    float pixelRayMarchNoise = texture(noiseTexture, uv2).r;

	resultColor.a = 1.0;

	float depth = texture(depthTexture, o_texcoord).r; // fetch the z-value from our depth texture

	// Reconstruct linear distance (0..1 mapping to 0..far)
    float linearDepth01 = linearDepthProjectionB / (depth - linearDepthProjectionA);

	// Reconstruct world space position
    vec3 worldPos = viewPos + viewRay * linearDepth01;

	// Parameters
    vec3 viewVec = worldPos.xyz-viewPos;
    float worldPosDist = length(viewVec);
    vec3 viewVecNorm = viewVec/worldPosDist;

	// Start and End Points of the ray
	float startDepth = 0.0;
	float endDepth = worldPosDist;

	 // Ray march
	vec3 curPos = viewPos + viewVecNorm * startDepth;
	vec3 rayEndPos = viewPos + viewVecNorm * endDepth;

    //const float stepLen = 0.005; // 200 steps
	float stepLen = 0.005; // 10 steps

	// If tracing distance is very short, we can fall into an inifinte loop due to
    // 0 length step and crash the driver. Return from function in this case
    if( worldPosDist < 0.0001)
    {
        stepLen = 1.0;
    }

	float stepLenWorld = stepLen * (endDepth-startDepth);
    vec3 scatteredLightAmount = vec3(0.0f);
	
	// D_DEPTH_RAY_DITHER
    curPos += stepLenWorld * viewVecNorm ;//* (2.0 * pixelRayMarchNoise - 1.0);

	float shadowOnLastStep = 0.0;

	vec3 currentLambda = lambda;
	if(endDepth > 99.0) 
	{
		currentLambda = lambda;
		rayleighCoefficient = 1.0;
		mieCoefficient = 0.0053;
	}
	else
	{
		rayleighCoefficient = 1.0;
		mieCoefficient = 0.0053;
	}

	// Calculate Phase functions
	// extinction (absorbtion + out scattering)
	// rayleigh coefficients
	vec3 betaR = totalRayleigh(currentLambda) * rayleighCoefficient;

	// mie coefficients            turbidity
	vec3 betaM = totalMie(currentLambda, K, 1.0) * mieCoefficient;

	// in scattering
	float cosTheta = dot(viewVecNorm, camToSunDirection);
		
	float rPhase = rayleighPhase(cosTheta);
	vec3 betaRTheta = betaR * rPhase;
			
	float mPhase = hgPhase(cosTheta, G_SCATTERING);//mieDirectionalG);
	vec3 betaMTheta = betaM * mPhase;

	vec3 phaseFinal = ((betaRTheta + betaMTheta) / (betaR + betaM));

	//							  sunDirection
	float sunE = sunIntensity(dot(camToSunDirection, vec3(0.0f, 1.0f, 0.0f)));	

	// Mac does not like float in for loops!
    for(int l = 1; l < int(1.0/stepLen) - 1; l++) // Do not do the first and last steps
    {
        curPos += stepLenWorld * viewVecNorm;
        
		// Sample Shadow Map
		vec4 shadowCoord = depthBiasMVP * vec4(curPos, 1);
		shadowCoord /= shadowCoord.w;
		float lightPointDepth = texture(shadowMapTexture, shadowCoord.xy).r;
       
	    // To Shadow or not to shadow 
        float shadow = (lightPointDepth > (shadowCoord.z)) ? 1.0 : 0.0;			

		// optical length
		// cutoff angle at 90 to avoid singularity in next formula. // curPos
		float zenithAngle = acos(max(0, dot(vec3(0, 1, 0), normalize(curPos - vec3(0, 0, 0)))));
		float sR = rayleighZenithLength / (cos(zenithAngle) + 0.15 * pow(93.885 - ((zenithAngle * 180.0f) / pi), -1.253));
		float sM = mieZenithLength / (cos(zenithAngle) + 0.15 * pow(93.885 - ((zenithAngle * 180.0f) / pi), -1.253));

		// combined extinction factor	
		vec3 Fex = exp(-(betaR * sR + betaM * sM));
		Fex = mix(Fex, 1.0 - Fex, abs((camToSunDirection).y));

		vec3 Lin = sunE * phaseFinal * (Fex);

		scatteredLightAmount += shadow * Lin;

		shadowOnLastStep = shadow;
    }

	// Calculate final ScatteringAmount
	resultColor = vec4(scatteredLightAmount * stepLen * vec3(1.0) , 1.0);

	//resultColor = ToneMap(resultColor);
}