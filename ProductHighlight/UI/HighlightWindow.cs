using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UiFramework;
using Mafi.Unity.UserInterface;
using Mafi.Unity.UserInterface.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Core.Prototypes;
using Mafi.Core.Products;
using Mafi;
using ProductHighlight.Actions;


namespace ProductHighlight.UI
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class HighlightWindow : WindowView
    {
        private Btn phlButton;
        private Btn phlClearButton;
        private Dropdwn phlDropdown;
        public static string selectedProduct;
        private List<String> productList;
        private readonly ProtosDb _protosDb;
        private readonly  Highlight _highlight;

        public HighlightWindow(ProtosDb db, Highlight highlight) : base("HighlightWindow")
        {
            _protosDb = db;
            _highlight = highlight;
            productList = _protosDb.Filter<ProductProto>(_ => true).Select(x => x.Id.ToString().Replace("Product_", "")).OrderBy(x => x).ToList();
            selectedProduct = productList.ElementAt(0);
        }

        protected override void BuildWindowContent()
        {
            SetTitle("Product highlight");
            SetContentSize(680f, 400f);
            PositionSelfToCenter();
            MakeMovable();

            StackContainer stackContainer = Builder
                .NewStackContainer("PHL Stack")
                .SetStackingDirection(StackContainer.Direction.TopToBottom)
                .SetSizeMode(StackContainer.SizeMode.Dynamic)
                .SetInnerPadding(Offset.All(15f))
                .SetItemSpacing(15f)
                .PutToTopOf(this, 0.0f);

            phlButton = Builder
                .NewBtnGeneral("PHL Highlight Button")
                .SetButtonStyle(Style.Global.GeneralBtn)
                .SetText("Highlight Selected")
                .OnClick(() => _highlight.highlightAll(selectedProduct))
                .AddToolTip("Highlight Machines and Storages for the selected product");
            phlButton.AppendTo(stackContainer);

            phlClearButton = Builder
                .NewBtnGeneral("PHL Clear Button")
                .SetButtonStyle(Style.Global.GeneralBtn)
                .SetText("Clear Highlights")
                .OnClick(() => _highlight.clearHighlights())
                .AddToolTip("Clear all highlights");
            phlClearButton.AppendTo(stackContainer);

            phlDropdown = Builder
                .NewDropdown("COI Dropdown")
                .AddOptions(productList)
                .OnValueChange(i => selectedProduct = productList.ElementAt(i));
            phlDropdown.AppendTo(stackContainer);
        }
    }
}
