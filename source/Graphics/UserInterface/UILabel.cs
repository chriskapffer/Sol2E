using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sol2E.Graphics.UI
{
    // possible positions for vertical text allignment
    public enum UIVerticalTextAllignment
    {
        Top,
        Center,
        Bottom
    };

    // possible positions for horizontal text allignment
    public enum UIHorizontalTextAllignment
    {
        Left,
        Center,
        Right
    };

    /// <summary>
    /// Subclass of UIElement, which can display text.
    /// </summary>
    [Serializable]
    public class UILabel : UIElement
    {
        #region Proteries and Fields

        public string Text { get; set; }
        public string FontName { get; set; }
        public float FontScale { get; set; }

        public Color ForegroundColor { get; set; }
        public UIVerticalTextAllignment VerticalTextAllignment { get; set; }
        public UIHorizontalTextAllignment HorizontalTextAllignment { get; set; }

        #endregion

        public UILabel(Rectangle rect, string fontName, string title = "")
            : base(rect, title)
        {
            Text = String.Empty;
            FontName = fontName;
            FontScale = 1.0f;

            ForegroundColor = Color.White;
            VerticalTextAllignment = UIVerticalTextAllignment.Center;
            HorizontalTextAllignment = UIHorizontalTextAllignment.Left;
        }

        /// <summary>
        /// Special GetResources function to account for font names.
        /// </summary>
        /// <param name="fonts">collection of used font names (ref)</param>
        /// <param name="textures">collection of used texture names (ref)</param>
        /// <param name="colors">collection of used colors (ref)</param>
        internal override void GetResources(ref ICollection<string> fonts, ref ICollection<string> textures, ref ICollection<Color> colors)
        {
            if (FontName != string.Empty)
                fonts.Add(FontName);

            // call base class' GetResources method
            base.GetResources(ref fonts, ref textures, ref colors);
        }

        /// <summary>
        /// Special Resize function to account for font resizing.
        /// </summary>
        /// <param name="factorX">horizontal rescaling factor</param>
        /// <param name="factorY">vertical rescaling factor</param>
        internal override void Resize(float factorX, float factorY)
        {
            // rescale font
            FontScale = Math.Min(FontScale * factorX, FontScale * factorY);
            // call base class' Resize method
            base.Resize(factorX, factorY);
        }

        /// <summary>
        /// Returns the position where text rendering should take place.
        /// </summary>
        /// <param name="textSize">width and height of given text in pixel</param>
        /// <returns>upper left corner of text, where drawing should begin</returns>
        public Vector2 GetTextPositionRegardingAllignment(Vector2 textSize)
        {
            Vector2 result = GlobalPos;
            switch (HorizontalTextAllignment)
            {
                case UIHorizontalTextAllignment.Center:
                    result.X += (int)((Width - textSize.X) / 2);
                    break;
                case UIHorizontalTextAllignment.Right:
                    result.X += Width - textSize.X;
                    break;
                case UIHorizontalTextAllignment.Left:
                    break;
            }
            switch (VerticalTextAllignment)
            {
                case UIVerticalTextAllignment.Center:
                    result.Y += (int)((Height - textSize.Y) / 2);
                    break;
                case UIVerticalTextAllignment.Bottom:
                    result.Y += Height - textSize.Y;
                    break;
                case UIVerticalTextAllignment.Top:
                    break;
            }
            return result;
        }
    }
}
