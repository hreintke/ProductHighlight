using Mafi;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Utils;
using Mafi.Core;
using Mafi.Localization;
using Mafi.Unity.Ui.Statistics;
using Mafi.Unity.UiToolkit.Library;
using System.Collections.Generic;
using System;
using Mafi.Collections;
using Mafi.Core.PropertiesDb;
using Mafi.Core.Entities;
using Mafi.Unity.Camera;
using Mafi.Unity.Entities;
using Mafi.Unity.InputControl;
using ProductHighlight;
using System.Linq;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Factory.Transports;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using Mafi.Unity.UiToolkit.Library.FloatingPanel;



public class HighlightWindow : Window
{
    private readonly Lyst<ProductProto> allProducts;

    public ProductProto selectedProduct;
    private Option<ProductProto> currentProduct = Option.None;
    private IOrderedEnumerable<ProductProto> sortedProductProtos;
    private readonly ProtosDb protosDb;
    private readonly ProductHighlightManager highlightManager;
    UnlockedProtosDbForUi unlockedProtosDb;
    private readonly EntitiesManager entitiesManager;
    private readonly Mafi.NewInstanceOf<EntityHighlighter> entityHighlighter;
    private CameraController cameraController;
    private Row productRow = new Row(5);
    private Panel productPanel = new Panel();
    private Panel overviewPanel = new Panel();

    private SingleProductPickerUi si;

    private Label productLabel = new Label(new LocStrFormatted("productLabel"));
    private ButtonText clearButton = new ButtonText("Clear".AsLoc());

    private EntityTypeView entityTypeviewStorage;
    private EntityTypeView entityTypeviewProducer;
    private EntityTypeView entityTypeviewConsumer;
    private EntityTypeView entityTypeviewTransport;

    private readonly Set<Proto> protosFound = new Set<Proto>();

    public HighlightWindow(
        ProtosDb db,
        ProductHighlightManager highlightM,
        UnlockedProtosDbForUi ulProtoDb,
        EntitiesManager eManager,
        Mafi.NewInstanceOf<EntityHighlighter> entityHighlight,
        CameraController cameraControl)
         : base(new LocStrFormatted("ProductHighlight"))
    {
        productPanel.Height(100);
        productPanel.AlignSelfStretch();


        productRow.MarginLeft<Row>(30);
        this.WindowSize(new Px(600), new Px(400));
        this.MakeMovable();
        
        productLabel.Value<Label>("product".AsLoc());
        productLabel.Width(200);

        protosDb = db;
        highlightManager = highlightM;
        sortedProductProtos = protosDb.Filter<ProductProto>(pp => true).OrderBy(x => x.Strings.Name.TranslatedString);
        selectedProduct = sortedProductProtos.ElementAt(0);
        unlockedProtosDb = ulProtoDb;
        entityHighlighter = entityHighlight;
        cameraController = cameraControl;
        entitiesManager = eManager;
        Option<ProductProto> p = (ProductProto)protosDb.Get(IdsCore.Products.Recyclables).Value; 
        si = new SingleProductPickerUi(sortedProductProtos.ToLyst, onClick, gp,primaryButtonIfNoProtoSet: true);
        productRow.Add(si);
        productRow.Add(productLabel);
        clearButton.Width(100.px());
        clearButton.OnClick(onClear);
        productRow.Add(clearButton);
        productPanel.Add(productRow);
        this.Body.Add(productPanel);
 
        entityTypeviewStorage = new EntityTypeView(EntityType.Storage, this);
        overviewPanel.Add(entityTypeviewStorage);
        entityTypeviewTransport= new EntityTypeView(EntityType.Transport, this);
        overviewPanel.Add(entityTypeviewTransport);
        entityTypeviewConsumer = new EntityTypeView(EntityType.Consumer, this);
        overviewPanel.Add(entityTypeviewConsumer);
        entityTypeviewProducer = new EntityTypeView(EntityType.Producer, this);
        overviewPanel.Add(entityTypeviewProducer);

        this.Body.Add(overviewPanel);
    }
    void onProductSelected(ProductProto product)
    {
        currentProduct = product;
        productLabel.Value<Label>(product.Strings.Name.TranslatedString.AsLoc());
    }

    Option<ProductProto> gp()
    {
        if (currentProduct.HasValue)
        {
            return currentProduct.Value;
        }
        return Option.None;
         
    }

    public void panToEntity(EntityType et, bool next)
    {
        EntityId panEntity = highlightManager.getNextEntity(et, next);

        if (!(panEntity == EntityId.Invalid))
        {

            if (entitiesManager.TryGetEntity(panEntity, out Entity entityOut))
            {
                if (entityOut is LayoutEntity layoutEntity)
                {
                    cameraController.PanTo(new Tile2f(layoutEntity.Transform.Position.X, layoutEntity.Transform.Position.Y));
                }
                else if (entityOut is Vehicle vehicle)
                {
                    cameraController.PanTo(vehicle.Position2f);
                }
                else if (entityOut is Transport transport)
                {
                    cameraController.PanTo(transport.Position2f);
                }
            }
        }
    }

    public int getEntityCount(EntityType entityType)
    {
        return highlightManager.getEntityCount(entityType);
    }

    public void clearHighlights()
    {
        entityHighlighter.Instance.ClearAllHighlights();
    }

    public void highlightUsage()
    {
        entityHighlighter.Instance.ClearAllHighlights();
        foreach (EntityType eType in Enum.GetValues(typeof(EntityType)))
        {
            foreach (var e in highlightManager.currentProductInfo.entityList(eType).getLyst)
            {
                if (entitiesManager.TryGetEntity(e, out IRenderedEntity entity))
                {
                    entityHighlighter.Instance.Highlight(entity, highlightManager.ColorScheme[eType]);
                }
            }
        }
    }
    void onClick(ProductProto product)
    {
        selectedProduct = product;
        currentProduct = product;
        productLabel.Value<Label>(product.Strings.Name.TranslatedString.AsLoc());
        highlightManager.updateProduct(selectedProduct);
        highlightUsage();

        entityTypeviewStorage.setValue();
        entityTypeviewProducer.setValue();
        entityTypeviewConsumer.setValue();
        entityTypeviewTransport.setValue();
    }

    void onClear()
    {
        clearHighlights();
        highlightManager.reset();
        productLabel.Value("None selected".AsLoc());
        currentProduct = Option.None;

        entityTypeviewStorage.setValue();
        entityTypeviewProducer.setValue();
        entityTypeviewConsumer.setValue();
        entityTypeviewTransport.setValue();
    }
}