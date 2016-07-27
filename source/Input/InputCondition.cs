using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Sol2E.Input
{
    public interface IInputCondition
    {
        bool Active { get; set; }
        bool IsMet(out object value);
    }

    /// <summary>
    /// A complex input condition, which consists of a collection of simple input conditions.
    /// All subconditions have to be met in order for IsMet to return true.
    /// </summary>
    [Serializable]
    public class ComplexInputCondition : IInputCondition
    {
        public bool Active { get; set; }

        private readonly ICollection<SimpleInputCondition> _simpleConditions;

        public ComplexInputCondition(ICollection<SimpleInputCondition> simpleConditions)
        {
            _simpleConditions = simpleConditions;
            Active = true;
        }

        /// <summary>
        /// Determines if this input conditon is met.
        /// </summary>
        /// <param name="value">Amout of how far the source has moved (out).
        /// Will be undefined for key or button sources. Only returns the
        /// value of the last checked subcondition. So mind the order, if you
        /// are dependent on that.</param>
        /// <returns>true if all conditions are met</returns>
        public bool IsMet(out object value)
        {
            value = 0;
            foreach(SimpleInputCondition condition in _simpleConditions)
            {
                if (!condition.IsMet(out value))
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A simple input condition, which is defined by an input source and an input state.
    /// If the specified source is in the specified state then the condition is met.
    /// </summary>
    [Serializable]
    public class SimpleInputCondition : IInputCondition
    {
        public bool Active { get; set; }

        public InputSource Source { get; set; }
        public InputState State { get; set; }

        public SimpleInputCondition(InputSource source, InputState state)
        {
            Source = source;
            State = state;
            Active = true;
        }

        /// <summary>
        /// Determines if this input conditon is met.
        /// </summary>
        /// <param name="value">Amout of how far the source has moved (out).
        /// Will be undefined for key or button sources</param>
        /// <returns>true if condition is met, else false</returns>
        public bool IsMet(out object value)
        {
            value = 0;

            switch (State)
            {
                case InputState.Up:
                    return SourceIsUp(Source);
                case InputState.Down:
                    return SourceIsDown(Source);
                case InputState.Pressed:
                    return SourceGotPressed(Source);
                case InputState.Released:
                    return SourceGotReleased(Source);
                case InputState.Moved:
                    return SourceHasMoved(Source, out value);
            }

            return false;
        }

        /// <summary>
        /// Determines if given input source is not touched at all.
        /// </summary>
        /// <param name="source">input source (key or mouse button)</param>
        /// <returns>true if not toched, else false</returns>
        private bool SourceIsUp(InputSource source)
        {
            switch (source)
            {
                case InputSource.MouseLeft:
                    return InputDevice.MouseCurrent.LeftButton == ButtonState.Released;
                case InputSource.MouseRight:
                    return InputDevice.MouseCurrent.RightButton == ButtonState.Released;
                case InputSource.MouseMiddle:
                    return InputDevice.MouseCurrent.MiddleButton == ButtonState.Released;
                case InputSource.MouseWeel:
                case InputSource.MouseMoveX:
                case InputSource.MouseMoveY:
                    throw new ArgumentException("Mouse movement can't be released.");
                default:
                    return InputDevice.KeyboardCurrent.IsKeyUp((Keys)source);
            }
        }

        /// <summary>
        /// Determines if given input source is held down.
        /// </summary>
        /// <param name="source">input source (key or mouse button)</param>
        /// <returns>true if held down, else false</returns>
        private bool SourceIsDown(InputSource source)
        {
            switch (source)
            {
                case InputSource.MouseLeft:
                    return InputDevice.MouseCurrent.LeftButton == ButtonState.Pressed;
                case InputSource.MouseRight:
                    return InputDevice.MouseCurrent.RightButton == ButtonState.Pressed;
                case InputSource.MouseMiddle:
                    return InputDevice.MouseCurrent.MiddleButton == ButtonState.Pressed;
                case InputSource.MouseWeel:
                case InputSource.MouseMoveX:
                case InputSource.MouseMoveY:
                    throw new ArgumentException("Mouse movement can't be pressed.");
                default:
                    return InputDevice.KeyboardCurrent.IsKeyDown((Keys)source);
            }
        }

        /// <summary>
        /// Determines if given input source has been pressed.
        /// </summary>
        /// <param name="source">input source (key or mouse button)</param>
        /// <returns>true if pressed, else false</returns>
        private bool SourceGotPressed(InputSource source)
        {
            switch (source)
            {
                case InputSource.MouseLeft:
                    return InputDevice.MouseCurrent.LeftButton == ButtonState.Pressed
                        && InputDevice.MousePrevious.LeftButton == ButtonState.Released;
                case InputSource.MouseRight:
                    return InputDevice.MouseCurrent.RightButton == ButtonState.Pressed
                        && InputDevice.MousePrevious.RightButton == ButtonState.Released;
                case InputSource.MouseMiddle:
                    return InputDevice.MouseCurrent.MiddleButton == ButtonState.Pressed
                        && InputDevice.MousePrevious.MiddleButton == ButtonState.Released;
                case InputSource.MouseWeel:
                case InputSource.MouseMoveX:
                case InputSource.MouseMoveY:
                    throw new ArgumentException("Mouse movement can't be pressed.");
                default:
                    return InputDevice.KeyboardCurrent.IsKeyDown((Keys)source)
                        && InputDevice.KeyboardPrevious.IsKeyUp((Keys)source);
            }
        }

        /// <summary>
        /// Determines if given input source has been released.
        /// </summary>
        /// <param name="source">input source (key or mouse button)</param>
        /// <returns>true if realeased, else false</returns>
        private bool SourceGotReleased(InputSource source)
        {
            switch (source)
            {
                case InputSource.MouseLeft:
                    return InputDevice.MouseCurrent.LeftButton == ButtonState.Released
                        && InputDevice.MousePrevious.LeftButton == ButtonState.Pressed;
                case InputSource.MouseRight:
                    return InputDevice.MouseCurrent.RightButton == ButtonState.Released
                        && InputDevice.MousePrevious.RightButton == ButtonState.Pressed;
                case InputSource.MouseMiddle:
                    return InputDevice.MouseCurrent.MiddleButton == ButtonState.Released
                        && InputDevice.MousePrevious.MiddleButton == ButtonState.Pressed;
                case InputSource.MouseWeel:
                case InputSource.MouseMoveX:
                case InputSource.MouseMoveY:
                    throw new ArgumentException("Mouse movement can't be released.");
                default:
                    return InputDevice.KeyboardCurrent.IsKeyUp((Keys)source)
                        && InputDevice.KeyboardPrevious.IsKeyDown((Keys)source);
            }
        }

        /// <summary>
        /// Determines if given input source has moved.
        /// </summary>
        /// <param name="source">input source</param>
        /// <param name="value">amout of how far the source has moved (out)</param>
        /// <returns>true if movement occured, else false</returns>
        private bool SourceHasMoved(InputSource source, out object value)
        {
            switch (source)
            {
                case InputSource.MouseWeel:
                    value = InputDevice.MouseRelativeWeel;
                    return InputDevice.MouseRelativeWeel != 0;
                case InputSource.MouseMoveX:
                    value = InputDevice.MouseRelativeX;
                    return InputDevice.MouseRelativeX != 0;
                case InputSource.MouseMoveY:
                    value = InputDevice.MouseRelativeY;
                    return InputDevice.MouseRelativeY != 0;
                default:
                    throw new ArgumentException("Keys can't move.");
            }
        }
    }
}
