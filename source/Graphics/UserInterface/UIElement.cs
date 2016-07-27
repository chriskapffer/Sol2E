using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Sol2E.Input;

namespace Sol2E.Graphics.UI
{
    /// <summary>
    /// This is the base class for any element in a 2D user interface.
    /// </summary>
    [Serializable]
    public class UIElement
    {
        #region Proteries and Fields

        public string Title { get; set; }
        public UIElement Parent { get; set; }
        public ICollection<UIElement> Children { get; private set; }
        public bool ClipChildren { get; set; }

        private Rectangle _rectangle;
        public Rectangle LocalRect
        {
            get { return _rectangle; }
        }
        public Rectangle GlobalRect
        {
            get
            {
                Rectangle result = _rectangle;
                Vector2 globalPosition = GlobalPos;
                result.X = (int)globalPosition.X;
                result.Y = (int)globalPosition.Y;
                return result;
            }
        }

        public Vector2 LocalPos
        {
            get { return new Vector2(_rectangle.X, _rectangle.Y); }
            set { _rectangle.X = (int)value.X; _rectangle.Y = (int)value.Y; }
        }
        public Vector2 GlobalPos
        {
            get
            {
                var result = new Vector2(_rectangle.X, _rectangle.Y);
                if (Parent != null)
                {
                    Vector2 parentPosition = Parent.GlobalPos;
                    result += parentPosition;
                }
                return result;
            }
            set
            {
                var newPosition = value;
                if (Parent != null)
                {
                    Vector2 parentPosition = Parent.GlobalPos;
                    newPosition -= parentPosition;
                }
                _rectangle.X = (int)newPosition.X; _rectangle.Y = (int)newPosition.Y;
            }
        }
        
        public int Width
        {
            get { return _rectangle.Width; }
            set { _rectangle.Width = value; }
        }
        public int Height
        {
            get { return _rectangle.Height; }
            set { _rectangle.Height = value; }
        }

        public string TextureName { get; set; }
        public Color BackgroundColor { get; set; }

        public bool Enabled { get; set; }
        public bool Visible { get; set; }

        #endregion

        public UIElement(Rectangle rect, string title = "")
        {
            _rectangle = rect;
            Title = title;

            Parent = null;
            Children = new List<UIElement>();
            ClipChildren = false;

            TextureName = string.Empty;
            BackgroundColor = Color.Transparent;

            Enabled = true;
            Visible = true;
        }

        #region Tree Manipulation Methods

        /// <summary>
        /// Adds an element as a child to this element.
        /// Its position is relative to its parent.
        /// </summary>
        /// <param name="element">child element to add</param>
        public void AddChildElemet(UIElement element)
        {
            if (element.Parent != null)
                element.RemoveFromParent();
            Children.Add(element);
            element.Parent = this;
        }

        /// <summary>
        /// Removes the given element from this element.
        /// If this element doesn't contain the given element
        /// the method returns false.
        /// </summary>
        /// <param name="element">child element to remove</param>
        /// <returns>true if element could be removed</returns>
        public bool RemoveChildElement(UIElement element)
        {
            if (Children.Remove(element))
            {
                element.Parent = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes this instance from its parent. This will never
        /// fail. If called on a root element, nothing happens.
        /// </summary>
        public void RemoveFromParent()
        {
            if (Parent != null)
            {
                Parent.RemoveChildElement(this);
            }
        }

        /// <summary>
        /// Traverses the element tree and returns the element
        /// which title matches the given title.
        /// </summary>
        /// <param name="title">title to search for</param>
        /// <returns>element with matching title or null</returns>
        public UIElement GetChildByTitle(string title)
        {
            UIElement result = null;
            foreach (UIElement child in Children)
            {
                if (child.Title != string.Empty && child.Title == title)
                    return child;

                result = child.GetChildByTitle(title);
                if (result != null)
                    break;
            }

            return result;
        }

        /// <summary>
        /// Retrieves the root element of the tree to
        /// which this element belogs to.
        /// </summary>
        /// <returns>root element</returns>
        public UIElement GetRoot()
        {
            UIElement result = this;
            while (result.Parent != null)
                result = result.Parent;

            return result;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Updates the element tree. This is used to listen for
        /// mouse activity like hover or click. Calls protected
        /// Update(Rectangle) method.
        /// </summary>
        /// <returns>true if cursor is within the frame of this element</returns>
        internal bool Update()
        {
            // stop updating and return false, to show that update
            // of parent elements has yet to be performed
            if (!Enabled)
                return false;

            // call update on children -> depth first search
            // if a child's update returns true, the cursor lies within its rect,
            // so we don't need to update this element any further.
            if (Children.Any(child => child.Update()))
                return true;

            // only update the protion of the rect which is not clipped by its parent
            Rectangle rect = GlobalRect;
            if (Parent != null && Parent.ClipChildren)
                rect = Rectangle.Intersect(rect, Parent.GlobalRect);

            // the actual update method for the given rect
            return Update(rect);
        }

        /// <summary>
        /// Resizes this element and all its children upon viewport change.
        /// </summary>
        /// <param name="factorX">horizontal rescaling factor</param>
        /// <param name="factorY">vertical rescaling factor</param>
        internal virtual void Resize(float factorX, float factorY)
        {
            _rectangle = new Rectangle(
                (int)Math.Floor(_rectangle.X * factorX), (int)Math.Floor(_rectangle.Y * factorY),
                (int)Math.Ceiling(_rectangle.Width * factorX), (int)Math.Ceiling(_rectangle.Height * factorY));

            // recursion
            foreach (UIElement element in Children)
                element.Resize(factorX, factorY);
        }

        /// <summary>
        /// Retrieves the names of used textures or fonts for this element tree.
        /// If an element doesn't use its texture, its background color is returned
        /// instead, to set the default's texture property.
        /// </summary>
        /// <param name="fonts">collection of used font names (ref)</param>
        /// <param name="textures">collection of used texture names (ref)</param>
        /// <param name="colors">collection of used colors (ref)</param>
        internal virtual void GetResources(ref ICollection<string> fonts, ref ICollection<string> textures, ref ICollection<Color> colors)
        {
            if(TextureName != string.Empty)
                textures.Add(TextureName);
            else
                colors.Add(BackgroundColor);

            // recursion
            foreach (UIElement element in Children)
                element.GetResources(ref fonts, ref textures, ref colors);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Protected update. 
        /// </summary>
        /// <param name="rect">rect to perform the update in</param>
        /// <returns>true if mouse cursor is within given rect</returns>
        protected virtual bool Update(Rectangle rect)
        {
            Vector2 mousePosition = InputDevice.MousePosition;
            return rect.Contains((int)mousePosition.X, (int)mousePosition.Y);
        }

        #endregion
    }
}
