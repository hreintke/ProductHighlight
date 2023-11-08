using Mafi;
using Mafi.Core.Buildings.Storages;
using Mafi.Core.Entities;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Unity.Entities;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductHighlight.Actions
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class Highlight
    {
        private readonly EntitiesManager _entitiesManager;
        private readonly Mafi.NewInstanceOf<EntityHighlighter> _entityHighlighter;
        private readonly ProtosDb _protosDb;

        public Highlight(EntitiesManager entitiesManager, Mafi.NewInstanceOf<EntityHighlighter> entityHighlighter, ProtosDb db)
        {
            _entitiesManager = entitiesManager;
            _entityHighlighter = entityHighlighter;
            _protosDb = db;
        }

        public void highlightMachines(string product)
        {
            string highlightProduct = "Product_" + product;
            foreach (Entity e in _entitiesManager.GetAllEntitiesOfType<Machine>())
            {
                Machine m = e as Machine;
                foreach (RecipeProto r in m.RecipesAssigned)
                {
                    foreach (RecipeOutput ri in r.AllOutputs.AsEnumerable())
                    {
                        if (ri.Product.Id.Value == highlightProduct)
                        {
                            ColorRgba color;
                            switch (m.CurrentState)
                            {
                                case Machine.State.NotEnoughComputing :
                                case Machine.State.NotEnoughInput :
                                case Machine.State.NotEnoughPower :
                                case Machine.State.NotEnoughWorkers : color = ColorRgba.Red;
                                    break;
                                case Machine.State.Working: color = ColorRgba.Green;
                                    break;
                                default : color = ColorRgba.Yellow;
                                    break;
                            }
                            _entityHighlighter.Instance.Highlight(m, color);
                        }
                    }
                }
            }
        }
        
        public void highlightStorage(string product) 
        {
            string highlightProduct = "Product_" + product;
            foreach (Entity e in _entitiesManager.GetAllEntitiesOfType<Storage>())
            {
                Storage s = e as Storage;
                if (s.StoredProduct != Option<ProductProto>.None)
                {
                    if (s.StoredProduct.Value.Id.ToString() == highlightProduct)
                    {
                        _entityHighlighter.Instance.Highlight(s, ColorRgba.Blue);
                    }
                }
            }
        }
        
        public void highlightAll(String product)
        {
            highlightMachines(product);
            highlightStorage(product);
        }

        public void clearHighlights()
        {
            _entityHighlighter.Instance.ClearAllHighlights();
        }

    }
}
