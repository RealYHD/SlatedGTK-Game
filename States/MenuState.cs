using System;
using System.Drawing;
using System.Numerics;
using SDL2;
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

namespace WebsiteSim.States
{
    public class MenuState : IState
    {
        private StateManager manager;
        WindowContext context;
        AssetManager assets;
        Camera2D camera;
        MeshBatchRenderer renderer;
        BitmapFont titleFont;
        RectangleMesh serverUnit;
        private BitmapFont genericFont;

        public bool Activate()
        {
            Keyboard.keyboardUpdateEvent += KeyInput;
            this.titleFont.PixelHeight = 120;
            this.titleFont.PrepareCharacterGroup("Website Simulator".ToCharArray());
            this.titleFont.PixelHeight = 40;
            this.titleFont.PrepareCharacterGroup("How it feels to be on the other end...".ToCharArray());
            this.titleFont.PrepareCharacterGroup("Press space to start...".ToCharArray());
            return true;
        }

        public bool Deactivate()
        {
            Keyboard.keyboardUpdateEvent -= KeyInput;
            return true;
        }

        public void Deinitialize()
        {
            titleFont.Dispose();
            genericFont.Dispose();
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
            this.manager.backgroundColour = Color.LightGray;
            this.context = new WindowContext("Website Simulator", width: 640, height: 640, options: SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN);
            this.assets = new AssetManager();
            this.assets.DefaultPathModifier = (p) => "resources/" + p;
            this.assets.Loaders.TryAdd("png", TextureLoader.Load2DTexture);
            this.camera = new Camera2D(Game.WIDTH_UNITS, Game.HEIGHT_UNITS);
            this.camera.Position = new Vector2(Game.WIDTH_UNITS / 2, Game.HEIGHT_UNITS / 2);
            this.camera.MoveTo = this.camera.Position;
            this.renderer = new MeshBatchRenderer(camera);

            //Set up title TTF
            this.titleFont = new BitmapFont("resources/BigShouldersDisplay-Regular.ttf", textureSizes: 512);
            this.titleFont.PixelsPerUnitHeight = 80;
            this.titleFont.PixelsPerUnitWidth = 80;
            genericFont = new BitmapFont("resources/BigShouldersDisplay-Light.ttf");
            genericFont.PixelsPerUnitHeight = 80;
            genericFont.PixelsPerUnitWidth = 80;
            //Add additional states
            GameOverState gameOverState = new GameOverState(genericFont, titleFont, renderer, assets);
            manager.AddState(gameOverState);
            manager.AddState(new GamePlayState(renderer, this.assets, genericFont, gameOverState));
            manager.AddState(new TutorialState(renderer, assets));

            //Load assets
            assets.Load("serverunit.png");
            assets.Load("packet.png");
            assets.Load("warning.png");
            assets.Load("downloadbar.png");
            assets.Load("drag.png");
            assets.Load("usage.png");
            assets.Load("health.png");
            assets.Load("beam.png");
            assets.Load("ram.png");

            Texture downloadBarTex = (Texture)assets["downloadbar.png"];
            downloadBarTex.SetNearestFilter(true, true);

            //Set up icon
            Texture serverUnitTex = (Texture)assets["serverunit.png"];
            serverUnitTex.SetNearestFilter(true, true);
            this.serverUnit = new RectangleMesh(new RectangleF(Game.WIDTH_UNITS/2 - 0.75f, Game.HEIGHT_UNITS * 0.5f - 0.75f, 1.5f, 1.5f), serverUnitTex, Color.White);
            
            this.context.Shown = true;
        }

        public void Render(double delta)
        {
            renderer.Begin(Matrix4x4.Identity, delta);
            this.titleFont.PixelHeight = 120;
            this.titleFont.WriteLine(renderer, 0.02f, 0.02f, "Website Simulator", Color.Black);

            this.titleFont.PixelHeight = 40;
            this.titleFont.WriteLine(renderer, 0, 1.2f, "How it feels to be on the other end...", Color.Gray);

            renderer.Draw(serverUnit);
            
            this.titleFont.WriteLine(renderer, Game.WIDTH_UNITS / 2f - 1.25f, Game.HEIGHT_UNITS / 2f + 1f, "Press space to start...", Color.Black);
            renderer.End();
        }

        public void Update(double timeStep)
        {
        }

        public void KeyInput(SDL.SDL_Keycode keys, bool pressed)
        {
            if (!pressed && keys == SDL.SDL_Keycode.SDLK_SPACE)
            {
                manager.ChangeState("Tutorial");
            }
        }
    }
}