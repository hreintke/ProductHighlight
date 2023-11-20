using Mafi;
using Mafi.Core;
using Mafi.Core.GameLoop;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.InputControl.Toolbar;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UserInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using ProductHighlight.Logging;

namespace ProductHighlight.UI
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class HighlightController : BaseWindowController<HighlightWindow>
    {
        private readonly ToolbarController _toolbarController;
        private readonly KeyBindings WindowKey = KeyBindings.FromKey(KbCategory.General, KeyCode.F7);
        private bool windowOpen = false;
        private ShortcutsManager _shortcutsManager;

        public HighlightController(IUnityInputMgr inputManager, IGameLoopEvents gameLoop, HighlightWindow window, ShortcutsManager shortcutsManager) 
                    : base(inputManager, gameLoop, window)
        {
            _shortcutsManager = shortcutsManager;
            gameLoop.InputUpdate.AddNonSaveable(this, myUpdate);
        }

        public void myUpdate(GameTime gameTime)
        {
            if (_shortcutsManager.IsDown(WindowKey))
            {
                if (!windowOpen)
                {
                    Activate();
                    windowOpen = true;
                }
                else
                {
                    Deactivate();
                    windowOpen = false;
                }
            }
        }

        public bool IsVisible => true;
        public bool DeactivateShortcutsIfNotVisible => false;
        public event Action<IToolbarItemInputController> VisibilityChanged;
    }
}
