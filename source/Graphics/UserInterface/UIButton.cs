using System;
using Microsoft.Xna.Framework;
using Sol2E.Input;

namespace Sol2E.Graphics.UI
{
    /// <summary>
    /// Subclass of UILabel, which reacts to mouse hover and mouse clicks.
    /// </summary>
    [Serializable]
    public class UIButton : UILabel
    {
        #region Proteries and Fields

        public static TimeSpan DurationOfStayingSelected = TimeSpan.FromMilliseconds(50);
        private DateTime _timeWhenLastSelected;

        public Color HightlightedBackgroundColor { get; set; }
        public Color HightlightedForegrundColor { get; set; }
        public bool Hightlighted { get; private set; }

        public Color SelectedBackgroundColor { get; set; }
        public Color SelectedForegrundColor { get; set; }
        public bool Selected { get; private set; }

        #endregion

        public UIButton(Rectangle rect, string fontName, string title = "")
            : base(rect, fontName, title)
        {
            HorizontalTextAllignment = UIHorizontalTextAllignment.Center;

            BackgroundColor = Color.Black.AddAlpha(128);
            SelectedBackgroundColor = BackgroundColor.Inverted();
            SelectedForegrundColor = ForegroundColor.Inverted();
            HightlightedBackgroundColor = BackgroundColor;
            HightlightedForegrundColor = ForegroundColor;

            Selected = false;
        }

        /// <summary>
        /// Special Update method to set selected and highlighted flags, regarding mouse cursor position
        /// and to invoke a UIButtonClickedEvent if cursor was inside given rect and mouse was clicked.
        /// </summary>
        /// <param name="rect">rect to perform the update in</param>
        /// <returns>true if mouse cursor is within given rect</returns>
        protected override bool Update(Rectangle rect)
        {
            // determine if mouse is within rect
            Vector2 mousePosition = InputDevice.MousePosition;
            bool mouseIsOver = rect.Contains((int)mousePosition.X, (int)mousePosition.Y);

            // mark element as selected, store current time and invoke click event
            if (!Selected && mouseIsOver && InputDevice.IsMouseClicked)
            {
                Selected = true;
                _timeWhenLastSelected = DateTime.Now;
                UIButtonClickedEvent.Invoke(this, EventArgs.Empty);
            }

            // deselect element if selection time exceeded the duration of staying selected
            if (Selected && (DateTime.Now - _timeWhenLastSelected) > DurationOfStayingSelected)
            {
                Selected = false;
            }

            // set highlighted flag regarding given states
            Hightlighted = mouseIsOver && !Selected;

            return mouseIsOver;
        }
    }
}
