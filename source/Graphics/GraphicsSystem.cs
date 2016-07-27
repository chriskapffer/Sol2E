using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sol2E.Common;
using Sol2E.Core;
using Sol2E.Graphics.UI;
using Sol2E.Utils;

using S2EModelMesh = Sol2E.Common.ModelMesh;
using S2ESimpleMesh = Sol2E.Common.SimpleMesh;
using XnaModelMesh = Microsoft.Xna.Framework.Graphics.ModelMesh;
using XnaDirectionalLight = Microsoft.Xna.Framework.Graphics.DirectionalLight;

namespace Sol2E.Graphics
{
    /// <summary>
    /// The audio system.
    /// For general explanatino on what a system does, see the documentation of IDomainSystem.
    /// Components used by this system are Camera, Transform, Mesh, Appearance, DirectionalLight,
    /// Ambience and UserInterface.
    /// 
    /// The system renders everything which can possibly be drawn and updates all user interfaces
    /// in respect to input state.
    /// </summary>
    public class GraphicsSystem : AbstractGraphicsSystem
    {
        #region Properties and Fields

        public GraphicsDevice GraphicsDevice { private get; set; }

        protected SpriteBatch SpriteBatch;
        protected BasicEffect BasicEffect;

        protected Color _clearColor;
        // rasterizer state for drawing shapes in wire fram mode
        protected readonly RasterizerState RasterizerStateWireFrame;
        // rasterizer state to enable clipping
        protected readonly RasterizerState RasterizerScissorTest;
        // collection of three directional light used by _basicEffect
        protected readonly IDictionary<XnaDirectionalLight, int> DirectionalLights;

        // collection of textures, each associated with a name 
        protected readonly IDictionary<string, SharedTexture> Textures;
        // collection of fonts, each associated with a name
        protected readonly IDictionary<string, SharedFont> Fonts;
        // collection of user interface root elements, each associated with an entity id
        protected readonly IDictionary<int, UIElement> UserInterfaces;
        // collection of mesh renderers, each associated with an entity id
        protected readonly IDictionary<int, MeshRenderer> MeshRenderers;

        private bool _globalTextureEnabled = true;
        public bool TextureEnabled
        {
            get { return _globalTextureEnabled; }
            set { _globalTextureEnabled = value; }
        }

        private bool _globalRenderWireframe;
        public bool RenderWireframe
        {
            get { return _globalRenderWireframe; }
            set { _globalRenderWireframe = value; }
        }

        private bool _lightingEnabled = true;
        public bool LightingEnabled
        {
            get
            {
                return _lightingEnabled;
            }
            set
            {
                _lightingEnabled = value;
                if (BasicEffect != null)
                    BasicEffect.LightingEnabled = value;
            }
        }

        private bool _preferPerPixelLighting;
        public bool PreferPerPixelLighting
        {
            get
            {
                return _preferPerPixelLighting;
            }
            set
            {
                _preferPerPixelLighting = value;
                if (BasicEffect != null)
                    BasicEffect.PreferPerPixelLighting = value;
            }
        }

        #endregion

        public GraphicsSystem()
            : base("Graphics")
        {
            Textures = new ConcurrentDictionary<string, SharedTexture>();
            Fonts = new ConcurrentDictionary<string, SharedFont>();
            UserInterfaces = new ConcurrentDictionary<int, UIElement>();
            MeshRenderers = new ConcurrentDictionary<int, MeshRenderer>();

            RasterizerStateWireFrame = new RasterizerState
            {
                FillMode = FillMode.WireFrame,
                CullMode = CullMode.None,
            };

            RasterizerScissorTest = new RasterizerState
            {
                ScissorTestEnable = true,
            };

            _clearColor = Color.CornflowerBlue;
            DirectionalLights = new Dictionary<XnaDirectionalLight, int>();
            
            TextureEnabled = true;
        }

        #region Implementation of AbstractGraphicsSystem

        /// <summary>
        /// Initializes internal resources, which might be not available at creation.
        /// </summary>
        public override void Initialize()
        {
            // set up sprite batch and basic effect
            // (can't do it in constructor, because graphics device, might be invalid)
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            BasicEffect = new BasicEffect(GraphicsDevice)
            {
                LightingEnabled = LightingEnabled,
                PreferPerPixelLighting = PreferPerPixelLighting
            };

            // to iterate through the directional lights of basic effect, we put them in a list
            DirectionalLights.Add(BasicEffect.DirectionalLight0, IDPool.InvalidID);
            DirectionalLights.Add(BasicEffect.DirectionalLight1, IDPool.InvalidID);
            DirectionalLights.Add(BasicEffect.DirectionalLight2, IDPool.InvalidID);

            // register to changed events of these components, to handle them appropriately
            ComponentChangedEvent<Camera>.ComponentChanged += CameraChanged;
            ComponentChangedEvent<Ambience>.ComponentChanged += AmbienceChanged;
            ComponentChangedEvent<Appearance>.ComponentChanged += AppearanceChanged;
            ComponentChangedEvent<UserInterface>.ComponentChanged += UserInterfaceChanged;
            ComponentChangedEvent<DirectionalLight>.ComponentChanged += DirectionalLightChanged;
        }

        /// <summary>
        /// Updates active user interfaces.
        /// </summary>
        /// <param name="deltaTime">elapsed game time in total seconds</param>
        protected override void Update(float deltaTime)
        {
            foreach (UIElement element in UserInterfaces.Values)
                element.Update();
        }

        /// <summary>
        /// Renders all visible content.
        /// </summary>
        /// <param name="deltaTime">elapsed game time in total seconds</param>
        protected override void Draw(float deltaTime)
        {
            GraphicsDevice.Clear(_clearColor);
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // set view matrix from camera position
            Transform camTransform = null;
            if (Camera.ActiveCamera != null)
                camTransform = Camera.ActiveCamera.GetHostingEntity().Get<Transform>();

            if (camTransform == null)
                camTransform = Transform.Default;

            BasicEffect.View = camTransform.View;

            var frustum = new BoundingFrustum(BasicEffect.View * BasicEffect.Projection);

            // get 3D meshes and models in camera frustum
            IEnumerable<Entity> visibleEntities = MeshRenderers
                .Select(kv => Entity.GetInstance(kv.Key))
                .Where(e => EntityIsInFrustum(frustum, e));

            var alphaBlendedEntities = new SortedList<float, Entity>();

            // draw opaque objects and sort objects with transparency by depth
            foreach (Entity entity in visibleEntities)
            {
                var appearance = entity.Get<Appearance>();
                if (appearance == null || appearance.DiffuseColor.A == 255)
                {
                    DrawEntity(entity);
                    continue;
                }

                var transform = entity.Get<Transform>() ?? Transform.Default;

                float distance = Vector3.DistanceSquared(transform.Position, camTransform.Position);
                alphaBlendedEntities.Add(distance, entity);
            }

            // draw objects with transparency
            foreach (Entity entity in alphaBlendedEntities.Values.Reverse())
                DrawEntity(entity);

            // draw 2D stuff
            GraphicsDevice.RasterizerState = RasterizerScissorTest;

            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, GraphicsDevice.DepthStencilState, GraphicsDevice.RasterizerState);
            {
                foreach (UIElement element in UserInterfaces.Values)
                    DrawUIElement(element);
            }
            SpriteBatch.End();
        }

        /// <summary>
        /// Cleans up internal resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources</param>
        protected override void Dispose(bool disposing)
        {
            // unregister from changed events of these components
            ComponentChangedEvent<Camera>.ComponentChanged -= CameraChanged;
            ComponentChangedEvent<Ambience>.ComponentChanged -= AmbienceChanged;
            ComponentChangedEvent<Appearance>.ComponentChanged -= AppearanceChanged;
            ComponentChangedEvent<UserInterface>.ComponentChanged -= UserInterfaceChanged;
            ComponentChangedEvent<DirectionalLight>.ComponentChanged -= DirectionalLightChanged;

            // dispose all vertex and index buffers
            foreach (MeshRenderer renderer in MeshRenderers.Values.Where(renderer => !renderer.IsModel))
            {
                renderer.IndexBuffer.Dispose();
                renderer.VertexBuffer.Dispose();
            }

            // dispose all textures
            foreach (SharedTexture shared in Textures.Values)
                shared.Texture.Dispose();

            SpriteBatch.Dispose();
            BasicEffect.Dispose();
        }

        #endregion

        #region Event Handling for Scene and Entity Changes

        /// <summary>
        /// This method should register to SceneChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected scene</param>
        /// <param name="eventType">whether an entity was added or removed</param>
        /// <param name="entity">affected entity</param>
        public override void SceneChanged(Scene sender, SceneEventType eventType, Entity entity)
        {
            if (eventType == SceneEventType.EntityAdded)
            {
                AddEntityResources(entity);
            }
            else
            {
                RemoveEntityResources(entity);
            }
        }

        /// <summary>
        /// This method should register to EntityChangedEvents and handle them appropriately.
        /// </summary>
        /// <param name="sender">affected entity</param>
        /// <param name="eventType">whether a component was added, removed or deserialized</param>
        /// <param name="component">affected component</param>
        public override void EntityChanged(Entity sender, EntityEventType eventType, Component component)
        {
            switch (eventType)
            {
                case EntityEventType.ComponentAdded:
                    throw new NotImplementedException();
                case EntityEventType.ComponentRemoved:
                    throw new NotImplementedException();
                // if the system uses this entity, determine the type
                // of the component and tell that all its properties have been updated
                case EntityEventType.ComponentDeserialized:
                    if (component is Camera)
                        CameraChanged(component as Camera, "All", null);
                    else if (component is Ambience)
                        AmbienceChanged(component as Ambience, "All", null);
                    else if (component is DirectionalLight)
                        DirectionalLightChanged(component as DirectionalLight, "All", null);
                    else if (component is Appearance)
                        AppearanceChanged(component as Appearance, "All", null);
                    else if (component is UserInterface)
                        UserInterfaceChanged(component as UserInterface, "All", null);
                break;
            }
        }

        #endregion

        #region Event Handling for Component Changes

        /// <summary>
        /// Handles changes of a Camera component.
        /// </summary>
        /// <param name="sender">component of type Camera</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void CameraChanged(Camera sender, string propertyName, object oldValue)
        {
            if(sender.IsActive)
                BasicEffect.Projection = sender.Projection;
        }

        /// <summary>
        /// Handles changes of a Ambience component.
        /// </summary>
        /// <param name="sender">component of type Ambience</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void AmbienceChanged(Ambience sender, string propertyName, object oldValue)
        {
            BasicEffect.AmbientLightColor = sender.AmbientLightColor.ToVector3();
            BasicEffect.FogColor = sender.FogColor.ToVector3();
            BasicEffect.FogEnabled = sender.FogEnabled;
            BasicEffect.FogStart = sender.FogStart;
            BasicEffect.FogEnd = sender.FogEnd;

            GraphicsDevice.Clear(sender.ClearColor);
        }

        /// <summary>
        /// Handles changes of a DirectionalLight component.
        /// </summary>
        /// <param name="sender">component of type DirectionalLight</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void DirectionalLightChanged(DirectionalLight sender, string propertyName, object oldValue)
        {
            TryToSetupDirectionalLight(sender, sender.GetHostingEntity().Id);
        }

        /// <summary>
        /// Handles changes of a Appearance component.
        /// </summary>
        /// <param name="sender">component of type Appearance</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void AppearanceChanged(Appearance sender, string propertyName, object oldValue)
        {
            switch (propertyName)
            {
                case "TextureName":
                    RemoveTextureByName(oldValue as string);
                    AddTextureByName(sender.TextureName);
                    break;
                case "All":
                    // add texture but do not increase counter if existent
                    AddTextureByName(sender.TextureName, false); 
                    break;
            }
        }

        /// <summary>
        /// Handles changes of a UserInterface component.
        /// </summary>
        /// <param name="sender">component of type UserInterface</param>
        /// <param name="propertyName">name of property that changed</param>
        /// <param name="oldValue">old value of that property or null, if not specified</param>
        protected virtual void UserInterfaceChanged(UserInterface sender, string propertyName, object oldValue)
        {
            int entityId = sender.GetHostingEntity().Id;
            if (!UserInterfaces.ContainsKey(entityId))
                return;

            switch (propertyName)
            {
                case "RootElement":
                    RemoveUserInterfaceResources(oldValue as UIElement);
                    AddUserInterfaceResources(sender.RootElement);
                    break;
                case "All":
                    // add resources but do not increase counter if existent
                    AddUserInterfaceResources(sender.RootElement, false);
                    // update reference to local copy
                    UserInterfaces[entityId] = sender.RootElement;
                    break;
            }
        }

        #endregion

        #region Helper Methods

        #region Draw Helpers

        /// <summary>
        /// Determines if the bounding box of given entity is within the view frustum.
        /// </summary>
        /// <param name="frustum">current camera frustum</param>
        /// <param name="entity">entity to draw</param>
        /// <returns>true if bounding box is within frustum</returns>
        protected static bool EntityIsInFrustum(BoundingFrustum frustum, Entity entity)
        {
            var mesh = entity.Get<S2ESimpleMesh>() ?? (Mesh)entity.Get<S2EModelMesh>();
            var transform = entity.Get<Transform>() ?? Transform.Default;

            var corners = new Vector3[8];
            mesh.BoundingBox.GetCorners(corners);

            Matrix worldTransform = transform.World;
            // transform bounding box from local to world space
            Vector3.Transform(corners, ref worldTransform, corners);
            // perform test
            return frustum.Intersects(BoundingBox.CreateFromPoints(corners));
        }

        /// <summary>
        /// Draws either a simple mesh or a model from given entity.
        /// </summary>
        /// <param name="entity">instance of entity, which contains drawing information</param>
        protected virtual void DrawEntity(Entity entity)
        {
            var appearance = entity.Get<Appearance>() ?? Appearance.Default;

            if (!appearance.Visible)
                return;

            SetupCommonEffectsFromAppearance(appearance);

            // determine what to draw and call the methods appropriately
            if (MeshRenderers[entity.Id].IsModel)
                Draw3DModelEntity(entity);
            else
                Draw3DMeshEntity(entity);

        }

        /// <summary>
        /// Draws a simple mesh from given entity.
        /// The caller has to make sure that the entity contains simple mesh information.
        /// </summary>
        /// <param name="entity">instance of entity, which contains the simple mesh</param>
        protected virtual void Draw3DMeshEntity(Entity entity)
        {
            var transform = entity.Get<Transform>() ?? Transform.Default;
            BasicEffect.World = transform.World;

            var vertexBuffer = MeshRenderers[entity.Id].VertexBuffer;
            var indexBuffer = MeshRenderers[entity.Id].IndexBuffer;

            BasicEffect.GraphicsDevice.SetVertexBuffer(vertexBuffer);
            BasicEffect.GraphicsDevice.Indices = indexBuffer;

            foreach (var effectPass in BasicEffect.CurrentTechnique.Passes)
            {
                effectPass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0,
                    vertexBuffer.VertexCount, 0,
                    indexBuffer.IndexCount / 3);
            }
        }

        /// <summary>
        /// Draws a model from given entity.
        /// The caller has to make sure that the entity contains model information.
        /// </summary>
        /// <param name="entity">instance of entity, which contains the model</param>
        protected virtual void Draw3DModelEntity(Entity entity)
        {
            var transform = entity.Get<Transform>() ?? Transform.Default;

            Matrix scale = entity.Get<S2EModelMesh>().Scale;

            var model = MeshRenderers[entity.Id].Model;
            var transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (XnaModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect modelEffect in mesh.Effects)
                {
                    // populate model effect with data from global basic effect
                    CopyBasicEffectToModelEffect(BasicEffect, modelEffect);
                    modelEffect.World = transforms[mesh.ParentBone.Index] * scale * model.Root.Transform * transform.World;

                }
                mesh.Draw();
            }
        }

        /// <summary>
        /// Draws the given UIElement instance and all its children.
        /// </summary>
        /// <param name="element">instance of UIElement</param>
        protected virtual void DrawUIElement(UIElement element)
        {
            if (!element.Visible)
                return;

            Rectangle rect = element.GlobalRect;
            if (element.Parent != null && element.Parent.ClipChildren)
            {
                rect = Rectangle.Intersect(rect, element.Parent.GlobalRect);
            }

            string textureName = element.TextureName;
            if (textureName == string.Empty)
                textureName = element.BackgroundColor.ToString();

            if (element.ClipChildren)
            {
                // clip rect if not in bounds
                Rectangle tmp = GraphicsDevice.ScissorRectangle;
                GraphicsDevice.ScissorRectangle = rect;
                // draw self
                DrawUIElementContent(element, Textures[textureName].Texture, rect, element.Enabled ? Color.White : Color.Gray);
                GraphicsDevice.ScissorRectangle = tmp;
            }
            else
            {
                // draw self
                DrawUIElementContent(element, Textures[textureName].Texture, rect, element.Enabled ? Color.White : Color.Gray);
            }

            // draw children (recursively)
            foreach (UIElement child in element.Children)
            {
                DrawUIElement(child);
            }
        }

        /// <summary>
        /// Draws the actual content of a UIElement instance.
        /// </summary>
        /// <param name="element">instance of UIElement</param>
        /// <param name="texture">texture used to colorize the elements background</param>
        /// <param name="rect">rect to draw in (might be used for clipping)</param>
        /// <param name="bgColor">background color</param>
        protected virtual void DrawUIElementContent(UIElement element, Texture2D texture, Rectangle rect, Color bgColor)
        {
            Color fgColor = Color.White;
            if (element is UIButton)
            {
                var button = element as UIButton;
                fgColor = button.Selected ? button.SelectedForegrundColor : button.ForegroundColor;
                fgColor = button.Hightlighted ? button.HightlightedForegrundColor : fgColor;
                bgColor = button.Selected ? button.SelectedBackgroundColor : button.BackgroundColor;
                bgColor = button.Hightlighted ? button.HightlightedBackgroundColor : bgColor;
            }

            SpriteBatch.Draw(texture, rect, bgColor);

            if (element is UILabel && ((UILabel)element).Text != String.Empty)
                DrawUILabelText(rect, fgColor, element as UILabel);
        }

        /// <summary>
        /// Draws the text of given UILabel instance.
        /// </summary>
        /// <param name="rect">rect to draw in (might be used for clipping)</param>
        /// <param name="fgColor">foreground / test color</param>
        /// <param name="label">instance of UILabel</param>
        protected virtual void DrawUILabelText(Rectangle rect, Color fgColor, UILabel label)
        {
            SpriteFont font = Fonts[label.FontName].Font;
            Vector2 textSize = font.MeasureString(label.Text)*label.FontScale;
            Vector2 textPosition = label.GetTextPositionRegardingAllignment(textSize);

            var textRect = new Rectangle((int) textPosition.X, (int) textPosition.Y, (int) textSize.X, (int) textSize.Y);
            if (!rect.Contains(textRect))
            {
                // clip text if not in bouds
                Rectangle tmp = GraphicsDevice.ScissorRectangle;
                GraphicsDevice.ScissorRectangle = rect;
                SpriteBatch.DrawString(font, label.Text, textPosition, fgColor, 0,
                    Vector2.Zero, label.FontScale, SpriteEffects.None, 0);
                GraphicsDevice.ScissorRectangle = tmp;
            }
            else
            {
                SpriteBatch.DrawString(font, label.Text, textPosition, fgColor, 0,
                    Vector2.Zero, label.FontScale, SpriteEffects.None, 0);
            }
        }

        #endregion

        #region BasicEffect Related Methods

        /// <summary>
        /// Sets up the global basic effect instance with data from given appearance.
        /// </summary>
        /// <param name="appearance">appearance component to get the data from</param>
        protected virtual void SetupCommonEffectsFromAppearance(Appearance appearance)
        {
            float alpha = appearance.DiffuseColor.A / 255.0f;
            bool isWireFrame = appearance.RenderWireframe || _globalRenderWireframe;
            bool isTextureEnabled = appearance.TextureEnabled && _globalTextureEnabled;

            BasicEffect.DiffuseColor = appearance.DiffuseColor.ToVector3();
            BasicEffect.EmissiveColor = appearance.EmissiveColor.ToVector3();
            BasicEffect.SpecularColor = appearance.SpecularColor.ToVector3();
            BasicEffect.SpecularPower = appearance.SpecularPower;

            BasicEffect.Alpha = alpha;
            BasicEffect.GraphicsDevice.BlendState = BlendStateFromAlpha(alpha);
            BasicEffect.GraphicsDevice.RasterizerState =
                RasterizerStateFromAlphaAndFillMode(alpha, isWireFrame);

            BasicEffect.TextureEnabled = isTextureEnabled;
            if (isTextureEnabled && appearance.TextureName != string.Empty)
                BasicEffect.Texture = Textures[appearance.TextureName].Texture;
        }

        /// <summary>
        /// Copies the values from one basic effect instance to another.
        /// Used to set up model effects from the global basic effect.
        /// </summary>
        /// <param name="basicEffect">basic effect which provides the data</param>
        /// <param name="modelEffect">model effect to copy the data to</param>
        protected virtual void CopyBasicEffectToModelEffect(BasicEffect basicEffect, BasicEffect modelEffect)
        {
            modelEffect.World = basicEffect.World;
            modelEffect.View = basicEffect.View;
            modelEffect.Projection = basicEffect.Projection;

            modelEffect.DiffuseColor = BasicEffect.DiffuseColor;
            modelEffect.EmissiveColor = BasicEffect.EmissiveColor;
            modelEffect.SpecularColor = BasicEffect.SpecularColor;
            modelEffect.SpecularPower = BasicEffect.SpecularPower;

            modelEffect.Alpha = basicEffect.Alpha;
            modelEffect.TextureEnabled = basicEffect.TextureEnabled;
            modelEffect.LightingEnabled = basicEffect.LightingEnabled;
            modelEffect.PreferPerPixelLighting = basicEffect.PreferPerPixelLighting;
            modelEffect.VertexColorEnabled = basicEffect.VertexColorEnabled;
            modelEffect.GraphicsDevice.BlendState = basicEffect.GraphicsDevice.BlendState;
            modelEffect.GraphicsDevice.RasterizerState = basicEffect.GraphicsDevice.RasterizerState;

            modelEffect.FogStart = basicEffect.FogStart;
            modelEffect.FogEnd = basicEffect.FogEnd;
            modelEffect.FogEnabled = basicEffect.FogEnabled;
            modelEffect.FogColor = basicEffect.FogColor;
            modelEffect.AmbientLightColor = basicEffect.AmbientLightColor;

            SetupXnaDirectionalLight(modelEffect.DirectionalLight0, basicEffect.DirectionalLight0);
            SetupXnaDirectionalLight(modelEffect.DirectionalLight1, basicEffect.DirectionalLight1);
            SetupXnaDirectionalLight(modelEffect.DirectionalLight2, basicEffect.DirectionalLight2);
        }

        /// <summary>
        /// This tries to set up one of the three directional lights (provided by xna's basicEffect)
        /// with values from a given s2e directional light component. If all three lights are in
        /// use the operation will fail.
        /// </summary>
        /// <param name="light">directional light component which provides the set up values</param>
        /// <param name="entityId">id of associated entity</param>
        /// <returns>false if all three lights are used by other entities</returns>
        protected virtual bool TryToSetupDirectionalLight(DirectionalLight light, int entityId)
        {
            // if one of those light is associated with given entity id, change its values
            var assignedLight = DirectionalLights.FirstOrDefault(e => e.Value == entityId).Key;
            if (assignedLight != null)
            {
                SetupXnaDirectionalLight(assignedLight, light);
                return true;
            }

            // if we find a light which is not used, we set up its values and assign entity id to it
            var unusedLight = DirectionalLights.FirstOrDefault(e => e.Value == IDPool.InvalidID).Key;
            if (unusedLight != null)
            {
                SetupXnaDirectionalLight(unusedLight, light);
                DirectionalLights[unusedLight] = entityId;
                return true;
            }

            // return false if all three lights are used by other entities
            return false;
        }

        /// <summary>
        /// Sets up a given directional light from another instance (a s2e component in this case).
        /// </summary>
        /// <param name="light">light to set up (xna)</param>
        /// <param name="other">other light toget the data from (s2e)</param>
        private void SetupXnaDirectionalLight(XnaDirectionalLight light, DirectionalLight other)
        {
            SetupXnaDirectionalLight(light, other.Direction, other.DiffuseColor, other.SpecularColor, other.Enabled);
        }

        /// <summary>
        /// Sets up a given directional light from another instance.
        /// </summary>
        /// <param name="light">light to set up (xna)</param>
        /// <param name="other">other light toget the data from (xna)</param>
        private void SetupXnaDirectionalLight(XnaDirectionalLight light, XnaDirectionalLight other)
        {
            SetupXnaDirectionalLight(light, other.Direction, other.DiffuseColor, other.SpecularColor, other.Enabled);
        }

        /// <summary>
        /// Sets up the given directional light with given values.
        /// </summary>
        /// <param name="light">light to set up (xna)</param>
        /// <param name="direction">direction to shine in</param>
        /// <param name="diffuse">diffuse color</param>
        /// <param name="specular">specular color</param>
        /// <param name="enabled">flag if light is enabled or not</param>
        protected virtual void SetupXnaDirectionalLight(XnaDirectionalLight light, Vector3 direction, Vector3 diffuse, Vector3 specular, bool enabled)
        {
            light.Direction = direction;
            light.DiffuseColor = diffuse;
            light.SpecularColor = specular;
            light.Enabled = enabled;
        }

        /// <summary>
        /// Returns a resterizer state from given alpha value.
        /// In case that wire frame rendering is desired, set resterizer state appropriately.
        /// </summary>
        /// <param name="alpha">alpha value</param>
        /// <param name="isWireFrame">flag if wire frame is desired</param>
        /// <returns>resterizer state (with fill mode Solid or Wireframe and cull mode CullCounterClockwise or CullNone)</returns>
        protected virtual RasterizerState RasterizerStateFromAlphaAndFillMode(float alpha, bool isWireFrame)
        {
            return isWireFrame
                ? RasterizerStateWireFrame
                : (alpha < 1f)
                    ? RasterizerState.CullCounterClockwise //TODO: sort vertices, then RasterizerState.CullNone
                    : RasterizerState.CullCounterClockwise;
        }

        /// <summary>
        /// Returns a blend state from given alpha value.
        /// </summary>
        /// <param name="alpha">alpha value</param>
        /// <returns>blend state (either AlphaBlend or Opaque)</returns>
        protected virtual BlendState BlendStateFromAlpha(float alpha)
        {
            return (alpha < 1f)
                ? BlendState.AlphaBlend
                : BlendState.Opaque;
        }

        #endregion

        #region Resource Management

        /// <summary>
        /// Adds all resources associated with this entity to the system.
        /// </summary>
        /// <param name="entity">entity which was added</param>
        protected virtual void AddEntityResources(Entity entity)
        {
            // if entity contains a simple mesh, create a renderer and add it to list
            var simpleMesh = entity.Get<S2ESimpleMesh>();
            if (simpleMesh != null)
            {
                var renderer = new MeshRenderer(simpleMesh, GraphicsDevice);
                MeshRenderers.Add(entity.Id, renderer);

                // also add a texture, if present
                var appearance = entity.Get<Appearance>();
                if (appearance != null)
                {
                    AddTextureByName(appearance.TextureName);
                }
            }

            // if entity contains a model mesh, create a renderer and add it to list
            var modelMesh = entity.Get<S2EModelMesh>();
            if (modelMesh != null)
            {
                var renderer = new MeshRenderer(modelMesh, CurrentResourceManager);
                MeshRenderers.Add(entity.Id, renderer);
            }

            // if entity contains ambience information, set it up
            var ambience = entity.Get<Ambience>();
            if (ambience != null)
            {
                BasicEffect.AmbientLightColor = ambience.AmbientLightColor.ToVector3();
                BasicEffect.FogColor = ambience.FogColor.ToVector3();
                BasicEffect.FogEnabled = ambience.FogEnabled;
                BasicEffect.FogStart = ambience.FogStart;
                BasicEffect.FogEnd = ambience.FogEnd;

                _clearColor = ambience.ClearColor;
            }

            // if entity contains a directionalLight, set it up
            var directionalLight = entity.Get<DirectionalLight>();
            if (directionalLight != null)
            {
                TryToSetupDirectionalLight(directionalLight, entity.Id);
            }

            // if entity contains a user interface, add its element to list
            var userInterface = entity.Get<UserInterface>();
            if (userInterface != null)
            {
                UserInterfaces.Add(entity.Id, userInterface.RootElement);
                AddUserInterfaceResources(userInterface.RootElement);
            }

            // entity contains a camera and camera is active, set projection matrix
            var camera = entity.Get<Camera>();
            if (camera != null && camera.IsActive)
                BasicEffect.Projection = camera.Projection;
        }

        /// <summary>
        /// Removes all resources associated with this entity from the system.
        /// </summary>
        /// <param name="entity">entity which will be removed</param>
        protected virtual void RemoveEntityResources(Entity entity)
        {
            MeshRenderer renderer;
            // remove mesh renderer if associated to this entity
            if (MeshRenderers.TryGetValue(entity.Id, out renderer))
            {
                if (!renderer.IsModel) // don't dispose model, as it might contain shared textures
                {
                    renderer.VertexBuffer.Dispose();
                    renderer.IndexBuffer.Dispose();
                }
                MeshRenderers.Remove(entity.Id);

                // remove texture
                var appearance = entity.Get<Appearance>();
                if (appearance != null)
                {
                    RemoveTextureByName(appearance.TextureName);
                }
            }

            // remove user interface element if associated to this entity
            if (UserInterfaces.ContainsKey(entity.Id))
            {
                RemoveUserInterfaceResources(UserInterfaces[entity.Id]);
                UserInterfaces.Remove(entity.Id);
            }

            var ambience = entity.Get<Ambience>();
            if (ambience != null)
            {
                // unset directional lights
                foreach (XnaDirectionalLight light in DirectionalLights.Keys.ToList())
                    DirectionalLights[light] = IDPool.InvalidID;
            }
        }

        /// <summary>
        /// Adds all resources from a given user interface element.
        /// If caused by component deserialization, it might be possible what the resources
        /// are already present. In that case they won't be added and the usage counters won't
        /// be increased
        /// </summary>
        /// <param name="element">user interface element, typically a root element</param>
        /// <param name="increaseConterIfExistent">flag if texture and font counters should be increased</param>
        protected virtual void AddUserInterfaceResources(UIElement element, bool increaseConterIfExistent = true)
        {
            ICollection<Color> colors = new List<Color>();
            ICollection<string> fonts = new List<string>();
            ICollection<string> textures = new List<string>();
            element.GetResources(ref fonts, ref textures, ref colors);

            foreach (string fontName in fonts)
                AddFontByName(fontName, increaseConterIfExistent);
            foreach (string textureName in textures)
                AddTextureByName(textureName, increaseConterIfExistent);
            foreach (Color color in colors)
                AddTextureByColor(color, increaseConterIfExistent);
        }

        /// <summary>
        /// Removes all resources associated with this user interface element from the system.
        /// </summary>
        /// <param name="element">user interface element which will be removed</param>
        protected virtual void RemoveUserInterfaceResources(UIElement element)
        {
            ICollection<Color> colors = new List<Color>();
            ICollection<string> fonts = new List<string>();
            ICollection<string> textures = new List<string>();
            element.GetResources(ref fonts, ref textures, ref colors);

            foreach (string fontName in fonts)
                RemoveFontByName(fontName);
            foreach (string textureName in textures)
                RemoveTextureByName(textureName);
            foreach (Color color in colors)
                RemoveTextureByName(color.ToString());
        }

        /// <summary>
        /// Adds a texture from a given color to the system.
        /// </summary>
        /// <param name="textureColor">color which should be used by the texture</param>
        /// <param name="increaseCounterIfExistent">flag if texture and font counters should be increased</param>
        protected virtual void AddTextureByColor(Color textureColor, bool increaseCounterIfExistent = true)
        {
            Func<Texture2D> textureLoadOperation = () =>
            {
                // creates a one by one default texture, which will be stretched and colorized
                var texture = new Texture2D(GraphicsDevice, 1, 1);
                texture.SetData(new[] { textureColor });
                return texture;
            };

            AddTexture(textureLoadOperation, textureColor.ToString(), increaseCounterIfExistent);
        }

        /// <summary>
        /// Adds a texture with the associated name to the system.
        /// </summary>
        /// <param name="textureName">asset name of texture</param>
        /// <param name="increaseCounterIfExistent">flag if texture and font counters should be increased</param>
        protected virtual void AddTextureByName(string textureName, bool increaseCounterIfExistent = true)
        {
            Func<Texture2D> textureLoadOperation = () => CurrentResourceManager.Load<Texture2D>(textureName);
            AddTexture(textureLoadOperation, textureName, increaseCounterIfExistent);
        }

        /// <summary>
        /// Generic AddTexture method, which is called by AddTextureByColor and AddTextureByName.
        /// </summary>
        /// <param name="textureLoadOperation">function pointer to individual loading function</param>
        /// <param name="textureName">name to use as key to look up the texture</param>
        /// <param name="increaseCounterIfExistent">flag if texture and font counters should be increased</param>
        protected virtual void AddTexture(Func<Texture2D> textureLoadOperation, string textureName, bool increaseCounterIfExistent = true)
        {
            if (textureName == string.Empty)
                return;

            if (!Textures.ContainsKey(textureName))
            {
                var texture = textureLoadOperation.Invoke();
                Textures.Add(textureName, new SharedTexture(texture, 1));
            }
            else if (increaseCounterIfExistent)
            {
                Textures[textureName].Count++;
            }
        }

        /// <summary>
        /// Adds a font with the associated name to the system.
        /// </summary>
        /// <param name="fontName">asset name of font</param>
        /// <param name="increaseCounterIfExistent">flag if texture and font counters should be increased</param>
        protected virtual void AddFontByName(string fontName, bool increaseCounterIfExistent = true)
        {
            if (fontName == string.Empty)
                return;

            if (!Fonts.ContainsKey(fontName))
            {
                var font = CurrentResourceManager.Load<SpriteFont>(fontName);
                Fonts.Add(fontName, new SharedFont(font, 1));
            }
            else if (increaseCounterIfExistent)
            {
                Fonts[fontName].Count++;
            }
        }

        /// <summary>
        /// Removes the texture associated to given name.
        /// It actually only removes the texture if not used by any other clients any more,
        /// otherwise it only decreases its usage counter.
        /// </summary>
        /// <param name="textureName">name to use as key to look up the texture</param>
        protected virtual void RemoveTextureByName(string textureName)
        {
            if (!Textures.ContainsKey(textureName))
                return;

            Textures[textureName].Count--;
            if (Textures[textureName].Count == 0)
            {
                Textures[textureName].Texture.Dispose();
                Textures.Remove(textureName);
            }
        }

        /// <summary>
        /// Removes the font associated to given name.
        /// It actually only removes the font if not used by any other clients any more,
        /// otherwise it only decreases its usage counter.
        /// </summary>
        /// <param name="fontName">name to use as key to look up the font</param>
        protected virtual void RemoveFontByName(string fontName)
        {
            if (!Fonts.ContainsKey(fontName))
                return;

            Fonts[fontName].Count--;
            if (Fonts[fontName].Count == 0)
            {
                Fonts.Remove(fontName);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Resizes all user interfaces of the game upon viewport change.
        /// </summary>
        /// <param name="factorX">horizontal rescaling factor</param>
        /// <param name="factorY">vertical rescaling factor</param>
        public virtual void ResizeUserInterfaces(float factorX, float factorY)
        {
            foreach (var userInterface in Component.GetAll<UserInterface>())
            {
                // resize all user interfaces of the game
                userInterface.RootElement.Resize(factorX, factorY);
                // update references to interfaces currently used
                int entityId = userInterface.GetHostingEntity().Id;
                if (UserInterfaces.ContainsKey(entityId))
                    UserInterfaces[entityId] = userInterface.RootElement;
            }
        }

        #region Protected Class Definitions

        /// <summary>
        /// Texture2D wrapper class to decorate it with a counter.
        /// Counter keeps track of the number of clients using this texture.
        /// </summary>
        protected class SharedTexture
        {
            public Texture2D Texture { get; private set; }
            public int Count { get; set; }

            public SharedTexture(Texture2D texture, int count)
            {
                Texture = texture;
                Count = count;
            }
        }

        /// <summary>
        /// SpriteFont wrapper class to decorate it with a counter.
        /// Counter keeps track of the number of clients using this font.
        /// </summary>
        protected class SharedFont
        {
            public SpriteFont Font { get; private set; }
            public int Count { get; set; }

            public SharedFont(SpriteFont font, int count)
            {
                Font = font;
                Count = count;
            }
        }

        #endregion
    }

    public static class DichtionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            lock (dict)
            {
                if (dict.ContainsKey(key))
                    return false;
                dict.Add(key, value);
                return true;
            }
        }
    }
}
