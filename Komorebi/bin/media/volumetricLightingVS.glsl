#version 330 core
 
in vec3 i_position; 
in vec2 i_texcoord;

out vec2 o_texcoord;
out vec3 viewRay;

uniform mat4 invVP;
 
void main() 
{  
	o_texcoord = i_texcoord.xy;
	gl_Position = vec4(i_position, 1); 

	viewRay = (invVP * vec4(i_texcoord.st * 2.0 - 1.0, 1.0, 0.0)).xyz;
}
