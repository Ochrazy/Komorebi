#version 330 core

in vec3 i_position; 
in vec2 i_texcoord;
out vec2 o_texcoord;

void main()
{
	o_texcoord = i_texcoord.xy;
	gl_Position = vec4(i_position, 1); 
}
