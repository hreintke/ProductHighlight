using Mafi;
using Mafi.Base.Prototypes.Trains;
using Mafi.Collections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.FuelStations;
using Mafi.Core.Buildings.Settlements;
using Mafi.Core.Buildings.Shipyard;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Factory.Transports;
using Mafi.Core.Input;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain.Resources;
using Mafi.Core.Trains;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.Trucks;
using Mafi.Unity.Camera;
using Mafi.Unity.Entities;
using Mafi.Unity.InputControl.ResVis;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using static Mafi.Base.Assets.Base.Buildings;
using static Mafi.Core.Trains.CargoWagon;

namespace ProductHighlight;

public enum EntityType
{
    Storage,
    Consumer,
    Producer,
    Transport,
    Vehicle,
    NeedBuild
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
        {EntityType.NeedBuild, ColorRgba.Blue},
    };


    private readonly EntitiesManager entitiesManager;
    private readonly IVehiclesManager vehiclesManager;
    private readonly ProductsSlimIdManager productSlimIdManager;
    private readonly Mafi.NewInstanceOf<EntityHighlighter> entityHighlighter;
    private readonly InputScheduler inputScheduler;
    private CameraController cameraController;
    private readonly ResVisBarsRenderer resVisBarsRenderer;
    private readonly ResVisBarsRenderer.Activator resActivator;
    private readonly TerrainResourcesProvider terrainResourcesProvider;

    public ProductInfo currentProductInfo;

    public ProductHighlightManager(
                    EntitiesManager entitiesM,
                    IVehiclesManager vehiclesM,
                    ProductsSlimIdManager productSlimIdM,
                    Mafi.NewInstanceOf<EntityHighlighter> entityHighlight,
                    InputScheduler inputSchedule,
                    CameraController cameraControl,
                    ProtosDb db,
                    ResVisBarsRenderer rvbr,
                    TerrainResourcesProvider trp)
    {
        entitiesManager = entitiesM;
        vehiclesManager = vehiclesM;
        productSlimIdManager = productSlimIdM;
        entityHighlighter = entityHighlight;
        inputScheduler = inputSchedule;
        cameraController = cameraControl;
        resVisBarsRenderer = rvbr;
        resActivator = resVisBarsRenderer.CreateActivator();
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

    public Quantity getNeeded()
    {
        return currentProductInfo.totalNeedForBuild;
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
        Dict<Type, int> entityCount = new Dict<Type, int>();

        reset();

        foreach (Entity e in entitiesManager.Entities)
        {

            if (e is IStaticEntity iStatic)
            {
                if ((iStatic.ConstructionState == ConstructionState.NotInitialized) || 
                        (iStatic.ConstructionState == ConstructionState.InConstruction) ||
                        (iStatic.ConstructionState == ConstructionState.BeingUpgraded))
                {
                    if (iStatic.ConstructionProgress.HasValue)
                    {
                        addConstruction(e.Id, iStatic.ConstructionProgress.Value, productProto);
                        IConstructionProgress cp = iStatic.ConstructionProgress.Value;
                    }
                }
            }

            if (e is Mafi.Core.Buildings.Shipyard.Shipyard sy)
            {
                if (sy.RepairProgress.HasValue)
                {
                    addConstruction(e.Id, sy.RepairProgress.Value, productProto);
                }
                if  (sy.ModificationProgress.HasValue)
                {
                    addConstruction(e.Id, sy.ModificationProgress.Value, productProto);
                }
            }

            if (entityCount.ContainsKey(e.GetType()))
            {
                entityCount[e.GetType()]++;
            }
            else
            {
                entityCount[e.GetType()] = 1;
            }

            switch (e)
            {
                default:
                    break;
                case Machine machine :
                    addMachine(machine, productProto);
                    break;
                case Farm farm :
                    addFarm(farm, productProto);
                    break;
                case AnimalFarm animalFarm :
                    addAnimalFarm(animalFarm, productProto);
                    break;
                case SettlementFoodModule settlementFoodModule:
                    addSettlementFoodModule(settlementFoodModule, productProto);
                    break;
                case SettlementServiceModule settlementServiceModule:
                    addSettlementServiceModule(settlementServiceModule, productProto);
                    break;
                case Hospital hospital :
                    addHospital(hospital, productProto);
                    break;
                case StorageBase storage:
                    addStorage(storage, productProto);
                    break;
                case Transport transport :
                    addTransport(transport, productProto);
                    break;
                case Truck truck:
                    addTruck(truck, productProto);
                    break;
                case CargoWagon cargoWagon :
                    addCargoWagon(cargoWagon, productProto);
                    break;
                case Excavator excavator :
                    addExcavator(excavator, productProto);
                    break;
                case TrainStationModule trainStationModule :
                    addStationModule((TrainStationModule)e, productProto);
                    break;
                case TrainStationFuel trainStationFuel :
                    addtrainFuelStation(trainStationFuel, productProto);
                    break;
            };
        }
    }

    public void addConstruction(EntityId id, IConstructionProgress cp, ProductProto productProto)
    {
        foreach (var p in cp.Buffers)
        {
            if (p.Product == productProto)
            {
                currentProductInfo.addEntity(EntityType.NeedBuild, id);
                currentProductInfo.addNeeded(cp.GetMissingQuantityFor(p.Product));
            }
        }
    }

    public void addMachine(Machine machine, ProductProto productProto)
    {
        bool producedFound = false;
        bool consumedFound = false;
        foreach (RecipeProto r in machine.RecipesAssigned)
        {
            if (r is IRecipeForUi)
            {
                foreach (var ro in r.AllUserVisibleOutputs)
                {
                    if (ro.Product == productProto && !producedFound)
                    {
                        currentProductInfo.addEntity(EntityType.Producer, machine.Id);
                        Fix32 v = 600 / r.Duration.Ticks;
                        Fix32 f = ro.Quantity.Value * v;
                        Quantity q = new Quantity(f.IntegerPart);
                        currentProductInfo.addProduced(q);
                        producedFound = true;
                        break;
                    }
                }
                foreach (var ri in r.AllUserVisibleInputs)
                {
                    if (ri.Product == productProto && !consumedFound)
                    {
                        currentProductInfo.addEntity(EntityType.Consumer, machine.Id);
                         Fix32  v = 600 / r.Duration.Ticks;
                        Fix32 f = ri.Quantity.Value * v;
                        Quantity q = new Quantity(f.IntegerPart);
                        currentProductInfo.addConsumed(q);
                        consumedFound = true;
                        break;
                    }
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
        
        FertilizerProductParam paramValue;
        if (productProto.TryGetParam<FertilizerProductParam>(out paramValue))
        {
            currentProductInfo.addEntity(EntityType.Consumer, farm.Id);
        }

        if (farm.Prototype.WaterCollectedPerDay.Product == productProto)
        {
            currentProductInfo.addEntity(EntityType.Consumer, farm.Id);
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

    public void addSettlementFoodModule(SettlementFoodModule foodModule, ProductProto productProto)
    {
        foreach (var buffer in foodModule.BuffersPerSlot)
        {
            if ((buffer.HasValue) && (buffer.Value.Product == productProto))
            {
                currentProductInfo.addEntity(EntityType.Storage, foodModule.Id);
            }
        }
    }

    public void addSettlementServiceModule(SettlementServiceModule serviceModule, ProductProto productProto)
    {
        if (serviceModule.Prototype.InputProduct == productProto)
        {
            currentProductInfo.addEntity(EntityType.Storage, serviceModule.Id);
        }
    }

    public void addHospital(Hospital hospital, ProductProto productProto)
    {
        foreach (var buffer in hospital.BuffersPerSlot)
        {
            if ((buffer.HasValue) && (buffer.Value.Product == productProto))
            {
                currentProductInfo.addEntity(EntityType.Storage, hospital.Id);
            }
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
                currentProductInfo.addEntity(EntityType.Vehicle, excavator.Id);
                currentProductInfo.addTransportInUse(cargoEnumerator.Current.Value);
            }
        }
    }

    public void addCargoWagon(CargoWagon cargoWagon, ProductProto productProto)
    {
        foreach(SubCargoWagon subcar in cargoWagon.SubCars)
        {
            
            if ((subcar.OnlyAllowedProduct.HasValue) && (subcar.OnlyAllowedProduct.Value == productProto) || (subcar.Cargo.Product == productProto))
            {
                currentProductInfo.addEntity(EntityType.Transport, cargoWagon.Id);
                break;
            }
        }
    }

    public void addStationModule(TrainStationModule trainStationModule, ProductProto productProto)
    {
        if ((trainStationModule.Buffer.HasValue) && trainStationModule.Buffer.Value.Product == productProto)
        {
            currentProductInfo.addEntity(EntityType.Storage, trainStationModule.Id);
        }
    }

    public void addtrainFuelStation(TrainStationFuel trainStationFuel, ProductProto productProto)
    {
        if ((trainStationFuel.Prototype.PrimaryProduct.Product == productProto) ||
            ((trainStationFuel.Prototype.SecondaryProduct.HasValue) && (trainStationFuel.Prototype.SecondaryProduct.Value.Product == productProto)))
        {
            currentProductInfo.addEntity(EntityType.Consumer, trainStationFuel.Id);
        }
    }
}
