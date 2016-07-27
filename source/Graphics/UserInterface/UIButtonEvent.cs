using System;

namespace Sol2E.Graphics.UI
{
    public delegate void UIButtonClickedEventHandler(UIButton sender, EventArgs e);

    /// <summary>
    /// Static event, where clients can register to, to listen for user interface button clicks.
    /// </summary>
    public static class UIButtonClickedEvent
    {
        public static event UIButtonClickedEventHandler UIButtonClicked;

        public static void Invoke(UIButton sender, EventArgs e)
        {
            if (UIButtonClicked != null)
                UIButtonClicked(sender, e);
        }
    }
}
