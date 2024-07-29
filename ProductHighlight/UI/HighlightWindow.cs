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
using UnityEngine;
using Mafi.Unity;
using static Mafi.Unity.Assets.Unity.Generated.Icons;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core.Utils;
using Mafi.Unity.InputControl.RecipesBook;
using Mafi.Collections;
using System.Collections;
using ProductHighlight.Logging;
using static ProductHighlight.Actions.Highlight;
using Mafi.Unity.InputControl;


namespace ProductHighlight.UI
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class HighlightWindow : WindowView
    {
        private class EntityTypeView : IUiElement
        {
            private StackContainer stackContainer;
            private IconContainer iconContainer;
            private Txt typeName;
            private Txt typeCount;
            private Btn nextButton;
            private Btn previousButton;
            private Panel typePanel;
            private EntityType et;
            private Highlight highlight;

            public GameObject GameObject => typePanel.GameObject;

            public RectTransform RectTransform => typePanel.RectTransform;

            public EntityTypeView(
                  IUiElement parent,
                  UiBuilder builder,
                  Highlight.EntityType entityType,
                  Highlight hl)
            {
                typePanel = builder.NewPanel("typePanel");
                typeName = builder.NewTxt("typeName").SetFontSize(17).SetAlignment(TextAnchor.MiddleLeft).SetText(entityType.ToString());
                typeName.PutToLeftTopOf(typePanel, new Vector2(100f, 30f), Offset.Top(10f) + Offset.Left(10f));

                typeCount = builder.NewTxt("typeCount").SetFontSize(17).SetText("cnt").SetAlignment(TextAnchor.MiddleRight);
                typeCount.PutToLeftTopOf(typePanel, new Vector2(30f, 30f), Offset.Top(10f) + Offset.Left(100));

                nextButton = builder.NewBtnPrimary("viewNext").SetText("Previous");
                nextButton.PutToLeftTopOf(typePanel, new Vector2(75f, 30f), Offset.Top(7f) + Offset.Left(150));
                nextButton.OnClick(() => highlight.panToEntity(entityType, false));

                previousButton = builder.NewBtnPrimary("viewNext").SetText("Next");
                previousButton.PutToLeftTopOf(typePanel, new Vector2(75f, 30f), Offset.Top(7f) + Offset.Left(230));
                previousButton.OnClick(() => highlight.panToEntity(entityType, true));

                et = entityType;
                highlight = hl;

            }
            public void setValue()
            {
                typeCount.SetText(highlight.GetEntityTypeElement(et).listCount().ToString());
            }
        }
        private TxtField searchBox;
        private Txt pname;
        public Btn phlButton;
        private Btn phlClearButton;
        private Btn phlUsage;
        private EntityTypeView entityTypeviewStorage;
        private EntityTypeView entityTypeviewProducer;
        private EntityTypeView entityTypeviewConsumer;
        private EntityTypeView entityTypeviewTransport;
        private Btn showNextStorage;
        public ProductProto selectedProduct;
        private IOrderedEnumerable<ProductProto> sortedProductProtos;
        private readonly ProtosDb _protosDb;
        private readonly  Highlight _highlight;
        UnlockedProtosDbForUi unlockedProtosDb;

        private IconContainer ic;
        private readonly Dict<ProductProto, Btn> allProductButtons = new Dict<ProductProto, Btn>();

        public HighlightWindow(ProtosDb db, Highlight highlight, UnlockedProtosDbForUi ulProtoDb) : base("HighlightWindow")
        {
            _protosDb = db;
            _highlight = highlight;
            sortedProductProtos = _protosDb.Filter<ProductProto>(pp => true).OrderBy(x => x.Strings.Name.TranslatedString);
            selectedProduct = sortedProductProtos.ElementAt(0);
            unlockedProtosDb = ulProtoDb;
        }

        private readonly Set<Proto> m_protosFound = new Set<Proto>();

        protected override void BuildWindowContent()
        {
            SetTitle("Product highlight");
            SetContentSize(600f, 300f);
            PositionSelfToCenter();
            MakeMovable();

            searchBox = Builder.NewTxtField("Search")//, (IUiElement)this.GetContentPanel()).SetStyle(this.Builder.Style.Global.LightTxtFieldStyle).SetPlaceholderText(Tr.Search).SetCharLimit(30).PutToLeftTopOf<TxtField>((IUiElement)this.GetContentPanel(), new Vector2((float)(100 - 20), 30f), Offset.Top(10f) + Offset.Left(10f));
                .SetPlaceholderText("Search")
                .SetCharLimit(30)
                .SetStyle(Builder.Style.Global.LightTxtFieldStyle)
                .PutToLeftTopOf<TxtField>((IUiElement)GetContentPanel(), new Vector2(200f, 30f), Offset.Top(10f) + Offset.Left(10f));


            ScrollableContainer buttonScrollContainer = Builder.NewScrollableContainer("ScrollableTitles")
                .AddVerticalScrollbar()
                .PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(200f, 250f), Offset.Top(50f) + Offset.Left(10f));

            var productButtonContainer = Builder
                .NewStackContainer("PHL Top")
                .SetStackingDirection(StackContainer.Direction.TopToBottom)
                .SetSizeMode(StackContainer.SizeMode.Dynamic)
                .SetSize(new Vector2(175f, 100f))
                .SetItemSpacing(3f);

            searchBox.SetDelayedOnEditEndListener(new Action<string>(search));

            foreach (var product in sortedProductProtos)
            {
                string iconPath = product.IconPath;
                Vector2? nullable = new Vector2?(16.Vector2());
                ColorRgba? color = new ColorRgba?();
                Vector2? size = nullable;
                IconStyle iconStyle = new IconStyle(iconPath, color, size);

                var productButton = Builder
                    .NewBtnPrimary("PHL Usage Button")
                    .SetButtonStyle(Builder.Style.Global.ListMenuBtn)
                    .SetText(product.Strings.Name.TranslatedString)
                    .SetTextAlignment(TextAnchor.MiddleLeft)
                    .OnClick((Action)(() => onClick(product)))
                    .SetIcon(iconStyle)
                    .SetSize(new Vector2(175f, 30f));
                productButton.SetBackgroundColor(ColorRgba.Black);
                productButton.SetOnMouseEnterLeaveActions(() => productButton.SetBackgroundColor(ColorRgba.DarkDarkGray), () => productButton.SetBackgroundColor(ColorRgba.DarkGray));

                productButton.AppendTo(productButtonContainer);
                allProductButtons[product] = productButton;
            }

            ic = Builder.NewIconContainer("ProductIcon");
            ic.PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(40f, 40f), Offset.Top(10f) + Offset.Left(250f));
            pname = Builder.NewTxt("type").SetTextStyle(Builder.Style.Global.BoldText).SetFontSize(17);
            pname.PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(150f, 70f), Offset.Top(20f) + Offset.Left(300f));

            phlClearButton = Builder.NewBtnPrimary("test").SetText("Clear");
            phlClearButton.OnClick( () => onClear());
            phlClearButton.PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(100f, 30f), Offset.Top(10f) + Offset.Left(460f));


            

            entityTypeviewStorage = new EntityTypeView(this, Builder, Highlight.EntityType.Storage, _highlight);
            entityTypeviewStorage.PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(100f, 100f), Offset.Top(100f) + Offset.Left(250f));

            entityTypeviewProducer = new EntityTypeView(this, Builder, Highlight.EntityType.Producer, _highlight);
            entityTypeviewProducer.PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(100f, 100f), Offset.Top(150f) + Offset.Left(250f));

            entityTypeviewConsumer = new EntityTypeView(this, Builder, Highlight.EntityType.Consumer, _highlight);
            entityTypeviewConsumer.PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(100f, 100f), Offset.Top(200f) + Offset.Left(250f));

            entityTypeviewTransport = new EntityTypeView(this, Builder, Highlight.EntityType.Transport, _highlight);
            entityTypeviewTransport.PutToLeftTopOf((IUiElement)GetContentPanel(), new Vector2(100f, 100f), Offset.Top(250f) + Offset.Left(250f));

            void onClear()
            {
                _highlight.clearHighlights();
                searchBox.SetText("");
                search("");
                pname.SetText("None selected");
                ic.SetIcon("");
                ic.SetVisibility(false);
                entityTypeviewStorage.setValue();
                entityTypeviewProducer.setValue();
                entityTypeviewConsumer.setValue();
                entityTypeviewTransport.setValue();
            }

            void onClick(ProductProto product)
            {
                selectedProduct = product;
                _highlight.clearHighlights();
                _highlight.highlightUsage(selectedProduct);
                ic.SetIcon(selectedProduct.IconPath);
                ic.SetVisibility(true);
                pname.SetText(product.Strings.Name.TranslatedString);
                entityTypeviewStorage.setValue();
                entityTypeviewProducer.setValue();
                entityTypeviewConsumer.setValue();
                entityTypeviewTransport.setValue();

            }
            onClear();

            


 

        void search(string text)
        {
            productButtonContainer.StartBatchOperation();

            

            UiSearchUtils.MatchProtos<Proto>(text, (IIndexable<Proto>)sortedProductProtos.ToLyst<Proto>(), m_protosFound);

            foreach (var item in allProductButtons)
            {
                productButtonContainer.SetItemVisibility(item.Value, m_protosFound.Contains(item.Key) && item.Key.IsAvailable && unlockedProtosDb.IsUnlocked((IProto)item.Key));
            }
            productButtonContainer.FinishBatchOperation();
        }


            buttonScrollContainer.AddItemTop((IUiElement)productButtonContainer);
            buttonScrollContainer.SetContentToScroll((IUiElement)productButtonContainer);


        }
    }
}
