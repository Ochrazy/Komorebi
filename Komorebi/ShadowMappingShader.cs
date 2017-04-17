using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Linq;

namespace Komorebi
{
    class ShadowMappingShader : ShaderProgram
    {
        // Locations of the Attributes and Uniforms in the Shader
        int positionLocation = -1;
        int normalLocation = -1;
        int texcoordsLocation = -1;
        // Uniforms
        int mvpLocation = -1;
        int depthBiasMVPLocation = -1;
        int shadowMapLocation = -1;
        int textureLocation = -1;
        int viewModelLocation = -1;
        int normalMatrixLocation = -1;
        int lightDirectionLocation = -1;
        int useStratifiedPoissonSamplingLocation = -1;
        int useModelTextureLocation = -1;
        // Phong 
        int diffuseLocation = -1;
        int ambientLocation = -1;
        int specularLocation = -1;
        int shininessLocation = -1;

        // Shader Parameters
        public float useStratifiedPoissonSampling = 0f;

        Matrix4 biasMatrix = new Matrix4(0.5f, 0.0f, 0.0f, 0.0f,
                                            0.0f, 0.5f, 0.0f, 0.0f,
                                            0.0f, 0.0f, 0.5f, 0.0f,
                                            0.5f, 0.5f, 0.5f, 1.0f);

        public ShadowMappingShader(String vshader, String fshader) : base(vshader, fshader)
        {
            // Get Locations of Attributes and Uniforms
            positionLocation = GL.GetAttribLocation(ProgramID, "i_position");
            normalLocation = GL.GetAttribLocation(ProgramID, "i_normal");
            texcoordsLocation = GL.GetAttribLocation(ProgramID, "i_texcoord");

            mvpLocation = GL.GetUniformLocation(ProgramID, "MVP");
            shadowMapLocation = GL.GetUniformLocation(ProgramID, "shadowMap");
            textureLocation = GL.GetUniformLocation(ProgramID, "modelTexture");
            depthBiasMVPLocation = GL.GetUniformLocation(ProgramID, "depthBiasMVP");
            viewModelLocation = GL.GetUniformLocation(ProgramID, "m_viewModel");
            normalMatrixLocation = GL.GetUniformLocation(ProgramID, "m_normal");
            lightDirectionLocation = GL.GetUniformLocation(ProgramID, "l_dir");
            useStratifiedPoissonSamplingLocation = GL.GetUniformLocation(ProgramID, "useStratifiedPoissonSampling");
            useModelTextureLocation = GL.GetUniformLocation(ProgramID, "useModelTexture");

            // Phong
            diffuseLocation = GL.GetUniformLocation(ProgramID, "diffuse");
            ambientLocation = GL.GetUniformLocation(ProgramID, "ambient");
            specularLocation = GL.GetUniformLocation(ProgramID, "specular");
            shininessLocation = GL.GetUniformLocation(ProgramID, "shininess");
        }

        // We actually use our own Quad Data so ignore the input
        public void bindModelData(WorldObject worldObject)
        {
            GL.BindVertexArray(worldObject.mesh.vertexArrayObject);

            // Set the types of buffer data
            GL.BindBuffer(BufferTarget.ArrayBuffer, worldObject.mesh.vertexBufferObject);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, worldObject.mesh.normalBufferObject);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, worldObject.mesh.indexBufferObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, worldObject.mesh.texcoordsBuffer);
            GL.VertexAttribPointer(texcoordsLocation, 2, VertexAttribPointerType.Float, true, 0, 0);

            GL.BindVertexArray(0);
        }

		private void uploadParameters(WorldObject worldObject, Matrix4 viewMatrix, Matrix4 projectionMatrix, Matrix4 depthViewProjectionMatrix, Vector3 lightDirection)
        {   
			// Matrices for Shader
			Matrix4 mv = worldObject.modelMatrix * viewMatrix;
			Matrix4 mvp = mv * projectionMatrix;
			Matrix4 mNormal = Utility.getNormalMatrix(mv);

			GL.UniformMatrix4(mvpLocation, false, ref mvp);
			GL.UniformMatrix4(viewModelLocation, false, ref mv);
			GL.UniformMatrix4(normalMatrixLocation, false, ref mNormal);

            // Set Light Direction
            GL.Uniform3(lightDirectionLocation, Vector3.Transform(lightDirection, viewMatrix.ClearTranslation()));

            // Compute the MVP matrix from the light's point of view (and add a bias)
            Matrix4 depthMVP = worldObject.modelMatrix * depthViewProjectionMatrix * biasMatrix;

            GL.UniformMatrix4(depthBiasMVPLocation, false, ref depthMVP);
            GL.Uniform1(shadowMapLocation, 0);
            GL.Uniform1(textureLocation, 1);

            GL.Uniform1(useStratifiedPoissonSamplingLocation, useStratifiedPoissonSampling);
            if(worldObject.mesh.isTextured == true)
                GL.Uniform1(useModelTextureLocation, 1f);
            else GL.Uniform1(useModelTextureLocation, 0f);

            // Phong
            GL.Uniform3(diffuseLocation, worldObject.diffuse);
            GL.Uniform3(ambientLocation, worldObject. ambient);
            GL.Uniform3(specularLocation, worldObject.specular);
            GL.Uniform1(shininessLocation, worldObject.shininess);
        }

        public void activate()
        {
            GL.UseProgram(ProgramID);
        }

		public void render(WorldObject worldObject, Matrix4 viewMatrix, Matrix4 projectionMatrix, Matrix4 depthViewProjectionMatrix, Vector3 lightDirection, int shadowMapTexture)
        {
            // Activate the states for the buffers etc.
            GL.BindVertexArray(worldObject.mesh.vertexArrayObject);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, shadowMapTexture);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, worldObject.mesh.textureID);

            // Enable the model data
            GL.EnableVertexAttribArray(positionLocation);
            GL.EnableVertexAttribArray(normalLocation);
            GL.EnableVertexAttribArray(texcoordsLocation);

            // Upload Parameters
            uploadParameters(worldObject, viewMatrix, projectionMatrix, depthViewProjectionMatrix, lightDirection);

            // Render Object      
            GL.DrawElements(BeginMode.Triangles, worldObject.mesh.indices.Count, DrawElementsType.UnsignedInt, 0);

            // Clean Up
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.DisableVertexAttribArray(positionLocation);
            GL.DisableVertexAttribArray(normalLocation);
            GL.DisableVertexAttribArray(texcoordsLocation);
        }
    }
}
