﻿using Mafi;
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
using Mafi.Unity.Entities;
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

namespace ProductHighlight.Actions
{
    [GlobalDependency(RegistrationMode.AsSelf, false)]
    public class Highlight
    {
        private readonly EntitiesManager _entitiesManager;
        private readonly IVehiclesManager _vehiclesManager;
        private readonly ProductsSlimIdManager _productSlimIdManager;
        private readonly Mafi.NewInstanceOf<EntityHighlighter> _entityHighlighter;
        private readonly ProtosDb _protosDb;
        private readonly InputScheduler _inputScheduler;
        private readonly TerrainMiningManager _terrainMiningManager;
        private readonly TerrainDumpingManager _terrainDumpingManager;
        private ColorRgba colorProducer = ColorRgba.White;
        private ColorRgba colorConsumer = ColorRgba.Yellow;
        private ColorRgba colorWorking = ColorRgba.Green;
        private ColorRgba colorFail = ColorRgba.Red;
        private ColorRgba colorOther = ColorRgba.Yellow;
        private ColorRgba colorStoarge = ColorRgba.Magenta;
        private ColorRgba colorTransport = ColorRgba.Green;

        public Highlight(
                            EntitiesManager entitiesManager,
                            IVehiclesManager vehiclesManager,
                            ProductsSlimIdManager productSlimIdManager,
                            Mafi.NewInstanceOf<EntityHighlighter> entityHighlighter,
                            InputScheduler inputScheduler,
                            TerrainMiningManager terrainMiningManager,
                            TerrainDumpingManager terrainDumpingManager,
                            ProtosDb db)
        {
           
            _entitiesManager = entitiesManager;
            _vehiclesManager = vehiclesManager;
            _productSlimIdManager = productSlimIdManager;
            _entityHighlighter = entityHighlighter;
            _inputScheduler = inputScheduler;
            _terrainDumpingManager = terrainDumpingManager;
            _terrainMiningManager = terrainMiningManager;
            _protosDb = db;
        }

        public void highlightUsage(string highlightProduct)
        {
            foreach (Entity e in _entitiesManager.Entities)
            {
                Type entityType = e.GetType();
                if (entityType == typeof(Machine))
                {
                    highlightMachine(e as Machine, highlightProduct);
                }
                else if (entityType == typeof(Storage))
                {
                    highlightStorage(e as Storage, highlightProduct);
                }
                else if (entityType == typeof(Transport))
                {
                    highlightTransport(e as Transport, highlightProduct);
                }
                else if (entityType == typeof(AnimalFarm))
                {
                    highlightAnimalFarm(e as AnimalFarm, highlightProduct);
                }
                else if (entityType == typeof(Farm))
                {
                    highlightFarm(e as Farm, highlightProduct);
                }
                else if (entityType == typeof(Truck))
                {
                    highlightTruck(e as Truck, highlightProduct);
                }
                else if (entityType == typeof(Excavator))
                {
                    highlightExcavator(e as Excavator, highlightProduct);
                }
            }
        }

        public void highlightAnimalFarm(AnimalFarm animalFarm, string highlightProduct)
        {

            if ((animalFarm.Prototype.Animal.Id.Value == highlightProduct) ||
                    (animalFarm.Prototype.CarcassProto.Id.Value == highlightProduct) ||
                    (animalFarm.Prototype.ProducedPerAnimalPerMonth.Value.Product.Id.Value == highlightProduct))
            {
                _entityHighlighter.Instance.Highlight(animalFarm, colorProducer);
            }
        }

        public void highlightMachine(Machine machine, string highlightProduct)
        {
            int needsHighlight = 0; ;
            ColorRgba color;

            foreach (RecipeProto r in machine.RecipesAssigned)
            {
                foreach (RecipeOutput ri in r.AllOutputs.AsEnumerable())
                {
                    if (ri.Product.Id.Value == highlightProduct)
                    {
                        needsHighlight = 1;
                    }
                }
                if (needsHighlight == 0)
                {
                    foreach (RecipeInput ri in r.AllInputs.AsEnumerable())
                    {
                        if (ri.Product.Id.Value == highlightProduct)
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
        }

        public void highlightFarm(Farm farm, string highlightProduct)
        {
            foreach (var c in farm.Schedule)
            {
                if (c != null)
                {
                    if (c.Value.ProductProduced.Product.Id.Value.ToString() == highlightProduct)
                    {
                        _entityHighlighter.Instance.Highlight(farm, colorProducer);
                    }
                }
            }
        }

        public void highlightStorage(Storage storage, string highlightProduct) 
        {
            if (storage.StoredProduct != Option<ProductProto>.None)
            {
                if (storage.StoredProduct.Value.Id.ToString() == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(storage, ColorRgba.Magenta);
                }
            }
        }

        public void highlightTransport(Transport transport,string highlightProduct)
        {
            foreach (var tp in transport.TransportedProducts.AsEnumerable())
            {
                if (tp.SlimId.ToFullOrPhantom(_productSlimIdManager).Id.ToString() == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(transport, colorTransport);
                    break; // No need tom check other products on the transport
                }
            }
        }

        public void highlightTruck(Truck truck, string highlightProduct)
        {
            var cargoEnumerator = truck.Cargo.GetEnumerator();
            while (cargoEnumerator.MoveNext())
            {
                if (cargoEnumerator.Current.Key.Id.Value == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(truck, colorTransport);
                }
            }
        }

        public void highlightExcavator(Excavator excavator, string highlightProduct)
        {
            var cargoEnumerator = excavator.Cargo.GetEnumerator();
            while (cargoEnumerator.MoveNext())
            {
                if (cargoEnumerator.Current.Key.Id.Value == highlightProduct)
                {
                    _entityHighlighter.Instance.Highlight(excavator, colorTransport);
                }
            }
        }

         public void clearHighlights()
        {
            _entityHighlighter.Instance.ClearAllHighlights();
        }
    }
}
