using System;
using System.Drawing;
using System.Numerics;
using SDL2;
using SkinnerBox.States.Gameplay;
using SlatedGameToolkit.Framework.AssetSystem;
using SlatedGameToolkit.Framework.Graphics;
using SlatedGameToolkit.Framework.Graphics.Render;
using SlatedGameToolkit.Framework.Graphics.Text;
using SlatedGameToolkit.Framework.Graphics.Textures;
using SlatedGameToolkit.Framework.Graphics.Window;
using SlatedGameToolkit.Framework.Input.Devices;
using SlatedGameToolkit.Framework.Loaders;
using SlatedGameToolkit.Framework.StateSystem;
using SlatedGameToolkit.Framework.StateSystem.States;

namespace SkinnerBox.States.Main
{
    public class MenuState : IState
    {
        private StateManager manager;
        WindowContext context;
        AssetManager assets;
        Camera2D camera;
        MeshBatchRenderer renderer;
        BitmapFont titleFont, boldFont;
        RectangleMesh serverUnit;
        
        public bool Activate()
        {
            Keyboard.keyboardUpdateEvent += KeyInput;
            return true;
        }

        public bool Deactivate()
        {
            Keyboard.keyboardUpdateEvent -= KeyInput;
            return true;
        }

        public void Deinitialize()
        {
            this.renderer.Dispose();
            this.assets.UnloadAll();
        }

        public string getName()
        {
            return "Main";
        }

        public void Initialize(StateManager manager)
        {
            this.manager = manager;
            this.manager.backgroundColour = Color.White;
            this.context = new WindowContext("You Are the Website", width: 640, height: 640, options: SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN); // Creates the window context.
            this.assets = new AssetManager();
            this.assets.DefaultPathModifier = (p) => "resources/" + p;
            this.assets.Loaders.TryAdd("png", TextureLoader.Load2DTexture);
            this.camera = new Camera2D(Game.WIDTH_UNITS, Game.HEIGHT_UNITS);
            this.camera.Position = new Vector2(Game.WIDTH_UNITS / 2, Game.HEIGHT_UNITS / 2);
            this.camera.MoveTo = this.camera.Position;
            this.renderer = new MeshBatchRenderer(camera);

            //Add additional states
            manager.AddState(new GamePlayState(renderer, this.assets));

            //Load assets
            this.assets.Load("serverunit.png");
            this.assets.Load("packet.png");
            this.assets.Load("warning.png");
            this.assets.Load("downloadbar.png");
            this.assets.Load("drag.png");

            //Set up title TTF
            this.titleFont = new BitmapFont("resources/BigShouldersDisplay-Regular.ttf", textureSizes: 512);
            this.titleFont.PixelHeight = 120;
            this.titleFont.PixelsPerUnitHeight = 80;
            this.titleFont.PixelsPerUnitWidth = 80;
            this.titleFont.PrepareCharacterGroup("You Are the Website.".ToCharArray());
            this.titleFont.PixelHeight = 40;
            this.titleFont.PrepareCharacterGroup("By: Reslate".ToCharArray());

            //Set up bold TTF
            boldFont = new BitmapFont("resources/BigShouldersDisplay-Black.ttf", textureSizes: 512);
            boldFont.PixelHeight = 60;
            boldFont.PixelsPerUnitWidth = 80;
            boldFont.PixelsPerUnitHeight = 80;
            boldFont.PrepareCharacterGroup("Press any key to start...".ToCharArray());

            //Set up icon
            Texture serverUnitTex = (Texture)assets["serverunit.png"];
            serverUnitTex.SetNearestFilter(true, true);
            this.serverUnit = new RectangleMesh(new RectangleF(Game.WIDTH_UNITS/2 - 0.75f, Game.HEIGHT_UNITS * 0.75f - 0.75f, 1.5f, 1.5f), serverUnitTex, Color.White);
            
            this.context.Shown = true;
        }

        public void Render(double delta)
        {
            renderer.Begin(Matrix4x4.Identity, delta);
            this.titleFont.PixelHeight = 120;
            this.titleFont.WriteLine(renderer, 0.02f, 0.02f, "You Are the Website.", Color.Black);

            this.titleFont.PixelHeight = 40;
            this.titleFont.WriteLine(renderer, 0, 1.2f, "By: Reslate", Color.Gray);

            renderer.Draw(serverUnit);
            
            this.boldFont.WriteLine(renderer, 1.15f, Game.HEIGHT_UNITS / 2, "Press any key to start...", Color.Black);
            renderer.End();
        }

        public void Update(double timeStep)
        {
        }

        public void KeyInput(SDL.SDL_Keycode keys, bool pressed)
        {
            if (pressed)
            {
                manager.ChangeState("GamePlayState");
            }
        }
    }
}