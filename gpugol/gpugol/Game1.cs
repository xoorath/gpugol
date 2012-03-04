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
        MouseState mousec, mousel;

        RenderTarget2D target;
        Texture2D previousframe;
        Texture2D fsquad;
        float coloroffset;
        uint mousesize = 5;
        
        VertexBuffer vbo;
        IndexBuffer ibo;
        Effect gpugol;

        Color[] screenbuffer;
        Point defaultSize = new Point(1280, 720);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
            coloroffset = 0.0f;

            IsMouseVisible = true;

            graphics.PreferredBackBufferWidth = defaultSize.X;
            graphics.PreferredBackBufferHeight = defaultSize.Y;
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
            gpugol.Parameters["resolution"].SetValue(new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
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

        void resizeTextures()
        {
            int w = graphics.PreferredBackBufferWidth;
            int h = graphics.PreferredBackBufferHeight;
            GraphicsDevice.Textures[0] = null;
            GraphicsDevice.SetRenderTarget(null);
            previousframe = new Texture2D(GraphicsDevice, w, h);
            fsquad = new Texture2D(GraphicsDevice, w, h);
            target = new RenderTarget2D(GraphicsDevice, w, h);
            screenbuffer = new Color[w * h];
        }

        /** Controls:
         * Alt + Enter : Toggle full screen
         * Escape : Quit
         * Space : Generate noise
         * click: inject pixels (draw)
         * scroll: adjust draw size
         */
        protected override void Update(GameTime gameTime)
        {
            keysl = keysc; keysc = Keyboard.GetState();
            mousel = mousec; mousec = Mouse.GetState();

            if (keysc.IsKeyUp(Keys.Escape) && keysl.IsKeyDown(Keys.Escape))
                this.Exit();

            if ((keysc.IsKeyDown(Keys.LeftAlt) || keysc.IsKeyDown(Keys.RightAlt)) && (keysc.IsKeyDown(Keys.Enter) && keysl.IsKeyUp(Keys.Enter)))
            {
                if (graphics.IsFullScreen)
                {
                    graphics.PreferredBackBufferWidth = defaultSize.X;
                    graphics.PreferredBackBufferHeight = defaultSize.Y;
                }
                else
                {
                    graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                    graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                }
                gpugol.Parameters["resolution"].SetValue(new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
                gpugol.Parameters["coloroffset"].SetValue(coloroffset);
                resizeTextures();
                graphics.ToggleFullScreen();
            }

            if (keysc.IsKeyDown(Keys.Space) && keysl.IsKeyUp(Keys.Space))
                DrawSplatter();

            int scrolldelta = mousec.ScrollWheelValue - mousel.ScrollWheelValue;
            if (scrolldelta != 0)
            {
                // dumb hack because .Net wont let a uint be incremented by an int.
                if (scrolldelta > 0)
                    mousesize ++;
                else
                    mousesize --;

                mousesize = Math.Max(1, mousesize);
                mousesize = Math.Min(1000, mousesize);
            }

            coloroffset += (float)gameTime.ElapsedGameTime.TotalSeconds;
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
            previousframe.SetData<Color>(screenbuffer);
        }

        void DrawFrame()
        {
            gpugol.CurrentTechnique = gpugol.Techniques["gpugol"];
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

            // Inject pixels at the mouse position if the mouse button is down.
            if (mousec.LeftButton == ButtonState.Pressed)
            {
                int x = mousec.X;
                int y = mousec.Y;
                int w = graphics.PreferredBackBufferWidth;
                int h = graphics.PreferredBackBufferHeight;
                Vector2 center = new Vector2(x, y);

                screenbuffer[(Math.Min(Math.Max(0, y), h - 1) * w) + Math.Min(Math.Max(0, x), w - 1)] = Color.White;
                for (int i = x - (int)mousesize; i < mousesize + x; ++i)
                {
                    for (int j = y - (int)mousesize; j < mousesize + y; ++j)
                    {
                        Vector2 current = new Vector2(Math.Min(Math.Max(0, i), w-1), Math.Min(Math.Max(0, j), h-1));
                        float dist = Vector2.Distance(current, center); 
                        if(((int)dist & 1) == 0)
                            continue;
                        if (dist <= mousesize)
                        {
                            screenbuffer[(int)current.Y * w + (int)current.X] =
                                new Color(1.0f, current.X / (float)w, 1.0f - current.Y / (float)h, 1);
                        }
                    }
                }
            }

            // Unbind our texture
            GraphicsDevice.Textures[0] = null;
            // set the data on our texture
            previousframe.SetData<Color>(screenbuffer);

            gpugol.CurrentTechnique = gpugol.Techniques["randomsplatter"];
            gpugol.Parameters["previousframe"].SetValue(previousframe);
            DrawIndexed();
        }
    }
}
