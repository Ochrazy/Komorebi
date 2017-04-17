#version 330 core
 
in vec3 i_position; 
in vec3 i_normal;
in vec2 i_texcoord;

uniform mat4 m_viewModel; // Modell View Matrix
uniform mat4 m_normal; // normal Matrix

uniform mat4 MVP;
uniform mat4 depthBiasMVP;

// the data to be sent to the fragment shader
out vec3 normal;
out vec3 position;
out vec2 o_texcoord;
out vec4 shadowCoord;
 
void main() 
{
    normal = normalize(mat3(m_normal) * i_normal);
    position = vec3(m_viewModel * vec4(i_position, 1));

	// Normal Offset Shadows (Normal Bias)
	vec3 shadowPosition = i_position + (i_normal * 0.06); 
    shadowCoord = depthBiasMVP * vec4(shadowPosition, 1);

    gl_Position =  MVP * vec4(i_position, 1);

	o_texcoord = i_texcoord.xy;
}
