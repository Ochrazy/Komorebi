#version 330

uniform vec4 diffuse = vec4(0.6, 0.3, 0, 1);
uniform vec4 ambient = vec4(0.1, 0.1, 0.1, 1);
uniform vec4 specular = vec4(1, 1, 1, 1);
uniform float shininess = 64;

uniform vec3 l_dir;    // camera space
 
in vec3 normal;
in vec3 position;
 
out vec4 colorOut;
 
void main() {
   vec3 L = normalize(l_dir);// - position);   
   vec3 E = normalize(-position); // we are in Camera Coordinates, so EyePos is (0,0,0)  
   vec3 R = normalize(-reflect(L, normal));  
 
   //calculate Ambient Term:  
   vec4 Iamb = ambient;    

   //calculate Diffuse Term:  
   vec4 Idiff = diffuse * max(dot(normal,L), 0.0);
   Idiff = clamp(Idiff, 0.0, 1.0);     
   
   // calculate Specular Term:
   vec4 Ispec = specular * pow(max(dot(R,E),0.0), shininess);
   Ispec = clamp(Ispec, 0.0, 1.0); 
   // write Total Color:  
   colorOut = vec4(0, 0, 0, 1) + Iamb + Idiff + Ispec; 
}