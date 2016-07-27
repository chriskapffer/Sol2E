using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Sol2E;
using Sol2E.Audio;
using Sol2E.Common;
using Sol2E.Core;
using Sol2E.Graphics;
using Sol2E.Graphics.UI;
using Sol2E.Input;

namespace GameTemplate
{
    /// <summary>
    /// User interface creation.
    /// </summary>
    public static class GameMenu
    {
        public static string GameStateInfo { get; set; }

        public static void Initialize()
        {
            var bounds = new Rectangle(0, 0,
                VitalGameMethods.ScreenRect.Width,
                VitalGameMethods.ScreenRect.Height);

            CreateMainMenu(bounds, "fonts/segoe14");
            CreateOptionsMenu(bounds, "fonts/segoe10Bold");
        }

        #region Menu Creation

        private static void CreateMainMenu(Rectangle screen, string fontName)
        {
            const int buttonWidth = 200;
            const int buttonHeight = 30;

            var buttonRect = new Rectangle(
                (screen.Width - buttonWidth) / 2,
                screen.Height - 6 * buttonHeight,
                buttonWidth,
                buttonHeight);

            var labelHeight = buttonHeight * 2;
            var labelRect = new Rectangle(0, (screen.Height - labelHeight) / 2, screen.Width, labelHeight);
            var lblGameState = new UILabel(labelRect, "fonts/segoe14", "gameState")
            {
                HorizontalTextAllignment = UIHorizontalTextAllignment.Center
            };

            var btnContinue = CreateMenuButton(buttonRect, fontName, "Continue");
            buttonRect.Y += buttonHeight + 5;
            var btnOptions = CreateMenuButton(buttonRect, fontName, "Options");
            buttonRect.Y += buttonHeight + 5;
            var btnExit = CreateMenuButton(buttonRect, fontName, "Exit");

            var mainMenu = new UIElement(screen)
                { BackgroundColor = Color.Black.AddAlpha(128), Visible = false, Enabled = false };
            mainMenu.AddChildElemet(lblGameState);
            mainMenu.AddChildElemet(btnContinue);
            mainMenu.AddChildElemet(btnOptions);
            mainMenu.AddChildElemet(btnExit);
            mainMenu.AddChildElemet(new UILabel(
                new Rectangle(screen.Width - 240, screen.Height - 100, 220, 90), "fonts/segoe08")
            {
                Text = "Press F5 for quick save.\n"
                    + "Press F6 for quick restore.\n"
                    + "Press F7 to restart the level.\n\n"
                    + "Press F1 to toggle debug info.\n"
            });

            Entity mainMenuEntity = Entity.Create();
            mainMenuEntity.AddComponent(new AudioSource("sounds/click") { Volume = 0.7f } );
            mainMenuEntity.AddComponent(new UserInterface("mainMenu", mainMenu));
            mainMenuEntity.AddComponent(new ScriptCollection<InputScript>(
                new GenericKeyPressScript(InputSource.KeyEscape, ToggleMainMenu)));
            mainMenuEntity.AddComponent(new ScriptCollection<ButtonClickedScript>
            {
                new GenericButtonClickedScript(btnContinue.Title, ToggleMainMenu, true),
                new GenericButtonClickedScript(btnOptions.Title, ToggleOptionsMenu, true),
                new GenericButtonClickedScript(btnExit.Title, VitalGameMethods.Exit, true),
            });

            Scene.Global.AddEntity(mainMenuEntity);
            ToggleMainMenu();
        }

        public static void CreateOptionsMenu(Rectangle screen, string fontName)
        {
            var dim = new Point(350, 30);

            UIButton btnWfOn; UIButton btnWfOff;
            UIButton btnLtOn; UIButton btnLtOff;
            UIButton btnPpOn; UIButton btnPpOff;
            UIButton btnFsOn; UIButton btnFsOff;

            UIButton btnMv1; UIButton btnMv2; UIButton btnMv3;
            UIButton btnIl1; UIButton btnIl2; UIButton btnIl3;
            UIButton btnMs1; UIButton btnMs2; UIButton btnMs3;

            var lblGraphics = new UILabel(new Rectangle(50, 50, dim.X, dim.Y), fontName) { Text = "Graphics Settings" };
            var containerLighting = CreateOnOffSwitch(new Rectangle(50, 100, dim.X, dim.Y), fontName, "Lighting", out btnLtOn, out btnLtOff);
            var containerPerPixel = CreateOnOffSwitch(new Rectangle(50, 150, dim.X, dim.Y), fontName, "Per Pixel Lighting", out btnPpOn, out btnPpOff);
            var containerWireFrame = CreateOnOffSwitch(new Rectangle(50, 200, dim.X, dim.Y), fontName, "Wireframe", out btnWfOn, out btnWfOff);

            var lblSound = new UILabel(new Rectangle(50, 250, dim.X, dim.Y), fontName) { Text = "Sound Settings" };
            var containerVolume = CreateTripleOption(new Rectangle(50, 300, dim.X, dim.Y), fontName, "Master Volume", "0", "0.5", "1",
                out btnMv1, out btnMv2, out btnMv3);

            var lblPerformance = new UILabel(new Rectangle(450, 50, dim.X, dim.Y), fontName) { Text = "Performance Settings" };
            var containerFixedSteps = CreateOnOffSwitch(new Rectangle(450, 100, dim.X, dim.Y), fontName, "Fixed Time Steps", out btnFsOn, out btnFsOff);
            var containerMaxSteps = CreateTripleOption(new Rectangle(450, 150, dim.X, dim.Y), fontName, "Max Time Steps", "1", "3", "5",
                out btnMs1, out btnMs2, out btnMs3);
            var containerIterations = CreateTripleOption(new Rectangle(450, 200, dim.X, dim.Y), fontName, "Iteration Limit", "5", "10", "15",
                out btnIl1, out btnIl2, out btnIl3);

            var btnBack = CreateMenuButton(
                new Rectangle((screen.Width - 200) / 2, screen.Height - 60, 200, 30), fontName, "Back");

            var optionsMenu = new UIElement(screen)
                { BackgroundColor = Color.Black.AddAlpha(128), Visible = false, Enabled = false };
            optionsMenu.AddChildElemet(lblGraphics);
            optionsMenu.AddChildElemet(lblSound);
            optionsMenu.AddChildElemet(lblPerformance);
            optionsMenu.AddChildElemet(containerVolume);
            optionsMenu.AddChildElemet(containerMaxSteps);
            optionsMenu.AddChildElemet(containerIterations);
            optionsMenu.AddChildElemet(containerWireFrame);
            optionsMenu.AddChildElemet(containerLighting);
            optionsMenu.AddChildElemet(containerPerPixel);
            optionsMenu.AddChildElemet(containerFixedSteps);
            optionsMenu.AddChildElemet(btnBack);

            Entity optionsMenuEntity = Entity.Create();
            optionsMenuEntity.AddComponent(new AudioSource("sounds/click") { Volume = 0.7f } );
            optionsMenuEntity.AddComponent(new UserInterface("optionsMenu", optionsMenu));
            optionsMenuEntity.AddComponent(new ScriptCollection<ButtonClickedScript>
            {
                new GenericButtonClickedScript(btnFsOn.Title, () => VitalGameMethods.UseFixedTimeSteps = true, true),
                new GenericButtonClickedScript(btnWfOn.Title, () => VitalGameMethods.RenderWireframe = true, true),
                new GenericButtonClickedScript(btnLtOn.Title, () => VitalGameMethods.LightingEnabled = true, true),
                new GenericButtonClickedScript(btnPpOn.Title, () => VitalGameMethods.PreferPerPixelLighting = true, true),
                new GenericButtonClickedScript(btnFsOff.Title, () => VitalGameMethods.UseFixedTimeSteps = false, true),
                new GenericButtonClickedScript(btnWfOff.Title, () => VitalGameMethods.RenderWireframe = false, true),
                new GenericButtonClickedScript(btnLtOff.Title, () => VitalGameMethods.LightingEnabled = false, true),
                new GenericButtonClickedScript(btnPpOff.Title, () => VitalGameMethods.PreferPerPixelLighting = false, true),
                new GenericButtonClickedScript(btnMv1.Title, () => VitalGameMethods.MasterVolume = 0f, true),
                new GenericButtonClickedScript(btnMv2.Title, () => VitalGameMethods.MasterVolume = 0.5f, true),
                new GenericButtonClickedScript(btnMv3.Title, () => VitalGameMethods.MasterVolume = 1f, true),
                new GenericButtonClickedScript(btnMs1.Title, () => VitalGameMethods.MaxStepsPerFrame = 1, true),
                new GenericButtonClickedScript(btnMs2.Title, () => VitalGameMethods.MaxStepsPerFrame = 3, true),
                new GenericButtonClickedScript(btnMs3.Title, () => VitalGameMethods.MaxStepsPerFrame = 5, true),
                new GenericButtonClickedScript(btnIl1.Title, () => VitalGameMethods.IterationLimit = 5, true),
                new GenericButtonClickedScript(btnIl2.Title, () => VitalGameMethods.IterationLimit = 10, true),
                new GenericButtonClickedScript(btnIl3.Title, () => VitalGameMethods.IterationLimit = 15, true),
                new GenericButtonClickedScript(btnBack.Title, ToggleOptionsMenu)
            });

            Scene.Global.AddEntity(optionsMenuEntity);
        }

        public static void CreateDebugInfo(Sol2EGame game, bool initiallyVisible)
        {
            var debugLabel = new UILabel(new Rectangle(5, 5, 345, 165), "fonts/segoe10")
            {
                VerticalTextAllignment = UIVerticalTextAllignment.Top
            };

            var debugInfo = new UIElement(new Rectangle(20, 20, 355, 175))
            {
                BackgroundColor = Color.Black.AddAlpha(128),
                ClipChildren = true,
                Visible = initiallyVisible
            };
            debugInfo.AddChildElemet(debugLabel);

            Entity debugInfoEntity = Entity.Create();
            debugInfoEntity.AddComponent(new UserInterface("debugInfo", debugInfo));
            debugInfoEntity.AddComponent(new ScriptCollection<InputScript>
                { new GenericKeyPressScript(InputSource.KeyF1, ToggleDebugInfo) });

            debugInfoEntity.AssignToScene(Scene.Global);
            game.DebugInfoChanged += UpdateDebugInfo;
        }

        #endregion

        #region Menu Logic

        public static void ToggleMainMenu()
        {
            var gameStateInfo = string.Empty;
            if (GameLevels.ScoreLevelOne.Current < 0)
            {
                GameLevels.ScoreLevelOne.Current = 0;
                gameStateInfo = "                            You LOST!\n";
            }

            var shipsLeft = GameLevels.ScoreLevelOne.Target - GameLevels.ScoreLevelOne.Current;

            gameStateInfo += string.Format("Shoot all {0} ships to finish the level.",
                                              GameLevels.ScoreLevelOne.Target);
            switch (shipsLeft)
            {
                case 0:
                    gameStateInfo += " CONGRATULATIONS! You got them all";
                    break;
                case 1:
                    gameStateInfo += " There is only 1 ship left.";
                    break;
                default:
                    gameStateInfo += string.Format(" There are {0} ships left.", shipsLeft);
                    break;
            }

            var mainMenu = Component.GetAll<UserInterface>().FirstOrDefault(ui => ui.Name == "mainMenu");
            var root = mainMenu.RootElement;

            ((UILabel)root.GetChildByTitle("gameState")).Text = gameStateInfo;

            root.Visible = !root.Visible;
            root.Enabled = !root.Enabled;

            GameEntities.EnableInputScripts(Entity.GetInstance(GameEntities.PlayerId), !root.Visible);

            VitalGameMethods.IsPaused = root.Visible;
            VitalGameMethods.IsMouseLocked = !root.Visible;
            VitalGameMethods.IsMouseVisible = root.Visible;
        }

        public static void ToggleOptionsMenu()
        {
            var mainMenu = Component.GetAll<UserInterface>().FirstOrDefault(ui => ui.Name == "mainMenu");
            var root = mainMenu.RootElement;
            root.Visible = !root.Visible;
            root.Enabled = !root.Enabled;

            var optionsMenu = Component.GetAll<UserInterface>().FirstOrDefault(ui => ui.Name == "optionsMenu");
            root = optionsMenu.RootElement;
            root.Visible = !root.Visible;
            root.Enabled = !root.Enabled;
        }

        public static void ToggleDebugInfo()
        {
            var debugInfo = Component.GetAll<UserInterface>().FirstOrDefault(ui => ui.Name == "debugInfo");
            var root = debugInfo.RootElement;
            root.Visible = !root.Visible;
        }

        private static void UpdateDebugInfo(object sender, EventArgs e)
        {
            var debugInfo = Component.GetAll<UserInterface>().FirstOrDefault(ui => ui.Name == "debugInfo");
            ((UILabel)debugInfo.RootElement.Children.ElementAt(0)).Text = ((Sol2EGame)sender).DebugInfo;
        }

        #endregion

        #region Helper Methods

        private static UIButton CreateMenuButton(Rectangle buttonRect, string fontName, string text)
        {
            return new UIButton(buttonRect, fontName)
            {
                HightlightedForegrundColor = Color.White,
                ForegroundColor = Color.Gray,
                Title = text + Sol2E.Utils.IDPool.GetInstance(typeof(UIButton)).GetNextAvailableID(),
                Text = text
            };
        }

        private static UIElement CreateOnOffSwitch(Rectangle rect, string fontName, string text, out UIButton btnOn, out UIButton btnOff)
        {
            UIButton dummy;
            return CreateTripleOption(rect, fontName, text, "On", "Off", string.Empty, out btnOn, out btnOff, out dummy);
        }

        private static UIElement CreateTripleOption(Rectangle rect, string fontName, string labelText,
            string textOptionOne, string textOptionTwo, string textOptionThree,
            out UIButton btnOne, out UIButton btnTwo, out UIButton btnThree)
        {
            int halfWidth = rect.Width / 2;
            var label = new UILabel(new Rectangle(0, 0, halfWidth, rect.Height), fontName) { Text = labelText };

            int btnDist = halfWidth / 3;
            int btnWidth = (int)(btnDist * 0.9f);

            btnOne   = CreateMenuButton(new Rectangle(rect.Width - btnDist * 3, 0, btnWidth, rect.Height), fontName, textOptionOne);
            btnTwo   = CreateMenuButton(new Rectangle(rect.Width - btnDist * 2, 0, btnWidth, rect.Height), fontName, textOptionTwo);
            btnThree = CreateMenuButton(new Rectangle(rect.Width - btnDist * 1, 0, btnWidth, rect.Height), fontName, textOptionThree);

            var container = new UIElement(rect);
            container.AddChildElemet(label);
            if (btnOne.Text != string.Empty) container.AddChildElemet(btnOne);
            if (btnTwo.Text != string.Empty) container.AddChildElemet(btnTwo);
            if (btnThree.Text != string.Empty) container.AddChildElemet(btnThree);

            return container;
        }

        #endregion
    }
}
