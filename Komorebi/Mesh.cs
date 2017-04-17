using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Assimp;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Linq;

namespace Komorebi
{
    class Mesh
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<int> indices = new List<int>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector3> colorData = new List<Vector3>();

        public bool isTextured = false;
        public int textureID = -1;
        public int texcoordsBuffer = -1;
        public List<Vector2> textureCoordinates = new List<Vector2>();

        // Vertex, Normal and Index Buffer Objects
        public int vertexBufferObject, normalBufferObject, indexBufferObject;
        public int vertexArrayObject;

        public Mesh(String filename)
        {
            loadMeshFromFile(filename);
        }

        public void loadMeshFromFile(String filename)
        {
            //Filepath to our model
            String fileName = FileManager.getMediaFile(filename);

            //Create a new importer
            AssimpContext importer = new AssimpContext();

            ////This is how we add a logging callback 
            //LogStream logstream = new LogStream(delegate (String msg, String userData)
            //{
            //    Console.WriteLine(msg);
            //});
            //logstream.Attach();

            //Import the model. The model is imported, loaded into managed memory. 
            // Then the unmanaged memory is released, and everything is reset.
            Scene model = importer.ImportFile(fileName, PostProcessPreset.TargetRealTimeMaximumQuality);

            // Load the model data into our own structures
            for (var i = 0; i < model.MeshCount; i++)
            {
                // Vertices
                for (var currentVertex = 0; currentVertex < model.Meshes[i].VertexCount; currentVertex++)
                {
                    Vector3D vector = model.Meshes[i].Vertices[currentVertex];
                    vertices.Add(new Vector3(vector.X, vector.Y, vector.Z));
                    colorData.Add(new Vector3(0.5f, 0.5f, 0.5f));
                }

                // Normals
                for (var currentNormal = 0; currentNormal < model.Meshes[i].Normals.Count; currentNormal++)
                {
                    Vector3D normal = model.Meshes[i].Normals[currentNormal];
                    normals.Add(new Vector3(normal.X, normal.Y, normal.Z));
                }

                // Material
                if (model.Materials[0].HasTextureDiffuse == true)
                {
                    isTextured = true;

                    textureCoordinates.Capacity = model.Meshes[0].TextureCoordinateChannels[0].Count;
                    foreach (Assimp.Vector3D element in model.Meshes[0].TextureCoordinateChannels[0])
                    {
                        textureCoordinates.Add(new Vector2(element.X, element.Y));
                    }

                    textureID = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, textureID);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                    // Use anisotropic Filtering if available
                    var extensions = GL.GetString(StringName.Extensions).Split(' ');
                    if (extensions.Contains("GL_EXT_texture_filter_anisotropic"))
                    {
                        int max_aniso = GL.GetInteger((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt);
                        GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, max_aniso);
                    }

                    Bitmap bmp = new Bitmap(FileManager.getMediaFile("textures/" + model.Materials[0].TextureDiffuse.FilePath));
                    BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

                    bmp.UnlockBits(bmp_data);

                    // Generate MipMaps (especially to get rid of texture flickering)
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

                }
                GL.BindTexture(TextureTarget.Texture2D, -1);

                // Indices
                indices.AddRange(model.Meshes[i].GetIndices());
            }

            //End of example
            importer.Dispose();

            // Generate the buffers
            generateBuffers();
        }

        public void generateBuffers()
        {
            // Create the buffers
            GL.GenBuffers(1, out vertexBufferObject);
            if (normals.Count != 0) GL.GenBuffers(1, out normalBufferObject);
            if (indices.Count != 0) GL.GenBuffers(1, out indexBufferObject);

            // Create the Vertex Array Object     
            GL.GenVertexArrays(1, out vertexArrayObject);
            GL.BindVertexArray(vertexArrayObject);

            // Position and Normals never change so upload the data to the Buffer (gfx card)
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Count * Vector3.SizeInBytes), vertices.ToArray(), BufferUsageHint.StaticDraw);

            if (normals.Count != 0)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalBufferObject);
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(normals.Count * Vector3.SizeInBytes), normals.ToArray(), BufferUsageHint.StaticDraw);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            if (indices.Count != 0)
            {
                // Buffer index data
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferObject);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indices.Count * sizeof(int)), indices.ToArray(), BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            }

            if (isTextured == true)
            {
                // Texture Coordinates
                GL.GenBuffers(1, out texcoordsBuffer);
                GL.BindBuffer(BufferTarget.ArrayBuffer, texcoordsBuffer);
                GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(textureCoordinates.Count * Vector2.SizeInBytes), textureCoordinates.ToArray(), BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }

            GL.BindVertexArray(0);
        }

        public void updateTextureCoordinates()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, texcoordsBuffer);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(textureCoordinates.Count * Vector2.SizeInBytes), textureCoordinates.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }
}
