using Mafi;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Buildings.Farms;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Entities;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Factory.Transports;
using Mafi.Core.GameLoop;
using Mafi.Core.Input;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain.Designation;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Excavators;
using Mafi.Core.Vehicles.Trucks;
using Mafi.Unity.Camera;
using Mafi.Unity.Entities;
using Mafi.Core.Entities.Static.Layout;
using Mafi.Unity.InputControl;
using Microsoft.SqlServer.Server;
using ProductHighlight.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ProductHighlight.Extensions;
using ProductHighlight.Logging;
using Mafi.Core.Entities.Dynamic;

namespace ProductHighlight.Actions
{
    [GlobalDependency(RegistrationMode.AsSelf, false)]
    public class Highlight
    {
        public enum EntityType
        {
            Storage,
            Consumer,
            Producer,
            Transport
        };

        public class EntityTypeElement
        {
            public int currentIndex;
            public Lyst<EntityId> currentList;

            public EntityTypeElement()
            {
                reset();
            }

            public void reset()
            {
                currentIndex = -1;
                currentList = new Lyst<EntityId>();
            }

            public EntityId nextEntity()
            {
                if (currentList.Count == 0)
                {
                    return EntityId.Invalid;
                }
                currentIndex.NextModulo(currentList.Count);
                return currentList[currentIndex];
            }

            public EntityId previousEntity()
            {
                if (currentList.Count == 0)
                {
                    return EntityId.Invalid;
                }
                currentIndex.PreviousModulo(currentList.Count);
                return currentList[currentIndex];
            }

            public int listCount()
            {
                return currentList.Count;
            }
        }

        private readonly EntitiesManager _entitiesManager;
        private readonly IVehiclesManager _vehiclesManager;
        private readonly ProductsSlimIdManager _productSlimIdManager;
        private readonly Mafi.NewInstanceOf<EntityHighlighter> _entityHighlighter;
        private readonly ProtosDb _protosDb;
        private readonly InputScheduler _inputScheduler;
        private readonly TerrainMiningManager _terrainMiningManager;
        private readonly TerrainDumpingManager _terrainDumpingManager;
        private CameraController _cameraController;
        private ColorRgba colorProducer = ColorRgba.White;
        private ColorRgba colorConsumer = ColorRgba.Yellow;
        private ColorRgba colorWorking = ColorRgba.Green;
        private ColorRgba colorFail = ColorRgba.Red;
        private ColorRgba colorOther = ColorRgba.Yellow;
        private ColorRgba colorStoarge = ColorRgba.Magenta;
        private ColorRgba colorTransport = ColorRgba.Green;
        private int storageIndex = -1;

        Dict<EntityType, EntityTypeElement> currentEntities;

        public Highlight(
                            EntitiesManager entitiesManager,
                            IVehiclesManager vehiclesManager,
                            ProductsSlimIdManager productSlimIdManager,
                            Mafi.NewInstanceOf<EntityHighlighter> entityHighlighter,
                            InputScheduler inputScheduler,
                            TerrainMiningManager terrainMiningManager,
                            TerrainDumpingManager terrainDumpingManager,
                            CameraController cameraController,
                            ProtosDb db)
        {
           
            _entitiesManager = entitiesManager;
            _vehiclesManager = vehiclesManager;
            _productSlimIdManager = productSlimIdManager;
            _entityHighlighter = entityHighlighter;
            _inputScheduler = inputScheduler;
            _terrainDumpingManager = terrainDumpingManager;
            _terrainMiningManager = terrainMiningManager;
            _cameraController = cameraController;
            _protosDb = db;

            clearEntityDict();
        }

        public void clearEntityDict()
        {
            currentEntities = new Dict<EntityType, EntityTypeElement>()
            {
                {EntityType.Producer, new EntityTypeElement()},
                {EntityType.Consumer, new EntityTypeElement()},
                {EntityType.Storage, new EntityTypeElement()},
                {EntityType.Transport, new EntityTypeElement()},
            };
        }

        public EntityTypeElement GetEntityTypeElement(EntityType et)
        {
            return (currentEntities[et]);
        }

        public void panToEntity(EntityType et, bool next)
        {
            if (!currentEntities[et].currentList.IsEmpty)
            {
                EntityId entityId = next ? currentEntities[et].nextEntity() : currentEntities[et].previousEntity();
                if (_entitiesManager.TryGetEntity(entityId, out Entity entityOut))
                {
                    if (entityOut is LayoutEntity layoutEntity)
                    {
                        _cameraController.PanTo(new Tile2f(layoutEntity.Transform.Position.X, layoutEntity.Transform.Position.Y));
                    }
                    else if (entityOut is Vehicle vehicle)
                    {
                        _cameraController.PanTo(vehicle.Position2f);
                    }
                } 
            }
        }

        public void highlightUsage(ProductProto highlightProduct)
        {

            foreach (Entity e in _entitiesManager.Entities)
            {
                Type entityType = e.GetType();
                if (entityType == typeof(Machine))
                {
                    switch (highlightMachine(e as Machine, highlightProduct))
                    {
                        case 1: currentEntities[EntityType.Producer].currentList.Add(e.Id);
                            break;
                        case 2: currentEntities[EntityType.Consumer].currentList.Add(e.Id);
                            break;
                        default: break;
                    }
                }
                else if (entityType == typeof(Storage))
                {
                    if (highlightStorage(e as Storage, highlightProduct))
                    {
                        currentEntities[EntityType.Storage].currentList.Add(e.Id);
                    };
                }
                else if (entityType == typeof(Transport))
                {
                    if (highlightTransport(e as Transport, highlightProduct))
                    {
 //                       currentEntities[EntityType.Transport].currentList.Add(e.Id);
                    }
                }
                else if (entityType == typeof(AnimalFarm))
                {
                    if (highlightAnimalFarm(e as AnimalFarm, highlightProduct))
                    {
                        currentEntities[EntityType.Producer].currentList.Add(e.Id);
                    }
                }
                else if (entityType == typeof(Farm))
                {
                    if (highlightFarm(e as Farm, highlightProduct))
                    {
                        currentEntities[EntityType.Producer].currentList.Add(e.Id);
                    }
                }
                else if (entityType == typeof(Truck))
                {
                    if (highlightTruck(e as Truck, highlightProduct))
                    {
                        currentEntities[EntityType.Transport].currentList.Add(e.Id);
                    }
                }
                else if (entityType == typeof(Excavator))
                {
                    if (highlightExcavator(e as Excavator, highlightProduct))
                    {
                        currentEntities[EntityType.Transport].currentList.Add(e.Id);
                    }
                }
            }
        }

        public bool highlightAnimalFarm(AnimalFarm animalFarm, ProductProto highlightProduct)
        {

            if ((animalFarm.Prototype.Animal == highlightProduct) ||
                    (animalFarm.Prototype.CarcassProto == highlightProduct) ||
                    (animalFarm.Prototype.ProducedPerAnimalPerMonth.Value.Product == highlightProduct))
            {
                _entityHighlighter.Instance.Highlight(animalFarm, colorProducer);
                return true;
            }
            return false;
        }

        public int highlightMachine(Machine machine, ProductProto highlightProduct)
        {
            int needsHighlight = 0; ;
            ColorRgba color;

            foreach (RecipeProto r in machine.RecipesAssigned)
            {
                foreach (RecipeOutput ri in r.AllOutputs.AsEnumerable())
                {
                    if (ri.Product == highlightProduct)
                    {
                        needsHighlight = 1;
                    }
                }
                if (needsHighlight == 0)
                {
                    foreach (RecipeInput ri in r.AllInputs.AsEnumerable())
                    {
                        if (ri.Product == highlightProduct)
                        {
                            needsHighlight = 2;
                            break;
                        }
                    }
                }
                if (needsHighlight != 0)
                {
                    color = (needsHighlight == 1) ? colorProducer : colorConsumer;
                    _entityHighlighter.Instance.Highlight(machine, color);
                }
            }
            return needsHighlight;
        }

        public bool highlightFarm(Farm farm, ProductProto highlightProduct)
        {
            foreach (var c in farm.Schedule)
            {
                if (c != null)
                {
                    if (c.Value.ProductProduced.Product == highlightProduct)
                    {
                        _entityHighlighter.Instance.Highlight(farm, colorProducer);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool highlightStorage(Storage storage, ProductProto highlightProduct) 
        {
            if (storage.StoredProduct != Option<ProductProto>.None)
            {
                if (storage.StoredProduct.Value == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(storage, ColorRgba.Magenta);
                    return true;
                }
            }
            return false;
        }

        public bool highlightTransport(Transport transport,ProductProto highlightProduct)
        {
            foreach (var tp in transport.TransportedProducts.AsEnumerable())
            {
                if (tp.SlimId.ToFullOrPhantom(_productSlimIdManager) == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(transport, colorTransport);
                    return true; 
                }
            }
            return false;
        }

        public bool highlightTruck(Truck truck, ProductProto highlightProduct)
        {
            var cargoEnumerator = truck.Cargo.GetEnumerator();
            while (cargoEnumerator.MoveNext())
            {
                if (cargoEnumerator.Current.Key == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(truck, colorTransport);
                    return true;
                }
            }
            return false;
        }

        public bool highlightExcavator(Excavator excavator, ProductProto highlightProduct)
        {
            var cargoEnumerator = excavator.Cargo.GetEnumerator();
            while (cargoEnumerator.MoveNext())
            {
                if (cargoEnumerator.Current.Key == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(excavator, colorTransport);
                    return true;
                }
            }
            return false;
        }

        public void clearHighlights()
        {
            _entityHighlighter.Instance.ClearAllHighlights();
            clearEntityDict();
        }
    }
}
