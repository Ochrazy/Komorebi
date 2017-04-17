#version 330
 
uniform vec4 diffuse = vec4(0.3, 0.3, 1, 1);
uniform vec4 ambient = vec4(0.1, 0.1, 0.1, 1);
uniform vec4 specular = vec4(1, 1, 1, 1);
uniform float shininess = 64;

uniform vec3 l_dir;    // camera space
 
in vec3 normal;
in vec3 position;
 
out vec4 colorOut;
 
void main() {
   colorOut = vec4(0, 0, 0, 1);
}