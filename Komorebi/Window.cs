using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Komorebi
{
    class Window : GameWindow
    {
		public Window() : base(screenWidth, screenHeight, new GraphicsMode(32, 24, 0, 8), "Komorebi", 
			GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Default)
        {
            Console.Out.WriteLine(" GL Version: " + GL.GetString(StringName.Version) + " | GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
        }

        public static int screenWidth = 1280, screenHeight = 768;
		public Matrix4 projectionMatrix = Matrix4.Identity;  
        Camera camera = new Camera();

        public WorldObject rmonkey = new WorldObject();
        public WorldObject monkey = new WorldObject();
        public WorldObject house = new WorldObject();
        public WorldObject magicBox = new WorldObject();
        public WorldObject sun = new WorldObject();
        public WorldObject floor = new WorldObject();
        public List<WorldObject> trees = new List<WorldObject>();

        // Load shaders from file
        PhongShader phongShader; 
		public PostProcessShader postProcessShader;
        public VolumetricLightingShader volumetricLightingShader;
        public PostProcessShader blurShader;
        public PostProcessShader blurShader2;
        DepthMapShader shadowMapShader;
        public DepthMapShader depthMapShader;
        public ShadowMappingShader shadowMappingShader;
        // ToDo: Create Shader Classes for the sun and occluding objects (but the PhongShader class works too)
        PhongShader blackShader; 
		PhongShader whiteShader;

        public bool onlyShowDepthMap = false; // shadowMap, for Debugging
        public bool onlyShowPostProcessingTexture = false; // for debugging
        public bool enableShadows = true;
        public bool enablePostProcessing = true;
        public float blurRadius = 0f; // No Blur at start
        public bool useRayMarching = false;

        InputManager inputManager;

        float time = 0;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            inputManager = new InputManager(this, camera);

            phongShader = new PhongShader(FileManager.getMediaFile("phongVS.glsl"), FileManager.getMediaFile("phongFS.glsl"));
            postProcessShader = new PostProcessShader(FileManager.getMediaFile("postProcessVS.glsl"), FileManager.getMediaFile("postProcessFS.glsl"));
            volumetricLightingShader = new VolumetricLightingShader(FileManager.getMediaFile("volumetricLightingVS.glsl"), FileManager.getMediaFile("volumetricLightingFS.glsl"));
            blurShader = new PostProcessShader(FileManager.getMediaFile("bilinearGaussianBlurVS.glsl"), FileManager.getMediaFile("bilinearGaussianBlurFS.glsl"));
            blurShader2 = new PostProcessShader(FileManager.getMediaFile("bilinearGaussianBlurVS.glsl"), FileManager.getMediaFile("bilinearGaussianBlurFS.glsl"));
            shadowMapShader = new DepthMapShader(FileManager.getMediaFile("depthMapVS.glsl"), FileManager.getMediaFile("depthMapFS.glsl"));
            depthMapShader = new DepthMapShader(FileManager.getMediaFile("depthMapVS.glsl"), FileManager.getMediaFile("depthMapFS.glsl"), screenWidth, screenHeight); 
            shadowMappingShader = new ShadowMappingShader(FileManager.getMediaFile("shadowMappingVS.glsl"), FileManager.getMediaFile("shadowMappingFS.glsl"));
            blackShader = new PhongShader(FileManager.getMediaFile("blackVS.glsl"), FileManager.getMediaFile("blackFS.glsl"));
			whiteShader = new PhongShader(FileManager.getMediaFile("whiteVS.glsl"), FileManager.getMediaFile("whiteFS.glsl"));

            // Load Models
            sun.loadModelFromFile("sun.3ds");
            sun.position = new Vector3(20f, 30f, 35f);
            sun.scale = new Vector3(2f);
			sun.update();

            monkey.loadModelFromFile("monkey.3ds");
            monkey.position = new Vector3(30f, 4f, 2f);
			monkey.update();
            monkey.diffuse = new Vector3(0.1f, 0.1f, 0.8f);
            monkey.ambient = new Vector3(0.05f, 0.05f, 0.4f);

            rmonkey.loadModelFromFile("monkey.3ds");
            rmonkey.position = new Vector3(18.5f, 2f, -27.5f);
            rmonkey.update();
            rmonkey.diffuse = new Vector3(0.1f, 0.1f, 0.8f);
            rmonkey.ambient = new Vector3(0.05f, 0.05f, 0.4f);
            rmonkey.diffuse = new Vector3(0.1f, 0.1f, 0.1f);
            rmonkey.ambient = new Vector3(0.05f, 0.05f, 0.05f);
            rmonkey.specular = new Vector3(0f);
            rmonkey.shininess = 1f;

            house.loadModelFromFile("house.3ds");
            house.position = new Vector3(15f, 3.01f, -34f);
            house.scale = new Vector3(3f);
            house.update();
            house.diffuse = new Vector3(0.1f, 0.1f, 0.1f);
            house.ambient = new Vector3(0.05f, 0.05f, 0.05f);
            house.specular = new Vector3(0f);
            house.shininess = 1f;

            magicBox.loadModelFromFile("magicBox.3ds");
            magicBox.position = new Vector3(30f, 6f, 0f);
            magicBox.scale = new Vector3(5f);
			magicBox.update();
            magicBox.diffuse = new Vector3(0.2f, 0.2f, 0f);
            magicBox.ambient = new Vector3(0.1f, 0.1f, 0f);
            // No Specular for the magicBox
            magicBox.specular = new Vector3(0f);
            magicBox.shininess = 1f;

            //Load floor
            floor.loadModelFromFile("floor.3ds");
            floor.position = new Vector3(0f, 0f, -10f);
            // Scale the Floor (and the texCoords)
            floor.scale = new Vector3(1000f, 1f, 1000f);
            //floor.scale = new Vector3(0.05f, 0.05f, 0.05f);
           // floor.rotation = new Vector3(45, 0, 0);
             for (int i = 0; i < floor.mesh.textureCoordinates.Count; i++)
              floor.mesh.textureCoordinates[i] *= 200f;
             floor.mesh.updateTextureCoordinates();
            floor.update();
            // No Specular for floor
            floor.specular = new Vector3(0f);
            floor.shininess = 1f;

            //Load trees
            Random random = new Random();
            for (int x = 0; x < 2; x++)
            {
                for (int z = 0; z < 10; z++)
                {
                    WorldObject tree = new WorldObject();
                    tree.loadModelFromFile("tree.3ds");
                    tree.position = new Vector3((x * 10f) - 5f, 0f, -z * 6f);
                    tree.scale = new Vector3(2f);
                    tree.rotation = new Vector3(0f, (float)(random.NextDouble() * Math.PI * 2.0), 0f);
					tree.update();
                    tree.diffuse = new Vector3(0.6f, 0.3f, 0f);
                    tree.ambient = new Vector3(0.3f, 0.15f, 0f);
                    tree.specular = new Vector3(0f);
                    tree.shininess = 1f;
                    trees.Add(tree);
                }
            }

            // Move initial Position of the Camera away from origin
            camera.Position += new Vector3(0f, 0.5f, 0f);

            // Hide Mouse Cursor
            CursorVisible = false;
        }

        // ToDo: create RenderScene(shader xy) method (rewrite inputs of render functions)
        void renderScene()
        {
            // ------------- ShadowMapping -------------------
            if (enableShadows == true)
            {
                if (onlyShowDepthMap == false)
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadowMapShader.fbo);
                    GL.Viewport(0, 0, shadowMapShader.depthMapSizeX, shadowMapShader.depthMapSizeY);
                }
                else
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.Viewport(0, 0, screenWidth, screenHeight);
                }
                GL.ClearColor(Color.Black);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Enable(EnableCap.DepthTest);

                // Use SadowMap Shader
                shadowMapShader.activate();

                // Render floor
                shadowMapShader.bindModelData(floor);
                shadowMapShader.render(floor, camera.viewMatrix);

                // Render Trees
                shadowMapShader.bindModelData(trees[0]);
                foreach (WorldObject tree in trees)
                {
                    shadowMapShader.render(tree, camera.viewMatrix);
                }

                // Render Phong-Monkey
                shadowMapShader.bindModelData(monkey);
                shadowMapShader.render(monkey, camera.viewMatrix);

                shadowMapShader.bindModelData(rmonkey);
                shadowMapShader.render(rmonkey, camera.viewMatrix);

                GL.Disable(EnableCap.CullFace);
                // House
                shadowMapShader.bindModelData(house);
                shadowMapShader.render(house, camera.viewMatrix);
                GL.Enable(EnableCap.CullFace);
                // Render Ground
                shadowMapShader.bindModelData(magicBox);
                shadowMapShader.render(magicBox, camera.viewMatrix);

                //if (useRayMarching == true)
                //{
                    // ------------- DepthMap -------------------
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapShader.fbo);
                    GL.Viewport(0, 0, screenWidth, screenHeight);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    GL.Enable(EnableCap.DepthTest);

                    // Use DepthMap Shader
                    depthMapShader.activate();
                    // Render from the Perspective of the camera
                    depthMapShader.depthViewProjectionMatrix = camera.GetViewMatrix() * projectionMatrix;

                    // Render floor
                    depthMapShader.bindModelData(floor);
                    depthMapShader.render(floor, camera.viewMatrix);

                    // Render Trees
                    depthMapShader.bindModelData(trees[0]);
                    foreach (WorldObject tree in trees)
                    {
                        depthMapShader.render(tree, camera.viewMatrix);
                    }

                    // Render Phong-Monkey
                    depthMapShader.bindModelData(monkey);
                    depthMapShader.render(monkey, camera.viewMatrix);

                    depthMapShader.bindModelData(rmonkey);
                    depthMapShader.render(rmonkey, camera.viewMatrix);

                    GL.Disable(EnableCap.CullFace);
                    // Render House
                    depthMapShader.bindModelData(house);
                    depthMapShader.render(house, camera.viewMatrix);
                    GL.Enable(EnableCap.CullFace);
                    // Render Ground
                    depthMapShader.bindModelData(magicBox);
                    depthMapShader.render(magicBox, camera.viewMatrix);                   
                //}
            }

            if (onlyShowDepthMap == false)
            {
                if (useRayMarching == false)
                {
                    //  ------------- Light Shaft Occlusion Objects -------------------
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, postProcessShader.fbo);
                    GL.Viewport(0, 0, screenWidth, screenHeight);
                    GL.ClearColor(new Color4(0.05f, 0.05f, 0.075f, 1f));
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    GL.Enable(EnableCap.DepthTest);

                    // Render SUN
                    whiteShader.activate();
                    whiteShader.bindModelData(sun);
                    whiteShader.render(sun, camera.viewMatrix, projectionMatrix);

                    // Render floor
                    blackShader.activate();
                    blackShader.bindModelData(floor);
                    blackShader.render(floor, camera.viewMatrix, projectionMatrix);

                    // Render Trees
                    blackShader.bindModelData(trees[0]);
                    foreach (WorldObject tree in trees)
                    {
                        blackShader.render(tree, camera.viewMatrix, projectionMatrix);
                    }

                    // Render Monkey
                    blackShader.bindModelData(monkey);
                    blackShader.render(monkey, camera.viewMatrix, projectionMatrix);

                    blackShader.bindModelData(rmonkey);
                    blackShader.render(rmonkey, camera.viewMatrix, projectionMatrix);

                    GL.Disable(EnableCap.CullFace);
                    // Render House
                    blackShader.bindModelData(house);
                    blackShader.render(house, camera.viewMatrix, projectionMatrix);
                    GL.Enable(EnableCap.CullFace);
                    // Ground
                    blackShader.bindModelData(magicBox);
                    blackShader.render(magicBox, camera.viewMatrix, projectionMatrix);
                }

                // ------------- Render to normal Framebuffer -------------------
                // Render to normal Framebuffer
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                //GL.Viewport(0, 0, 1440, 900);
                GL.ClearColor(Color.Black);
                //if (useRayMarching == true)
                   // GL.ClearColor(new Color4(0.3f, 0.1f, 0.0f, 1f));
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Disable(EnableCap.Blend);
                GL.BlendEquation(BlendEquationMode.FuncAdd);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

                if (onlyShowPostProcessingTexture == false)
                {
                    if (enableShadows == true)
                    {
                        shadowMapShader.enableTextureCompare();

                        // Render floor
                        shadowMappingShader.activate();
                        shadowMappingShader.bindModelData(floor);
                        shadowMappingShader.render(floor, camera.viewMatrix, projectionMatrix, shadowMapShader.depthViewProjectionMatrix, shadowMapShader.lightDirection, shadowMapShader.depthTexture);

                        // Render Trees
                        shadowMappingShader.bindModelData(trees[0]);
                        foreach (WorldObject tree in trees)
                        {
                            shadowMappingShader.render(tree, camera.viewMatrix, projectionMatrix, shadowMapShader.depthViewProjectionMatrix, shadowMapShader.lightDirection, shadowMapShader.depthTexture);
                        }

                        // Render Monkey
                        shadowMappingShader.bindModelData(monkey);
                        shadowMappingShader.render(monkey, camera.viewMatrix, projectionMatrix, shadowMapShader.depthViewProjectionMatrix, shadowMapShader.lightDirection, shadowMapShader.depthTexture);

                        // Render Monkey
                        shadowMappingShader.bindModelData(rmonkey);
                        shadowMappingShader.render(rmonkey, camera.viewMatrix, projectionMatrix, shadowMapShader.depthViewProjectionMatrix, shadowMapShader.lightDirection, shadowMapShader.depthTexture);

                        GL.Disable(EnableCap.CullFace);
                        // Render House
                        shadowMappingShader.bindModelData(house);
                        shadowMappingShader.render(house, camera.viewMatrix, projectionMatrix, shadowMapShader.depthViewProjectionMatrix, shadowMapShader.lightDirection, shadowMapShader.depthTexture);
                        GL.Enable(EnableCap.CullFace);
                        // Render Ground
                        shadowMappingShader.bindModelData(magicBox);
                        shadowMappingShader.render(magicBox, camera.viewMatrix, projectionMatrix, shadowMapShader.depthViewProjectionMatrix, shadowMapShader.lightDirection, shadowMapShader.depthTexture);

                        shadowMapShader.disableTextureCompare();
                    }
                    else // No Shadows use Phong Shader
                    {
                        // Render floor
                        phongShader.activate();
                        phongShader.bindModelData(floor);
                        phongShader.render(floor, camera.viewMatrix, projectionMatrix);

                        // Render Trees
                        phongShader.bindModelData(trees[0]);
                        foreach (WorldObject tree in trees)
                        {
                            phongShader.render(tree, camera.viewMatrix, projectionMatrix);
                        }

                        // Render Monkey
                        phongShader.bindModelData(monkey);
                        phongShader.render(monkey, camera.viewMatrix, projectionMatrix);

                        phongShader.bindModelData(rmonkey);
                        phongShader.render(rmonkey, camera.viewMatrix, projectionMatrix);

                        GL.Disable(EnableCap.CullFace);
                        // Render House
                        phongShader.bindModelData(house);
                        phongShader.render(house, camera.viewMatrix, projectionMatrix);
                        GL.Enable(EnableCap.CullFace);
                        // Render Ground
                        phongShader.bindModelData(magicBox);
                        phongShader.render(magicBox, camera.viewMatrix, projectionMatrix);
                    }
                }

                if (enablePostProcessing == true)
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, blurShader.fbo);
                    GL.Viewport(0, 0, screenWidth, screenHeight);
                    GL.ClearColor(Color.Black);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                    //Render Texture/ Framebuffer to screen (Post Processing) -------------
                    if (useRayMarching == true)
                    {
                        volumetricLightingShader.activate();
                        volumetricLightingShader.bindModelData(sun);
                        volumetricLightingShader.render(depthMapShader.lightDirection, camera, projectionMatrix, depthMapShader.depthTexture, shadowMapShader.depthTexture, shadowMapShader.depthViewProjectionMatrix);
                    }
                    else
                    {
                        postProcessShader.activate();
                        postProcessShader.bindModelData(sun);
                        postProcessShader.render(sun.position, depthMapShader.depthTexture, camera.viewMatrix, projectionMatrix);
                    }

                    // Blur fbos
                    // First Blur (horizontal)
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, blurShader2.fbo);
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    blurShader.activate();
                    blurShader.bindModelData(sun);
                    blurShader.uShift = new Vector2(blurRadius / (screenWidth), 0f);
                    blurShader.render(sun.position, depthMapShader.depthTexture, camera.viewMatrix, projectionMatrix);

                    // Second Blur (vertical)
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    //GL.Viewport(0, 0, 1440, 900);
                    blurShader2.activate();
                    blurShader2.bindModelData(sun);
                    blurShader2.uShift = new Vector2(0f, blurRadius / (screenHeight));
                    blurShader2.render(sun.position, depthMapShader.depthTexture, camera.viewMatrix, projectionMatrix);
                }
                else // Render Sun if no Post Processing is done
                {
                    whiteShader.activate();
                    whiteShader.bindModelData(sun);
                    whiteShader.render(sun, camera.viewMatrix, projectionMatrix);
                }
            }

            GL.Flush();
            SwapBuffers();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            renderScene();
        }

        Vector2 RayConeIntersect(Vector3 f3ConeApex, Vector3 f3ConeAxis, float fCosAngle, Vector3 f3RayStart, Vector3 f3RayDir)
        {
            f3RayStart -= f3ConeApex;
            float a = Vector3.Dot(f3RayDir, f3ConeAxis);
            float b = Vector3.Dot(f3RayDir, f3RayDir);
            float c = Vector3.Dot(f3RayStart, f3ConeAxis);
            float d = Vector3.Dot(f3RayStart, f3RayDir);
            float e = Vector3.Dot(f3RayStart, f3RayStart);
            fCosAngle *= fCosAngle;
            float A = a * a - b * fCosAngle;
            float B = 2 * (c * a - d * fCosAngle);
            float C = c * c - e * fCosAngle;
            float D = B * B - 4 * A * C;
            if (D > 0)
            {
                D = (float) Math.Sqrt(D);
                Vector2 t = (new Vector2(-B) + Math.Sign(A) * new Vector2(-D, +D)) / (2 * A);
                Vector2 temp = new Vector2(c) + a * t;
                Vector2 b2IsCorrect = new Vector2(0f, 0f);
                if(temp.X > 0.0)
                    b2IsCorrect.X = 1f;
                if (temp.Y > 0.0)
                    b2IsCorrect.Y = 1f;

                Vector2 b2Comp = new Vector2(0f);
                if (b2IsCorrect.X == 0f)
                    b2Comp.X = 1f;
                if (b2IsCorrect.Y == 0f)
                    b2Comp.Y = 1f;

                t = t * b2IsCorrect + b2Comp * new Vector2(-1);
                return t;
            }
            else
                return new Vector2(-1, -1);
        }

        void TruncateEyeRayToLightCone(Vector3 cameraPos,
							   Vector3 f3EyeVector,
                               ref Vector3 f3RayStartPos,
                               ref Vector3 f3RayEndPos,
                               ref float fTraceLength,
                               ref float fStartDistance,
                               bool bIsCamInsideCone)
        {
            // Intersect view ray with the light cone
            Vector2 f2ConeIsecs =
                RayConeIntersect(new Vector3(1, 1, 0), new Vector3(0, -1, 0), 0.5f,
                                 cameraPos, f3EyeVector);

            if (bIsCamInsideCone)
            {
                f3RayStartPos = cameraPos;
                fStartDistance = 0;
                if (f2ConeIsecs.X > 0)
                {
                    // 
                    //   '.       *     .' 
                    //     '.      \  .'   
                    //       '.     \'  x > 0
                    //         '. .' \
                    //           '    \ 
                    //         '   '   \y = -FLT_MAX 
                    //       '       ' 
                    fTraceLength = Math.Min(f2ConeIsecs.X, fTraceLength);
                }
                else if (f2ConeIsecs.Y > 0)
                {
                    // 
                    //                '.             .' 
                    //    x = -FLT_MAX  '.---*---->.' y > 0
                    //                    '.     .'
                    //                      '. .'  
                    //                        '
                    fTraceLength = Math.Min(f2ConeIsecs.Y, fTraceLength);
                }
                f3RayEndPos = cameraPos + fTraceLength * f3EyeVector;
            }
            //else if( all(f2ConeIsecs > 0) )
            else if ((f2ConeIsecs.X > 0) && (f2ConeIsecs.Y > 0))
            {
                // 
                //          '.             .' 
                //    *-------'.-------->.' y > 0
                //          x>0 '.     .'
                //                '. .'  
                //                  '
                fTraceLength = Math.Min(f2ConeIsecs.Y, fTraceLength);
                f3RayEndPos = cameraPos + fTraceLength * f3EyeVector;
                f3RayStartPos = cameraPos + f2ConeIsecs.X * f3EyeVector;
                fStartDistance = f2ConeIsecs.X;
                fTraceLength -= f2ConeIsecs.X;
            }
            else if (f2ConeIsecs.Y > 0)
            {
                // 
                //   '.       \     .'                '.         |   .' 
                //     '.      \  .'                    '.       | .'   
                //       '.     \'  y > 0                 '.     |'  y > 0
                //         '. .' \                          '. .'| 
                //           '    *                           '  |   
                //         '   '   \x = -FLT_MAX            '   '|   x = -FLT_MAX 
                //       '       '                        '      |' 
                //                                               *
                //
                f3RayEndPos = cameraPos + fTraceLength * f3EyeVector;
                f3RayStartPos = cameraPos + f2ConeIsecs.Y * f3EyeVector;
                fStartDistance = f2ConeIsecs.Y;
                fTraceLength -= f2ConeIsecs.Y;
            }
            else
            {
                fTraceLength = 0;
                fStartDistance = 0;
                f3RayStartPos = cameraPos;
                f3RayEndPos = cameraPos;
            }
            fTraceLength = Math.Max(fTraceLength, 0);
        }

        const float G_SCATTERING = 0.1f;

        // Mie scaterring approximated with Henyey-Greenstein phase function.
        float ComputeScattering(float lightDotView)
        {
            float result = 1.0f - G_SCATTERING;
            result *= result;
            result /= (4.0f * 3.14f * (float)Math.Pow(1.0f + G_SCATTERING * G_SCATTERING - (2.0f * G_SCATTERING) * lightDotView, 1.5f));
            return result;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            Vector3 ffdsj = Vector3.Normalize(new Vector3(0.7f, 0.5f, 0) - new Vector3(1, 1, 0));
            float twm = ComputeScattering((float)Math.Cos(Math.PI/6.0));//Vector3.Dot(Vector3.Normalize(new Vector3(0.7f, 0.5f, 0) - new Vector3(1, 1, 0)), new Vector3(0, -1, 0)));

            //Vector2 inter =  RayConeIntersect(new Vector3(1,1,0), new Vector3(0, -1, 0), 0.5f, new Vector3(1f, 0.5f,0f), new Vector3(1, 0, 0));

            //float fTraceLength = Math.Min(inter.Y, 9999f);
            //Vector3 f3RayEndPos = new Vector3(1f, 0.5f, 0) + fTraceLength * new Vector3(1, 0, 0);
            //Vector3 f3RayStartPos = new Vector3(1f, 0.5f, 0) + inter.X * new Vector3(1, 0, 0);
            //float fStartDistance = inter.X;
            //fTraceLength -= inter.X;

            Vector3 f3RayEndPos = new Vector3(10f, 0.5f, 0);
            Vector3 f3RayStartPos = new Vector3(0f, 0.5f, 0);
            float traceLength = 10f;
            float startDist = 0f;

            Vector3 tmp = new Vector3(0f, 0.5f, 0f) - new Vector3(1f, 1f, 0f);
            tmp.Normalize();
            float halo = Vector3.Dot(tmp, new Vector3(0f, -1f, 0f));

            TruncateEyeRayToLightCone(new Vector3(1f, 0.5f, 0f), new Vector3(1, 0, 0), ref f3RayStartPos, ref f3RayEndPos, ref traceLength, ref startDist, true);

            time += (float)e.Time;

			// Update view and projecion Matrix
			camera.update();
            projectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float)System.Math.PI/2f, (float)screenWidth / (float)screenHeight, 0.1f, 100.0f);

            // Update Objects
            monkey.position += new Vector3(0f, (float)(Math.Sin(time) * e.Time) , 0f);
            monkey.update();

			rmonkey.rotation += new Vector3(0f, 0f, (float)(e.Time));
			rmonkey.update();

            // It is unnecessary to update static Objects
            //sun.update(camera);
            //ground.update(camera);
            //floor.update(camera);
            //foreach (WorldObject tree in trees)
            //{
            //tree.update(camera);
            //}

            // Show FPS in Console(max FPS is 58)
            //Console.Out.WriteLine(1f/(float)e.Time);


            //	return E;
           float sdkafl =  Vector3.Dot(new Vector3(2,3,7).Normalized(), new Vector3(0.0f, 1.0f, 0.0f));
  

            // Update the Input Manager if the window is focused
            if (Focused)
            {
                inputManager.update((float)e.Time);
            }
        }

        protected override void OnFocusedChanged(EventArgs e)
        {
            base.OnFocusedChanged(e);

            if (Focused)
            {
                inputManager.ResetCursor();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            //screenWidth = Width;
            //screenHeight = Height;

            //postProcessShader.resize();
        }
    }
}
