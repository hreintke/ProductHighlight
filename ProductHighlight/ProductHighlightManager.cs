using Mafi;
using Mafi.Collections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Entities;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Factory.Transports;
using Mafi.Core.Input;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.Trucks;
using Mafi.Unity.Camera;
using Mafi.Unity.Entities;
using ProductHighlight.Logging;
using System;
using System.CodeDom;
using System.Collections.Generic;
using static Mafi.Base.Assets.Base.Buildings;

namespace ProductHighlight;


public enum EntityType
{
    Storage,
    Consumer,
    Producer,
    Transport,
    Vehicle
};

[GlobalDependency(RegistrationMode.AsSelf, false)]
public class ProductHighlightManager
{

    public Dict<EntityType, ColorRgba> ColorScheme = new Dict<EntityType, ColorRgba>
    {{EntityType.Storage,ColorRgba.Magenta},
        {EntityType.Consumer, ColorRgba.Yellow},
        {EntityType.Producer, ColorRgba.White},
        {EntityType.Transport, ColorRgba.Green},
        {EntityType.Vehicle, ColorRgba.Red},
    };


    private readonly EntitiesManager entitiesManager;
    private readonly IVehiclesManager vehiclesManager;
    private readonly ProductsSlimIdManager productSlimIdManager;
    private readonly Mafi.NewInstanceOf<EntityHighlighter> entityHighlighter;
    private readonly InputScheduler inputScheduler;
    private CameraController cameraController;

    public ProductInfo currentProductInfo;

    public ProductHighlightManager(
                    EntitiesManager entitiesM,
                    IVehiclesManager vehiclesM,
                    ProductsSlimIdManager productSlimIdM,
                    Mafi.NewInstanceOf<EntityHighlighter> entityHighlight,
                    InputScheduler inputSchedule,
                    CameraController cameraControl,
                    ProtosDb db)
    {
        entitiesManager = entitiesM;
        vehiclesManager = vehiclesM;
        productSlimIdManager = productSlimIdM;
        entityHighlighter = entityHighlight;
        inputScheduler = inputSchedule;
        cameraController = cameraControl;

        currentProductInfo = new ProductInfo();
    }

    public Quantity getProduce()
    {
        return currentProductInfo.totalProduced;
    }

    public Quantity getConsume()
    {
        return currentProductInfo.totalConsumed;
    }

    public int getEntityCount(EntityType entityType)
    {
        return currentProductInfo.getEntityCount(entityType);
    }

    public EntityId getNextEntity(EntityType et, bool next)
    {
        return currentProductInfo.getNextEntity(et, next);
    }

    public void reset()
    {
        currentProductInfo = new ProductInfo();
    }

    public void updateProduct(ProductProto productProto)
    {
        reset();

        Dict<Type, int> entityCount = new Dict<Type, int>();
        

        foreach (Entity e in entitiesManager.Entities)
        {
            if (entityCount.ContainsKey(e.GetType()))
            {
                entityCount[e.GetType()]++;
            }
            else
            {
                entityCount[e.GetType()] = 1;
            }
            if (e is Machine machine)
            {
                addMachine(machine, productProto);
            }
            else if (e is Farm farm)
            {
                addFarm(farm, productProto);
            }
            else if (e is AnimalFarm animaalFarm)
            {
                addAnimalFarm(animaalFarm, productProto);
            }
            else if (e is StorageBase storage)
            {
                addStorage(storage, productProto);
            }
            else if (e is Transport transport)
            {
                addTransport(transport, productProto);
            }
            else if (e is Truck truck)
            {
                addTruck(truck, productProto);
            }
            else if (e is Excavator excavator)
            {
                addExcavator(excavator, productProto);
            }

        }
        foreach(var kvp in entityCount)
        {
            LogWrite.Info($"{kvp.Key.ToString()} {kvp.Value}");
        }
    }

    public void addMachine(Machine machine, ProductProto productProto)
    {
        foreach (RecipeProto r in machine.RecipesAssigned)
        {
            foreach (RecipeOutput ro in r.AllOutputs.AsEnumerable())
            {
                if (ro.Product == productProto)
                {
                    currentProductInfo.addEntity(EntityType.Producer, machine.Id);
                    currentProductInfo.addProduced(ro.Quantity);
                }
            }
            foreach (RecipeInput ri in r.AllInputs.AsEnumerable())
            {
                if (ri.Product == productProto)
                {
                    currentProductInfo.addEntity(EntityType.Consumer, machine.Id);
                    currentProductInfo.addConsumed(ri.Quantity);
                }
            }
        }
    }

    public void addFarm(Farm farm, ProductProto productProto)
    {
        foreach (var c in farm.Schedule)
        {
            if (c != null)
            {
                if (c.Value.ProductProduced.Product == productProto)
                {
                    currentProductInfo.addEntity(EntityType.Producer, farm.Id);
                   
                    foreach(var p in farm.AvgYieldPerYear)
                    {
                        if (p.Product == productProto)
                        {
                            currentProductInfo.addProduced(p.Quantity);
                        }
                    }
                }
            }
        }
    }

    public void addAnimalFarm(AnimalFarm animalFarm, ProductProto productProto)
    {

        if ((animalFarm.Prototype.Animal == productProto) ||
                (animalFarm.Prototype.CarcassProto == productProto) ||
                (animalFarm.Prototype.ProducedPerAnimalPerMonth.Value.Product == productProto))
            
        {
            currentProductInfo.addEntity(EntityType.Producer, animalFarm.Id);
            Quantity qt = (animalFarm.Prototype.ProducedPerAnimalPerMonth.Value.Quantity.Value * animalFarm.AnimalsCount).IntegerPart.Quantity();
            currentProductInfo.addProduced(qt);
        }
    }

    public void addStorage(StorageBase storage, ProductProto productProto)
    {
        if (storage.StoredProduct != Option<ProductProto>.None)
        {
            if (storage.StoredProduct.Value == productProto)
            {
                currentProductInfo.addEntity(EntityType.Storage, storage.Id);
                currentProductInfo.addCapacity(storage.Capacity);
                currentProductInfo.addStorageInUse(storage.CurrentQuantity);
             }
        }
    }

    public void addTransport(Transport transport, ProductProto highlightProduct)
    {
        Quantity q = Quantity.Zero;
        foreach (var tp in transport.TransportedProducts.AsEnumerable())
        {
            if (tp.SlimId.ToFullOrPhantom(productSlimIdManager) == highlightProduct)
            {
                q += tp.Quantity;
            }
        }
        if (!(q == Quantity.Zero))
        {
            LogWrite.Info($"Add transport {transport.ToString()} {transport.Id.ToString()}");
            currentProductInfo.addEntity(EntityType.Transport, transport.Id);
            currentProductInfo.addTransportInUse(q);
        }
    }

    public void addTruck(Truck truck, ProductProto productProduct)
    {
        var cargoEnumerator = truck.Cargo.GetEnumerator();
        while (cargoEnumerator.MoveNext())
        {
            if (cargoEnumerator.Current.Key == productProduct)
            {
                LogWrite.Info($"Add truckt {truck.ToString()}");
                currentProductInfo.addEntity(EntityType.Vehicle, truck.Id);
                currentProductInfo.addTransportInUse(cargoEnumerator.Current.Value);
            }
        }
    }

    public void addExcavator(Excavator excavator, ProductProto productProto)
    {
        var cargoEnumerator = excavator.Cargo.GetEnumerator();
        while (cargoEnumerator.MoveNext())
        {
            if (cargoEnumerator.Current.Key == productProto)
            {
                LogWrite.Info($"Add exc {excavator.ToString()}");
                currentProductInfo.addEntity(EntityType.Vehicle, excavator.Id);
                currentProductInfo.addTransportInUse(cargoEnumerator.Current.Value);
            }
        }
    }
}
