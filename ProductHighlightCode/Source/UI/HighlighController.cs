using Mafi;
using Mafi.Core;
using Mafi.Core.GameLoop;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using System.Diagnostics;
using Mafi.Core.Buildings.Towers;
using Mafi.Unity.Mine;
using Mafi.Unity.UiToolkit.Library;
using Mafi.Unity.Ui.Hud.Toolbar;
using Mafi.Unity.Ui.Research;

namespace ProductHighlight;

[GlobalDependency(RegistrationMode.AsEverything, false, false)]
public class HighlightController :
      WindowController<HighlightWindow>,
      IUnityInputController
{
    private readonly KeyBindings WindowKey = KeyBindings.FromKey(KbCategory.General, ShortcutMode.Game, KeyCode.Backslash);

    public HighlightController(IUnityInputMgr inputManager, ControllerContext controllerContext)
        : base(controllerContext)
    {
        inputManager.RegisterGlobalShortcut((Func<ShortcutsManager, KeyBindings>)(m => { return WindowKey; }), this);
        LogWrite.Info("Backslash registered");
    }
}
