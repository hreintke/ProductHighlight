using Mafi;
using Mafi.Localization;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace ProductHighlight;

public class EntityTypeView : Row
{
    Label typeName;
    Label typeCount;
    ButtonText nextButton;
    Button prevButton;
    HighlightWindow highlightWindow;
    EntityType entityType;

    public EntityTypeView(EntityType et, HighlightWindow hw)
    {
        entityType = et;
        highlightWindow = hw;
        this.Margin(20.px());
        //this.MarginBottom(5.px());
        this.Gap(20.px());

        typeName = new Label(new Mafi.Localization.LocStrFormatted(et.ToString()));
        typeCount = new Label(new LocStrFormatted("-"));
        nextButton = new ButtonText("Next".AsLoc(), () => { hw.panToEntity(entityType, false); });
        prevButton = new ButtonText("Previous".AsLoc(), () => { hw.panToEntity(entityType, true); });

        typeName.Height<Label>(20.px());
        typeName.Width<Label>(150.px());
        typeCount.Height<Label>(20.px());
        typeCount.Width<Label>(100.px());
        nextButton.Width(100.px());
        prevButton.Width(100.px());


        this.Add(typeName);
        this.Add(typeCount);
        this.Add(nextButton);
        this.Add(prevButton);
    }

    public void setValue()
    {
        typeCount.Value<Label>(highlightWindow.getEntityCount(entityType).ToString().AsLoc());
    }



}
