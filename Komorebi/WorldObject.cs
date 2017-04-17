using System;
using OpenTK;

namespace Komorebi
{
    class WorldObject
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 rotation = Vector3.Zero;
        public Vector3 scale = Vector3.One;

		public Matrix4 modelMatrix = Matrix4.Identity;

        public Mesh mesh;

        // Material
        public float useStratifiedPoissonSampling = 0f;
        public Vector3 diffuse = new Vector3(1f, 1f, 1f);
        public Vector3 ambient = new Vector3(0.5f, 0.5f, 0.5f);
        public Vector3 specular = new Vector3(1f, 1f, 1f);
        public float shininess = 64f;

        public void getModelMatrix()
        {
            modelMatrix = Matrix4.CreateScale(scale) * 
                            Matrix4.CreateRotationX(rotation.X) * Matrix4.CreateRotationY(rotation.Y) * Matrix4.CreateRotationZ(rotation.Z) * 
                            Matrix4.CreateTranslation(position);
        }

        public void loadModelFromFile(String filename)
        {
            mesh = MeshManager.getMesh(filename);
        }

		// Do not call update if this is a static Object
        public void update()
        {
            // Update model view matrices
			getModelMatrix();
        }
    }
}
