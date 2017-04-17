using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Komorebi
{
    class PhongShader : ShaderProgram
    {
        // Locations of the Attributes and Uniforms in the Shader
        int positionLocation = -1;
        int normalLocation = -1;
        // Uniforms
        int pvmLocation = -1;
        int viewModelLocation = -1;
        int normalMatrixLocation = -1;
        int lightDirectionLocation = -1;

        // Parameters of the Shader
        public Vector3 lightDirection = new Vector3(2f, 3f, 7f);

        public PhongShader(String vshader, String fshader) : base(vshader, fshader)
        {
            positionLocation = GL.GetAttribLocation(ProgramID, "i_position");
            normalLocation = GL.GetAttribLocation(ProgramID, "i_normal");

            pvmLocation = GL.GetUniformLocation(ProgramID, "m_pvm");
            viewModelLocation = GL.GetUniformLocation(ProgramID, "m_viewModel");
            normalMatrixLocation = GL.GetUniformLocation(ProgramID, "m_normal");
            lightDirectionLocation = GL.GetUniformLocation(ProgramID, "l_dir");
        }

        // Upload the model data to be used by this shader
        public void bindModelData(WorldObject worldObject)
        {
            GL.BindVertexArray(worldObject.mesh.vertexArrayObject);

            // Set the types of buffer data
            GL.BindBuffer(BufferTarget.ArrayBuffer, worldObject.mesh.vertexBufferObject);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, worldObject.mesh.normalBufferObject);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 0, 0);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, worldObject.mesh.indexBufferObject);

            GL.BindVertexArray(0);
        }

		void uploadParameters(Matrix4 modelMatrix, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            // Matrices for Shader
			Matrix4 mv = modelMatrix * viewMatrix;
			Matrix4 mvp = mv * projectionMatrix;
			Matrix4 mNormal = Utility.getNormalMatrix(mv);

			GL.UniformMatrix4(pvmLocation, false, ref mvp);
			GL.UniformMatrix4(viewModelLocation, false, ref mv);
			GL.UniformMatrix4(normalMatrixLocation, false, ref mNormal);

            GL.Uniform3(lightDirectionLocation, Vector3.Transform(lightDirection, viewMatrix.ClearTranslation()));
        }

        public void activate()
        {
            GL.UseProgram(ProgramID);
        }

		public void render(WorldObject worldObject, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            // Activate the states for the buffers etc.
            GL.BindVertexArray(worldObject.mesh.vertexArrayObject);

            GL.BindTexture(TextureTarget.Texture2D, -1);

            // Enable the model data
            GL.EnableVertexAttribArray(positionLocation);
            GL.EnableVertexAttribArray(normalLocation);

            // Upload Parameters
			uploadParameters(worldObject.modelMatrix, viewMatrix, projectionMatrix);

            // Render Object      
            GL.DrawElements(BeginMode.Triangles, worldObject.mesh.indices.Count, DrawElementsType.UnsignedInt, 0);

            // Clean Up
            GL.BindVertexArray(0);
            GL.DisableVertexAttribArray(positionLocation);
            GL.DisableVertexAttribArray(normalLocation);
        }
    }
}
