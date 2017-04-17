#version 330
 
uniform mat4 m_pvm; // modelViewProjectionMatrix
uniform mat4 m_viewModel; // Modell View Matrix
uniform mat4 m_normal; // normal Matrix
 
in vec3 i_position;   // local space
in vec3 i_normal;     // local space
 
// the data to be sent to the fragment shader
out vec3 normal;
out vec3 position;
 
void main () {
 
    normal = normalize(mat3(m_normal) * i_normal);
    position = vec3(m_viewModel * vec4(i_position, 1));
 
    gl_Position = m_pvm * vec4(i_position, 1);
}