using System;
using OpenTK.Graphics.OpenGL;
using System.IO;

namespace Komorebi
{
    public  class ShaderProgram
    {
        public int ProgramID = -1;
        public int VShaderID = -1;
        public int FShaderID = -1;

        public ShaderProgram(String vshader, String fshader)
        {
            ProgramID = GL.CreateProgram();

            LoadShaderFromFile(vshader, ShaderType.VertexShader);
            LoadShaderFromFile(fshader, ShaderType.FragmentShader);        

            try
            {
                Link();
            }
            catch (Exception ex)
            {
                string hello = ex.Message;
            }
        }

        private void loadShader(String code, ShaderType type, out int address)
        {
            address = GL.CreateShader(type);
            GL.ShaderSource(address, code);
            GL.CompileShader(address);
            GL.AttachShader(ProgramID, address);
            Console.WriteLine(GL.GetShaderInfoLog(address));
        }

        public void LoadShaderFromFile(String filename, ShaderType type)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                if (type == ShaderType.VertexShader)
                {
                    loadShader(sr.ReadToEnd(), type, out VShaderID);
                }
                else if (type == ShaderType.FragmentShader)
                {
                    loadShader(sr.ReadToEnd(), type, out FShaderID);
                }
            }
        }

        public void Link()
        {
            GL.LinkProgram(ProgramID);
            Console.WriteLine(GL.GetProgramInfoLog(ProgramID));  
        }
    }
}
