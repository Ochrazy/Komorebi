using OpenTK;
using OpenTK.Input;

namespace Komorebi
{
    class InputManager
    {
        Vector2 lastMousePos = new Vector2();
        bool moveForward = false, moveBack = false, moveRight = false, moveLeft = false, moveUp = false, moveDown = false;
        bool slowCameraMovement = false;
        Window windowCopy;
        Camera cameraCopy;

        public InputManager(Window window, Camera camera)
        {
            windowCopy = window;
            cameraCopy = camera;

            lastMousePos = new Vector2(OpenTK.Input.Mouse.GetCursorState().X, OpenTK.Input.Mouse.GetCursorState().Y);
            windowCopy.KeyDown += HandleKeyDown;
            windowCopy.KeyUp += HandleKeyUp;
        }

        public void update(float elapsedTime)
        {
            // Reset mouse position and add Rotation
            // Only rotate the camera if the Left Mouse Button is pressed
            if (OpenTK.Input.Mouse.GetState().IsButtonDown(MouseButton.Left))
            {
                Vector2 delta = lastMousePos - new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);
                lastMousePos += delta;

                cameraCopy.AddRotation(delta.X, delta.Y);
            }
            ResetCursor();

            // Update camera
            if(slowCameraMovement) cameraCopy.MoveSpeed = 1f * elapsedTime;
            else cameraCopy.MoveSpeed = 5f * elapsedTime;

            if (moveForward) cameraCopy.Move(0f, 1f, 0f);
            if(moveRight) cameraCopy.Move(-1f, 0f, 0f);
            if(moveBack) cameraCopy.Move(0f, -1f, 0f);
            if(moveLeft) cameraCopy.Move(1f, 0f, 0f);
            if(moveUp) cameraCopy.Move(0f, 0f, 1f);
            if(moveDown) cameraCopy.Move(0f, 0f, -1f);
        }

        // Moves the mouse cursor to the center of the screen
        public void ResetCursor()
        {
            OpenTK.Input.Mouse.SetPosition(windowCopy.Bounds.Left + windowCopy.Bounds.Width / 2, windowCopy.Bounds.Top + windowCopy.Bounds.Height / 2);
            lastMousePos = new Vector2(OpenTK.Input.Mouse.GetState().X, OpenTK.Input.Mouse.GetState().Y);
        }

        // Key Down
        void HandleKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape) windowCopy.Exit();

            switch (e.Key)
            {
                case Key.W:
                    moveForward = true;
                    break;
                case Key.A:
                    moveRight = true;
                    break;
                case Key.S:
                    moveBack = true;
                    break;
                case Key.D:
                    moveLeft = true;
                    break;
                case Key.Q:
                    moveUp = true;
                    break;
                case Key.E:
                    moveDown = true;
                    break;
            }
        }

        // Key Up
        void HandleKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.ShiftLeft) slowCameraMovement = !slowCameraMovement;

            switch (e.Key)
            {
                case Key.W:
                    moveForward = false;
                    break;
                case Key.A:
                    moveRight = false;
                    break;
                case Key.S:
                    moveBack = false;
                    break;
                case Key.D:
                    moveLeft = false;
                    break;
                case Key.Q:
                    moveUp = false;
                    break;
                case Key.E:
                    moveDown = false;
                    break;
                    // Switch settings
                case Key.P: // Switch Post Processing
                    windowCopy.enablePostProcessing = !windowCopy.enablePostProcessing;
                    if (windowCopy.onlyShowPostProcessingTexture || windowCopy.onlyShowDepthMap || (windowCopy.postProcessShader.noPostProcessing == 1.0f))
                    {
                        windowCopy.enablePostProcessing = true;
                        windowCopy.enableShadows = true;
                    }
                    windowCopy.onlyShowPostProcessingTexture = false;
                    windowCopy.onlyShowDepthMap = false;
                    windowCopy.postProcessShader.noPostProcessing = 0.0f;
                    break;
                case Key.L: // Switch Shadows
                    windowCopy.enableShadows = !windowCopy.enableShadows;
                    if (windowCopy.onlyShowPostProcessingTexture || windowCopy.onlyShowDepthMap || (windowCopy.postProcessShader.noPostProcessing == 1.0f))
                    {
                        windowCopy.enableShadows = true;
                        windowCopy.enablePostProcessing = true;
                    }
                    windowCopy.onlyShowPostProcessingTexture = false;
                    windowCopy.onlyShowDepthMap = false;
                    windowCopy.postProcessShader.noPostProcessing = 0.0f;
                    break;
                case Key.M: // Switch Shadow Sampling 
                    if (windowCopy.shadowMappingShader.useStratifiedPoissonSampling == 0.0)
                        windowCopy.shadowMappingShader.useStratifiedPoissonSampling = 1.0f;
                    else windowCopy.shadowMappingShader.useStratifiedPoissonSampling = 0.0f;
                    break;
                case Key.O: // Switch PostProcessing Texture with Radial Blur
                    windowCopy.onlyShowPostProcessingTexture = !windowCopy.onlyShowPostProcessingTexture;
                    if (windowCopy.postProcessShader.noPostProcessing == 1.0)
                        windowCopy.onlyShowPostProcessingTexture = true;
                    windowCopy.enablePostProcessing = true; // No PostProcessing no PostProcessingTexture
                    windowCopy.enableShadows = true;
                    windowCopy.onlyShowDepthMap = false;
                    windowCopy.postProcessShader.noPostProcessing = 0.0f;
                    break;
                case Key.I: // Switch PostProcessing Texture 
                    if (windowCopy.postProcessShader.noPostProcessing == 0.0)
                    {
                        windowCopy.postProcessShader.noPostProcessing = 1.0f;
                        windowCopy.enablePostProcessing = true; // No PostProcessing no PostProcessingTexture
                        windowCopy.onlyShowDepthMap = false;
                        windowCopy.onlyShowPostProcessingTexture = true;
                        windowCopy.enableShadows = true;
                    }
                    else
                    {
                        windowCopy.postProcessShader.noPostProcessing = 0.0f;
                        windowCopy.onlyShowPostProcessingTexture = false;
                    }
                    break;
                case Key.K: // Switch SadowMap
                    windowCopy.onlyShowDepthMap = !windowCopy.onlyShowDepthMap;
                    windowCopy.enableShadows = true; // No Shadows no DepthMap
                    windowCopy.postProcessShader.noPostProcessing = 0.0f;
                    windowCopy.onlyShowPostProcessingTexture = false;
                    windowCopy.enablePostProcessing = false;
                    break;
                case Key.T: // Switch Textures on/off
                    windowCopy.floor.mesh.isTextured = !windowCopy.floor.mesh.isTextured;
                    if(windowCopy.floor.diffuse.X == 0f)
                        windowCopy.floor.diffuse = new Vector3(1, 1, 1);
                    else windowCopy.floor.diffuse = new Vector3(0, 0, 0);
                    // All trees have the same mesh
                    windowCopy.trees[0].mesh.isTextured = !windowCopy.trees[0].mesh.isTextured;
                    break;
                case Key.B: // Switch Textures on/off
                    if (windowCopy.blurRadius == 0f) windowCopy.blurRadius = 1.0f;
                    else windowCopy.blurRadius = 0f;
                    break;
                case Key.V: // Switch Technique (RayMarching/RadialBlur)
                    windowCopy.useRayMarching =  !windowCopy.useRayMarching;
                    break;
                case Key.C:
                    windowCopy.volumetricLightingShader.enumScatteringTechnique++;
                    if (windowCopy.volumetricLightingShader.enumScatteringTechnique > 3.0f)
                        windowCopy.volumetricLightingShader.enumScatteringTechnique = 0.0f;
                    break;
			case Key.N:
				windowCopy.sun.position = new Vector3 (20f, 30f, -70f);
				windowCopy.sun.update();
				break;
			case Key.J:
				windowCopy.sun.position = new Vector3 (20f, 30f, 35f);
				windowCopy.sun.update();
				break;
            }
        }
    }
}
