using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;

namespace gpugol
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        KeyboardState keysc, keysl;

        RenderTarget2D target;
        Texture2D previousframe;
        Texture2D fsquad;
        
        VertexBuffer vbo;
        IndexBuffer ibo;
        Effect gpugol;

        Color[] screenbuffer;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();

            VertexPositionTexture[] vpt;
            int[] indices;

            vpt = new VertexPositionTexture[4];
            vpt[0] = new VertexPositionTexture(new Vector3(-1.0f, -1.0f, 0.0f), new Vector2(0.0f, 1.0f));
            vpt[1] = new VertexPositionTexture(new Vector3(+1.0f, -1.0f, 0.0f), new Vector2(1.0f, 1.0f));
            vpt[2] = new VertexPositionTexture(new Vector3(+1.0f, +1.0f, 0.0f), new Vector2(1.0f, 0.0f));
            vpt[3] = new VertexPositionTexture(new Vector3(-1.0f, +1.0f, 0.0f), new Vector2(0.0f, 0.0f));
            indices = new int[]{
                2, 1, 0,
                0, 3, 2,
            };

            vbo = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), vpt.Length, BufferUsage.WriteOnly);
            ibo = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
            vbo.SetData<VertexPositionTexture>(vpt);
            ibo.SetData<int>(indices);

            SamplerState sam = new SamplerState();
            sam.AddressU = TextureAddressMode.Wrap;
            sam.AddressV = TextureAddressMode.Wrap;
            sam.AddressW = TextureAddressMode.Wrap;
            sam.Filter = TextureFilter.Point;

            GraphicsDevice.SamplerStates[0] = sam;
            graphics.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(graphics_PreparingDeviceSettings);
            screenbuffer = new Color[GraphicsDevice.Viewport.Width * GraphicsDevice.Viewport.Height];

            target = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            
            
            previousframe = new Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            previousframe.SetData<Color>(screenbuffer);
            DrawSplatter();
        }

        void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents;
        }

        protected override void LoadContent()
        {
            gpugol = Content.Load<Effect>("gpugol");
        }

        protected override void UnloadContent()
        {
            gpugol.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            keysl = keysc; keysc = Keyboard.GetState();
            if (keysc.IsKeyUp(Keys.Escape) && keysl.IsKeyDown(Keys.Escape))
                this.Exit();

            base.Update(gameTime);
        }

        void DrawSplatter()
        {
            fsquad = new Texture2D(GraphicsDevice, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            Random r = new Random();
            for (int i = 0; i < screenbuffer.Length; ++i)
            {
                if ((r.Next() & 1) == 0)
                    screenbuffer[i] = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                else
                    screenbuffer[i] = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }

            fsquad.SetData<Color>(screenbuffer);
            GraphicsDevice.SetRenderTarget(target);
            gpugol.CurrentTechnique = gpugol.Techniques["randomsplatter"];
            gpugol.Parameters["previousframe"].SetValue(fsquad);
            
            DrawIndexed();
            GraphicsDevice.SetRenderTarget(null);
            target.GetData<Color>(screenbuffer);
            GraphicsDevice.Textures[0] = null;

            //fsquad.SetData<Color>(screenbuffer);
            previousframe.SetData<Color>(screenbuffer);
        }

        void DrawFrame()
        {
            gpugol.CurrentTechnique = gpugol.Techniques["gpugol"];
            //gpugol.CurrentTechnique = gpugol.Techniques["randomsplatter"];
            gpugol.Parameters["previousframe"].SetValue(previousframe);
            DrawIndexed();
        }

        void DrawIndexed()
        {
            gpugol.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.SetVertexBuffer(vbo);
            GraphicsDevice.Indices = ibo;
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 6, 0, 2);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clear
            GraphicsDevice.Clear(Color.CornflowerBlue);
            // Set our target
            GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents;
            GraphicsDevice.SetRenderTarget(target);
            // Draw to our target
            DrawFrame();
            // Unbind our target
            GraphicsDevice.SetRenderTarget(null);
            // Get the data we just drew to it
            target.GetData<Color>(screenbuffer);
            // Unbind our texture
            GraphicsDevice.Textures[0] = null;
            // set the data on our texture
            previousframe.SetData<Color>(screenbuffer);
            
            //DrawFrame();

            gpugol.CurrentTechnique = gpugol.Techniques["randomsplatter"];
            gpugol.Parameters["previousframe"].SetValue(previousframe);
            DrawIndexed();
        }
    }
}
