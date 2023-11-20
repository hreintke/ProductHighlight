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
        private StackContainer stackContainer;
        public Btn phlButton;
        private Btn phlClearButton;
        private Btn phlUsage;
        private Dropdwn phlDropdown;
        public static string selectedProduct;
        private IOrderedEnumerable<ProductProto> sortedProductProtos;
        private List<String> productList;
        private readonly ProtosDb _protosDb;
        private readonly  Highlight _highlight;

        public HighlightWindow(ProtosDb db, Highlight highlight) : base("HighlightWindow")
        {
            _protosDb = db;
            _highlight = highlight;
            sortedProductProtos = _protosDb.Filter<ProductProto>(pp => pp.IsAvailable).OrderBy(x => x.Strings.Name.TranslatedString);
            productList = sortedProductProtos.Select(x => x.Strings.Name.TranslatedString).ToList();
            selectedProduct = sortedProductProtos.ElementAt(0).Id.Value;
        }

        protected override void BuildWindowContent()
        {
            SetTitle("Product highlight");
            SetContentSize(280f, 320f);
            PositionSelfToCenter();
            MakeMovable();

            stackContainer = Builder
                .NewStackContainer("PHL Stack")
                .SetStackingDirection(StackContainer.Direction.TopToBottom)
                .SetSizeMode(StackContainer.SizeMode.Dynamic)
                .SetItemSpacing(15f)
                .SetInnerPadding(new Offset(15f, 15f, 15f, 15f));

            phlButton = Builder
                .NewBtnPrimary("PHL Highlight Button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText("Machine Status")
                .OnClick(() => _highlight.highlightStatus(selectedProduct))
                .AddToolTip("Shows running status for all machines producing or consuming the selected product ");
            phlButton.AppendTo(stackContainer, 2 * phlButton.GetOptimalSize(), ContainerPosition.LeftOrTop);

            phlButton = Builder
                .NewBtnPrimary("PHL Usage Button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText("Product Usage")
                .OnClick(() => _highlight.highlightUsage(selectedProduct))
                .AddToolTip("Shows all elements where the selected product is currently configured and located");
            phlButton.AppendTo(stackContainer, 2 * phlButton.GetOptimalSize(), ContainerPosition.LeftOrTop);

            phlClearButton = Builder
                .NewBtnPrimary("PHL Clear Button")
                .SetButtonStyle(Style.Global.PrimaryBtn)
                .SetText("Clear")
                .OnClick(() => _highlight.clearHighlights())
                .AddToolTip("Clear all highlights");
            phlClearButton.AppendTo(stackContainer, 2 * phlButton.GetOptimalSize(), ContainerPosition.LeftOrTop);

            phlDropdown = Builder
                .NewDropdown("PHL Dropdown")
                .AddOptions(productList)
                .OnValueChange(i => selectedProduct = sortedProductProtos.ElementAt(i).Id.Value);
            phlDropdown.AppendTo(stackContainer);

            stackContainer.PutTo(GetContentPanel());
        }
    }
}
