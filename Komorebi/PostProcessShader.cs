using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Komorebi
{
    class PostProcessShader : ShaderProgram
    {
        // Locations of the Attributes and Uniforms in the Shader
        int positionLocation = -1;
        int texcoordsLocation = -1;
        // Uniforms
        int exposureLocation = -1;
        int decayLocation = -1;
        int densityLocation = -1;
        int weightLocation = -1;
        int lightPositionOnScreenLocation = -1;
        int textureLocation = -1;
        int noPostProcessingLocation = -1;
        int uShiftLocation = -1;
        int depthTextureLocation = -1;

        // Shader Parameters
        float decay = 0.95f;
        float exposure = 0.4f;
        float density = 0.89f;
        float weight = 0.9f;

        public float noPostProcessing = 0.0f;
        public Vector2 uShift = new Vector2(0f);

        // Back-Buffers
        public int fbo, fbo_texture;
        int vbo_fbo_vertices, tbo_fbo_texcoords;
        int vertexArrayObject;

        public PostProcessShader(String vshader, String fshader) : base(vshader, fshader)
        {
            // Get Locations of Attributes and Uniforms
            positionLocation = GL.GetAttribLocation(ProgramID, "i_position");
            texcoordsLocation = GL.GetAttribLocation(ProgramID, "i_texcoord");

            exposureLocation = GL.GetUniformLocation(ProgramID, "exposure");
            decayLocation = GL.GetUniformLocation(ProgramID, "decay");
            densityLocation = GL.GetUniformLocation(ProgramID, "density");
            weightLocation = GL.GetUniformLocation(ProgramID, "weight");
            lightPositionOnScreenLocation = GL.GetUniformLocation(ProgramID, "lightPositionOnScreen");
            textureLocation = GL.GetUniformLocation(ProgramID, "fboTexture");
            noPostProcessingLocation = GL.GetUniformLocation(ProgramID, "noPostProcessing");
            uShiftLocation = GL.GetUniformLocation(ProgramID, "uShift");
            depthTextureLocation = GL.GetUniformLocation(ProgramID, "depthTexture");

            // Create back-buffer, used for post-processing
            //Texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.GenTextures(1, out fbo_texture);
            GL.BindTexture(TextureTarget.Texture2D, fbo_texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureParameterName.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureParameterName.ClampToEdge);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Window.screenWidth, Window.screenHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Framebuffer to link everything together 
            GL.GenFramebuffers(1, out fbo);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, fbo_texture, 0);

            FramebufferErrorCode status;
            if ((status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer)) != FramebufferErrorCode.FramebufferComplete)
            {
                Console.Out.Write("glCheckFramebufferStatus: error: " + status);
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // Create the Framebuffer Texture Vertices
            Vector3[] fbo_vertices = {
                new Vector3(-1, 1, 0), // Top Left
                new Vector3(-1, -1, 0), // Bottom Left
                new Vector3(1, 1, 0), // Top Right
                new Vector3(1, -1, 0) // Bottom Right
            };
            Vector2[] fbo_texcoords = {
                new Vector2(0f, 1f), // Top Left
                new Vector2(0f, 0f), // Bottom Left
                new Vector2(1f, 1f), // Top Right
                new Vector2(1f, 0f) // Bottom Right
            };

            // Create the Vertex Array Object     
            GL.GenVertexArrays(1, out vertexArrayObject);
            GL.BindVertexArray(vertexArrayObject);

            GL.GenBuffers(1, out vbo_fbo_vertices);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_fbo_vertices);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(fbo_vertices.Length * Vector3.SizeInBytes), fbo_vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Texture Coordinates
            GL.GenBuffers(1, out tbo_fbo_texcoords);
            GL.BindBuffer(BufferTarget.ArrayBuffer, tbo_fbo_texcoords);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(fbo_texcoords.Length * Vector2.SizeInBytes), fbo_texcoords, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void resize()
        {
            GL.BindTexture(TextureTarget.Texture2D, fbo_texture);  
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Window.screenWidth, Window.screenHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        // We actually use our own Quad Data so ignore the input
        public void bindModelData(WorldObject sun)
        {
            GL.BindVertexArray(vertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo_fbo_vertices);
            GL.VertexAttribPointer(
              positionLocation,  // attribute
              3,                  // number of elements per vertex, here (x,y,z)
              VertexAttribPointerType.Float,           // the type of each element
              false,           // take our values as-is
              0,                  // no extra data between each position
              0                   // offset of first element
            );
            GL.BindBuffer(BufferTarget.ArrayBuffer, tbo_fbo_texcoords);
            GL.VertexAttribPointer(
              texcoordsLocation,  // attribute
              2,                  // number of elements per vertex, here (x,y)
              VertexAttribPointerType.Float,           // the type of each element
              true,           // take our values as-is
              0,                  // no extra data between each position
              0                   // offset of first element
            );
            GL.BindVertexArray(0);
        }

		private void uploadParameters(Vector3 lightPosition, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            GL.Uniform1(textureLocation, 0);
            GL.Uniform1(depthTextureLocation, 1);

            GL.Uniform1(exposureLocation, exposure);
            GL.Uniform1(decayLocation, decay);
            GL.Uniform1(densityLocation, density);
            GL.Uniform1(weightLocation, weight);

			Vector3 lightPos = Utility.transformWorldPositionToScreenSpace(lightPosition, viewMatrix, projectionMatrix);
            GL.Uniform3(lightPositionOnScreenLocation, lightPos);

            // Radial Blur?
            GL.Uniform1(noPostProcessingLocation, noPostProcessing);
            GL.Uniform2(uShiftLocation, uShift);
        }

        public void activate()
        {
            GL.UseProgram(ProgramID);
        }

		public void render(Vector3 lightPosition, int depthMapTexture, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            // Set some settings
            GL.BindVertexArray(vertexArrayObject);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, fbo_texture);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, depthMapTexture);

            // Enable Position Attribute
            GL.EnableVertexAttribArray(positionLocation);
            GL.EnableVertexAttribArray(texcoordsLocation);

            // Upload Parameters
            uploadParameters(lightPosition, viewMatrix, projectionMatrix);

            // Enable Texture and set Blending
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);

            // Render the Quad
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            // Clean up and set Blending
            GL.DisableVertexAttribArray(positionLocation);
            GL.DisableVertexAttribArray(texcoordsLocation);
            GL.BindVertexArray(0);
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.Disable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }
    }
}
