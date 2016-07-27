using System;
using Sol2E.Common;
using Sol2E.Graphics.UI;

namespace Sol2E.Graphics
{
    /// <summary>
    /// Abstract script, which listens for a UIButtonClickedEvent and executes OnButtonClicked
    /// </summary>
    [Serializable]
    public abstract class ButtonClickedScript : ScriptCollectionItem
    {
        protected ButtonClickedScript()
        {
            UIButtonClickedEvent.UIButtonClicked += OnButtonClicked;
        }

        public abstract void OnButtonClicked(UIButton sender, EventArgs e);
    }
}
