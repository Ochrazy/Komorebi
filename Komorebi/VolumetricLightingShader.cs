using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Komorebi
{
    class VolumetricLightingShader : ShaderProgram
    {
        // Locations of the Attributes and Uniforms in the Shader
        int positionLocation = -1;
        int texcoordsLocation = -1;
        // Uniforms
        int depthTextureLocation = -1;
        int shadowTextureLocation = -1;
        int noiseTextureLocation = -1;
        int inverseViewProjectionMatrixLocation = -1;
        int viewPosLocation = -1;
        int depthBiasMVPLocation = -1;
        int noiseOffsetLocation = -1;
        int enumScatteringTechniqueLocation = -1;

        // Shader Parameters
        public float enumScatteringTechnique = 0.0f;

        // Back-Buffers
        int vbo_fbo_vertices, tbo_fbo_texcoords;
        int vertexArrayObject;
        int noiseTexture = -1;

        Random rand = new Random();

        int intNoiseOffsetX = 0;
        int intNoiseOffsetY = 0;

        Matrix4 biasMatrix = new Matrix4(0.5f, 0.0f, 0.0f, 0.0f,
                                      0.0f, 0.5f, 0.0f, 0.0f,
                                      0.0f, 0.0f, 0.5f, 0.0f,
                                      0.5f, 0.5f, 0.5f, 1.0f);

        public VolumetricLightingShader(String vshader, String fshader) : base(vshader, fshader)
        {
            // ToDo: automate this!
            // Get Locations of Attributes and Uniforms
            positionLocation = GL.GetAttribLocation(ProgramID, "i_position");
            texcoordsLocation = GL.GetAttribLocation(ProgramID, "i_texcoord");

            depthTextureLocation = GL.GetUniformLocation(ProgramID, "depthTexture");
            shadowTextureLocation = GL.GetUniformLocation(ProgramID, "shadowMapTexture");
            noiseTextureLocation = GL.GetUniformLocation(ProgramID, "noiseTexture");
            inverseViewProjectionMatrixLocation = GL.GetUniformLocation(ProgramID, "invVP");
            viewPosLocation = GL.GetUniformLocation(ProgramID, "viewPos");
            depthBiasMVPLocation = GL.GetUniformLocation(ProgramID, "depthBiasMVP");
            noiseOffsetLocation = GL.GetUniformLocation(ProgramID, "noiseOffset");
            enumScatteringTechniqueLocation = GL.GetUniformLocation(ProgramID, "enumScatteringTechnique");

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

            // Create Noise Texture
            GL.GenTextures(1, out noiseTexture);
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            int noiseTextureSize = 8; // bilateral blur, 1 center tap and 4 on each side
            byte[] noiseTex = new byte[noiseTextureSize * noiseTextureSize * sizeof(byte)];      
            rand.NextBytes(noiseTex); // easy

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, noiseTextureSize, noiseTextureSize, 0, PixelFormat.Red, PixelType.UnsignedByte, noiseTex);
            GL.BindTexture(TextureTarget.Texture2D, -1);

            intNoiseOffsetX = (int)(rand.NextDouble() * 128f);
            intNoiseOffsetY = (int)(rand.NextDouble() * 128f);
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

        /**
	    *	compute the inverse matrix (the correct??? way)
	    */
        void inverse(ref Matrix4 mat)
        {
            Matrix4 ret = new Matrix4();
            float det;
            det = mat.M11 * mat.M22 * mat.M33;
            det += mat.M21 * mat.M32 * mat.M13;
            det += mat.M31 * mat.M12 * mat.M23;
            det -= mat.M31 * mat.M22 * mat.M13;
            det -= mat.M21 * mat.M12 * mat.M33;
            det -= mat.M11 * mat.M32 * mat.M23;
            det = 1.0f / det;               //pas de sécurité pour division pas zéro...
            ret.M11 = (mat.M22 * mat.M33 - mat.M32 * mat.M23) * det;
            ret.M12 = -(mat.M12 * mat.M33 - mat.M32 * mat.M13) * det;
            ret.M13 = (mat.M12 * mat.M23 - mat.M22 * mat.M13) * det;
            ret.M14 = 0.0f;
            ret.M21 = -(mat.M21 * mat.M33 - mat.M31 * mat.M23) * det;
            ret.M22 = (mat.M11 * mat.M33 - mat.M31 * mat.M13) * det;
            ret.M23 = -(mat.M11 * mat.M23 - mat.M21 * mat.M13) * det;
            ret.M24 = 0.0f;
            ret.M31 = (mat.M21 * mat.M32 - mat.M31 * mat.M22) * det;
            ret.M32 = -(mat.M11 * mat.M32 - mat.M31 * mat.M12) * det;
            ret.M33 = (mat.M11 * mat.M22 - mat.M21 * mat.M12) * det;
            ret.M34 = 0.0f;
            ret.M41 = -(mat.M41 * ret.M11 + mat.M42 * ret.M21 + mat.M43 * ret.M31);
            ret.M42 = -(mat.M41 * ret.M12 + mat.M42 * ret.M22 + mat.M43 * ret.M32);
            ret.M43 = -(mat.M41 * ret.M13 + mat.M42 * ret.M23 + mat.M43 * ret.M33);
            ret.M44 = 1.0f;
            mat = ret;
        }

        private void uploadParameters(Vector3 lightPosition, Camera camera, Matrix4 projectionMatrix, Matrix4 depthViewProjectionMatrix)
        {
            GL.Uniform1(depthTextureLocation, 0);
            GL.Uniform1(shadowTextureLocation, 1);
            GL.Uniform1(noiseTextureLocation, 2);

            Matrix4 invVP = camera.viewMatrix * projectionMatrix;
            inverse(ref invVP);
        
            GL.UniformMatrix4(inverseViewProjectionMatrixLocation, false, ref invVP);

            GL.Uniform3(viewPosLocation, camera.Position);

            // Compute the MVP matrix from the light's point of view (and add a bias)
            Matrix4 depthMVP = depthViewProjectionMatrix * biasMatrix;
            GL.UniformMatrix4(depthBiasMVPLocation, false, ref depthMVP);
           
            Vector2 noiseOffset = new Vector2(intNoiseOffsetX, intNoiseOffsetY);
            GL.Uniform2(noiseOffsetLocation, noiseOffset);

            GL.Uniform1(enumScatteringTechniqueLocation, enumScatteringTechnique);
        }

        public void activate()
        {
            GL.UseProgram(ProgramID);
        }

		public void render(Vector3 lightPosition, Camera camera, Matrix4 projectionMatrix, int depthMapTexture, int shadowMapTexture, Matrix4 depthViewProjectionMatrix)
        {
            // Set some settings
            GL.BindVertexArray(vertexArrayObject);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, depthMapTexture);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, shadowMapTexture);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, noiseTexture);

            // Enable Position Attribute
            GL.EnableVertexAttribArray(positionLocation);
            GL.EnableVertexAttribArray(texcoordsLocation);

            // Upload Parameters
            uploadParameters(lightPosition, camera, projectionMatrix, depthViewProjectionMatrix);

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
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, -1);
            GL.Disable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }
    }
}
