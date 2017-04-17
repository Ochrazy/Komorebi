using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Linq;

namespace Komorebi
{
    class DepthMapShader : ShaderProgram
    {
        // Locations of the Attributes and Uniforms in the Shader
        int positionLocation = -1;
        int normalLocation = -1;
        // Uniforms
        int depthMVPLocation = -1;
        int normalMatrixLocation = -1;

        // Orthogonal Projection from the lights POV (ToDo: calculate the best Frustum, instead of hardcoding it here)
        public Matrix4 depthProjectionMatrix = Matrix4.CreateOrthographicOffCenter(-10f, 37, -5, 39, -110, 110);
        Matrix4 depthViewMatrix = Matrix4.Identity;
        public Matrix4 depthViewProjectionMatrix = Matrix4.Identity;

        // Shader Parameters
        public int depthMapSizeX, depthMapSizeY;
        public Vector3 lightDirection = new Vector3(2f, 0.4f, 3.5f);
		//public Vector3 lightDirection = new Vector3(0f, 0f, 1f);
        public int fbo, depthTexture;

        public DepthMapShader(String vshader, String fshader, int sizeX = 2048, int sizeY = 2048) : base(vshader, fshader)
        {
            // Get Locations of Attributes and Uniforms
            positionLocation = GL.GetAttribLocation(ProgramID, "i_position");
            normalLocation = GL.GetAttribLocation(ProgramID, "i_normal");
            depthMVPLocation = GL.GetUniformLocation(ProgramID, "depthMVP");
            normalMatrixLocation = GL.GetUniformLocation(ProgramID, "m_normal");

            // Set Size of the texture
            depthMapSizeX = sizeX;
            depthMapSizeY = sizeY;

            //Texture
            GL.GenTextures(1, out depthTexture);
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
           // GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (float)TextureCompareMode.CompareRefToTexture); // Use these two for ShadowMapping: texture2dShadow
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)All.Lequal);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, depthMapSizeX, depthMapSizeY, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, -1);

            // Framebuffer to link everything together 
            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, depthTexture, 0);
            GL.DrawBuffer(DrawBufferMode.None);

            FramebufferErrorCode status;
            if ((status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer)) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.Out.Write("glCheckFramebufferStatus: error: " + status);
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // ShadowMapping
        public void enableTextureCompare()
        {
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (float)TextureCompareMode.CompareRefToTexture); // Use these two for ShadowMapping: texture2dShadow
            GL.BindTexture(TextureTarget.Texture2D, -1);
        }

        public void disableTextureCompare()
        {
            GL.BindTexture(TextureTarget.Texture2D, depthTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (float)TextureCompareMode.None); // Use these two for ShadowMapping: texture2dShadow
            GL.BindTexture(TextureTarget.Texture2D, -1);
        }

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

		void uploadParameters(Matrix4 modelMatrix, Matrix4 viewMatrix)
        {
            // Compute the MVP matrix from the light's point of view
            Matrix4 depthMVP = modelMatrix * depthViewProjectionMatrix;

            // Send our transformation to the currently bound shader
			Matrix4 normalMatrix = Utility.getNormalMatrix(modelMatrix * viewMatrix);
            GL.UniformMatrix4(depthMVPLocation, false, ref depthMVP);
			GL.UniformMatrix4(normalMatrixLocation, false, ref normalMatrix);
        }

        public void activate()
        {
            GL.UseProgram(ProgramID);

            // The light direction should stay the same for every Object in a Frame
            depthViewMatrix = Matrix4.LookAt(lightDirection, new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            depthViewProjectionMatrix = depthViewMatrix * depthProjectionMatrix;
        }

        // Render Objects into the ShadowMap
		public void render(WorldObject worldObject, Matrix4 viewMatrix)
        {
            // Activate the states for the buffers etc.
            GL.BindVertexArray(worldObject.mesh.vertexArrayObject);

            GL.BindTexture(TextureTarget.Texture2D, -1);

            // Enable the model data
            GL.EnableVertexAttribArray(positionLocation);
            GL.EnableVertexAttribArray(normalLocation);

            // Upload Parameters
			uploadParameters(worldObject.modelMatrix, viewMatrix);

            // Render Object      
            GL.DrawElements(BeginMode.Triangles, worldObject.mesh.indices.Count, DrawElementsType.UnsignedInt, 0);

            // Clean Up
            GL.BindVertexArray(0);
            GL.DisableVertexAttribArray(positionLocation);
            GL.DisableVertexAttribArray(normalLocation);
        }
    }
}
