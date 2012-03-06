using System;
using System.IO;
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
using Form = System.Windows.Forms.Form;
using DragEventHandler = System.Windows.Forms.DragEventHandler;
using DragEventArgs = System.Windows.Forms.DragEventArgs;

namespace gpugol
{
    public enum Automation
    {
        gol, // Game of Life
        hl, // high life
        cs, // chaos seeds
        dan, // Day and Night
        r90,
    }

    delegate void Stamper();
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        KeyboardState keysc, keysl;
        MouseState mousec, mousel;

        RenderTarget2D target;
        Texture2D previousframe;
        Texture2D fsquad;
        uint mousesize = 5;
        
        VertexBuffer vbo;
        IndexBuffer ibo;
        Effect effect;
        Automation automation = Automation.gol;
        
        // Just a copy of our window as a form for drag and drop events.
        Form mainForm;

        // a delegate we call when we have a buffer of stamp data to use.
        Stamper stamper = null;
        Color[] stampdata;
        int stampw;
        int stamph;
        int stampx;
        int stampy;

        Color[] screenbuffer;
        Point defaultSize = new Point(1280, 720);


        public string GetAutomationString()
        {
            switch (automation)
            {
                default:
                case Automation.gol:
                    return "gpugol";
                case Automation.hl:
                    return "gpuhl";
                case Automation.cs:
                    return "gpucs";
                case Automation.dan:
                    return "gpudan";
                case Automation.r90:
                    return "gpur90";
            }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            mainForm = (Form)Form.FromHandle(Window.Handle);
            mainForm.AllowDrop = true;
            mainForm.DragEnter += new DragEventHandler(mainForm_DragEnter);
            mainForm.DragDrop += new DragEventHandler(mainForm_DragDrop);
            base.Initialize();
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
            effect.Parameters["resolution"].SetValue(new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
            DrawSplatter();
        }

        void stamp()
        {
            int w = graphics.PreferredBackBufferWidth;
            int h = graphics.PreferredBackBufferHeight;
            for (int i = 0; i < stampw; ++i)
            {
                for (int j = 0; j < stamph; ++j)
                {
                    int dx = stampx + i;
                    int dy = stampy + j;
                    int idx = dy * w + dx;
                    if (idx < screenbuffer.Length && idx > 0 && dx < w && dy < h)
                    {
                        if (stampdata[j * stampw + i].R > 0)
                            screenbuffer[idx] = Color.White;
                        else
                            screenbuffer[idx] = new Color(0, 0, 0, 0);
                    }

                }
            }
            stamper = null;
        }

        void mainForm_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Array a = (Array)e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);
                if (a != null)
                {
                    string s = a.GetValue(0) as string;
                    if (s.ToUpper().Contains("PNG"))
                    {
                        FileStream fs = File.Open(s, FileMode.Open);
                        Texture2D stamp = Texture2D.FromStream(GraphicsDevice, fs as Stream);
                        fs.Close();
                        stampdata = new Color[stamp.Width * stamp.Height];
                        stamp.GetData<Color>(stampdata);
                        stampw = stamp.Width;
                        stamph = stamp.Height;
                        stampx = e.X - Window.ClientBounds.Left;
                        stampy = e.Y - Window.ClientBounds.Top;

                        stamper = this.stamp;
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }

        void mainForm_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine(e.Data.ToString());
            if (e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
            {
                e.Effect = System.Windows.Forms.DragDropEffects.Copy;
            }
            else
                e.Effect = System.Windows.Forms.DragDropEffects.None;
            // Do nothing for now. Might use this later.
        }

        void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.DiscardContents;
        }

        protected override void LoadContent()
        {
            effect = Content.Load<Effect>("gpugol");
        }

        protected override void UnloadContent()
        {
            effect.Dispose();
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
         * Tilde: clear
         * click: inject pixels (draw)
         * scroll: adjust draw size
         * 1: use game of life rules
         * 2: use high life rules
         * 3: use chaos seeds rules
         * 4: use day and night rules
         * 5: use rule 90
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
                effect.Parameters["resolution"].SetValue(new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
                resizeTextures();
                graphics.ToggleFullScreen();
            }

            if (keysc.IsKeyDown(Keys.Space) && keysl.IsKeyUp(Keys.Space))
                DrawSplatter();

            if (keysc.IsKeyDown(Keys.OemTilde) && keysl.IsKeyUp(Keys.OemTilde))
                resizeTextures();      

            if (keysc.IsKeyDown(Keys.D1) && keysl.IsKeyUp(Keys.D1))
                automation = Automation.gol;

            if (keysc.IsKeyDown(Keys.D2) && keysl.IsKeyUp(Keys.D2))
                automation = Automation.hl;

            if (keysc.IsKeyDown(Keys.D3) && keysl.IsKeyUp(Keys.D3))
                automation = Automation.cs;

            if (keysc.IsKeyDown(Keys.D4) && keysl.IsKeyUp(Keys.D4))
                automation = Automation.dan;
            
            if (keysc.IsKeyDown(Keys.D5) && keysl.IsKeyUp(Keys.D5))
                automation = Automation.r90;

            int scrolldelta = mousec.ScrollWheelValue - mousel.ScrollWheelValue;
            if (scrolldelta != 0)
            {
                // dumb hack because .Net wont let a uint be incremented by an int.
                if (mousesize < 10)
                {
                    if (scrolldelta > 0)
                        mousesize++;
                    else
                        mousesize--;
                }
                else
                {
                    if (scrolldelta > 0)
                        mousesize = (uint)(((double)mousesize) * 1.2);
                    else
                        mousesize = (uint)(((double)mousesize) * 0.8);
                }

                mousesize = Math.Max(1, mousesize);
                mousesize = Math.Min(1000, mousesize);
            }

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
            effect.CurrentTechnique = effect.Techniques["randomsplatter"];
            effect.Parameters["previousframe"].SetValue(fsquad);
            
            DrawIndexed();
            GraphicsDevice.SetRenderTarget(null);
            target.GetData<Color>(screenbuffer);
            GraphicsDevice.Textures[0] = null;
            previousframe.SetData<Color>(screenbuffer);
        }

        void DrawFrame()
        {
            effect.CurrentTechnique = effect.Techniques[GetAutomationString()];
            effect.Parameters["previousframe"].SetValue(previousframe);
            DrawIndexed();
        }

        void DrawIndexed()
        {
            effect.CurrentTechnique.Passes[0].Apply();
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
            if ((mousec.LeftButton == ButtonState.Pressed || mousec.RightButton == ButtonState.Pressed) && mainForm.Focused)
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
                        if (dist <= mousesize)
                        {
                            if (mousec.RightButton == ButtonState.Pressed)
                            {
                                screenbuffer[(int)current.Y * w + (int)current.X] = new Color(0, 0, 0, 0);
                            }
                            else
                            {
                                screenbuffer[(int)current.Y * w + (int)current.X] =
                                    new Color(1.0f, current.X / (float)w, 1.0f - current.Y / (float)h, 1);
                            }

                        }
                    }
                }
            }

            if (stamper != null)
                stamper();

            // Unbind our texture
            GraphicsDevice.Textures[0] = null;
            // set the data on our texture
            previousframe.SetData<Color>(screenbuffer);

            effect.CurrentTechnique = effect.Techniques["randomsplatter"];
            effect.Parameters["previousframe"].SetValue(previousframe);
            DrawIndexed();
        }
    }
}
