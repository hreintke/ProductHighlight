using Mafi;
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

namespace ProductHighlight.UI
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class HighlightController : BaseWindowController<HighlightWindow>, IToolbarItemInputController
    {
        private readonly ToolbarController _toolbarController;

        public HighlightController(IUnityInputMgr inputManager, IGameLoopEvents gameLoop, HighlightWindow window, ToolbarController toolbarController) : base(inputManager, gameLoop, window)
        {
            _toolbarController = toolbarController;
        }

        public override void RegisterUi(UiBuilder builder)
        {
            _toolbarController.AddMainMenuButton("PHL_Toolbar", this, "unknown.png", 1338f, _ => KeyBindings.FromKey(KbCategory.Tools, KeyCode.F7));
            base.RegisterUi(builder);
        }

        public bool IsVisible => true;
        public bool DeactivateShortcutsIfNotVisible => false;
        public event Action<IToolbarItemInputController> VisibilityChanged;
    }
}
