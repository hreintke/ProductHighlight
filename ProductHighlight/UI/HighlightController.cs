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
using System.Diagnostics;
using Mafi.Core.Buildings.Towers;
using Mafi.Unity.Mine;

namespace ProductHighlight.UI
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class HighlightController : BaseWindowController<HighlightWindow>
    {
        private readonly ToolbarController _toolbarController;
        private readonly KeyBindings WindowKey = KeyBindings.FromKey(KbCategory.General,ShortcutMode.Game, KeyCode.F7);
        private bool windowOpen = false;
        private ShortcutsManager _shortcutsManager;
        private Stopwatch fpsTimer;
        private HighlightWindow _window;

        public HighlightController(
            IUnityInputMgr inputManager, 
            IGameLoopEvents gameLoop,
            HighlightWindow window,
            ShortcutsManager shortcutsManager,
            UiBuilder builder) 
                    : base(inputManager, gameLoop, builder, window)
        {
            _shortcutsManager = shortcutsManager;
            _window = window;
            fpsTimer = new Stopwatch();
            fpsTimer.Start();
            inputManager.RegisterGlobalShortcut((Func<ShortcutsManager, KeyBindings>)(m => { return WindowKey; }), this);
        }

        public override void Activate()
        {
            windowOpen = true;

            base.Activate();
            _window.Show();
        }

        public override void Deactivate()
        {
            windowOpen = false;
            _window.Hide();
            base.Deactivate();
        }

 //       public bool IsVisible => true;
//        public bool DeactivateShortcutsIfNotVisible => false;
 //       public event Action<IToolbarItemInputController> VisibilityChanged;
    }
}
