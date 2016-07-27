using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Sol2E.Input
{
    /// <summary>
    /// Static wrapper class around xna's keyboard and mouse. Keeps track
    /// of previous and current state, thus can react to state changes.
    /// </summary>
    public static class InputDevice
    {
        public static Vector2 ScreenCenter { private get; set; }

        // keyboard and mouse states
        internal static KeyboardState KeyboardPrevious { get; private set; }
        internal static KeyboardState KeyboardCurrent { get; private set; }
        internal static MouseState MousePrevious { get; private set; }
        internal static MouseState MouseCurrent { get; private set; }

        private static bool _lockMouse;
        public static bool IsMouseLocked
        {
            get
            {
                return _lockMouse;
            }
            set
            {
                if (_lockMouse != value)
                {
                    _lockMouse = value;
                    ResetMouseToCenter();
                }
            }
        }
        public static bool IsMouseClicked { get; private set; }
        
        public static Vector2 MousePosition
        {
            get { return new Vector2(MouseCurrent.X, MouseCurrent.Y); }
        }
        public static int MouseRelativeX
        {
            get {  return MouseCurrent.X - MousePrevious.X; }
        }
        public static int MouseRelativeY
        {
            get { return MouseCurrent.Y - MousePrevious.Y; }
        }
        public static int MouseRelativeWeel
        {
            get { return MouseCurrent.ScrollWheelValue - MousePrevious.ScrollWheelValue; }
        }

        static InputDevice()
        {
            KeyboardCurrent = Keyboard.GetState();
            KeyboardPrevious = Keyboard.GetState();
            MouseCurrent = Mouse.GetState();
            MousePrevious = Mouse.GetState();
        }

        /// <summary>
        /// Updates current keyboard and mouse state. Called at the beginning of Game.Update
        /// </summary>
        internal static void UpdateCurrent()
        {
            KeyboardCurrent = Keyboard.GetState();
            MouseCurrent = Mouse.GetState();
        }

        /// <summary>
        /// Updates previous keyboard and mouse state. Called at the end of Game.Update
        /// </summary>
        internal static void UpdatePrevious()
        {
            if (IsMouseLocked) { ResetMouseToCenter(); }

            IsMouseClicked = MouseCurrent.LeftButton == ButtonState.Pressed
                && MousePrevious.LeftButton == ButtonState.Released;

            KeyboardPrevious = KeyboardCurrent;
            MousePrevious = MouseCurrent;        
        }

        /// <summary>
        /// If mouse is locked, this gets called each frame.
        /// </summary>
        private static void ResetMouseToCenter()
        {
            Mouse.SetPosition((int)ScreenCenter.X, (int)ScreenCenter.Y);
            MouseCurrent = Mouse.GetState();
            MousePrevious = Mouse.GetState();
        }
    }
}
