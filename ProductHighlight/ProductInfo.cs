using Mafi;
using Mafi.Collections;
using Mafi.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mafi.Unity.Assets.Unity.TextMeshPro.Fonts;

namespace ProductHighlight;

public class ProductInfo
{
    Dict<EntityType, EntityList> productEntities;
    public Quantity totalProduced;
    public Quantity totalConsumed;
    Quantity storageCapacity;
    Quantity storageInUse;
    Quantity transportInUse;
    Quantity vehicleInUse;

    public ProductInfo()
    {
        productEntities = new Dict<EntityType, EntityList>()
        {
            {EntityType.Producer,  new EntityList()},
            {EntityType.Consumer,  new EntityList()},
            {EntityType.Storage,   new EntityList()},
            {EntityType.Transport, new EntityList()},
            {EntityType.Vehicle,   new EntityList()}
        };
        totalProduced = Quantity.Zero;
        totalConsumed = Quantity.Zero;
        storageCapacity = Quantity.Zero;
        storageInUse = Quantity.Zero;
        transportInUse = Quantity.Zero;
        vehicleInUse = Quantity.Zero;
    }

    public bool isProduced
    {
        get { return totalProduced != Quantity.Zero; }
    }

    public bool isConsumed
    {
        get { return totalConsumed != Quantity.Zero; }
    }

    public bool isInUse
    {
        get { return (isProduced || isConsumed); }
    }

    public EntityList entityList(EntityType entityType)
    {
        return productEntities[entityType];
    }

    public void addEntity(EntityType entityType, EntityId entityId)
    {
            productEntities[entityType].addEntity(entityId);
    }

    public void addProduced(Quantity produced)
    {
        totalProduced += produced;
    }

    public void addConsumed(Quantity consumed) 
    {
        totalConsumed += consumed;
    }

    public void addCapacity(Quantity capacity)
    {
        storageCapacity += capacity;
    }

    public void addStorageInUse(Quantity inUse)
    {
        storageInUse += inUse;
    }

    public void addTransportInUse(Quantity inUse)
    {
        transportInUse += inUse;
    }

    public void addVehicleInUse(Quantity inUse)
    {
        vehicleInUse += inUse;
    }

    public EntityId getNextEntity(EntityType et, bool next)
    {
        if (productEntities[et].listCount() == 0) 
        {
            return EntityId.Invalid;
        }
        else
        {
            return next ? productEntities[et].nextEntity() : productEntities[et].previousEntity();
        }
    }

    public int getEntityCount(EntityType entityType)
    {
        return (productEntities[entityType].listCount());
    }
}

