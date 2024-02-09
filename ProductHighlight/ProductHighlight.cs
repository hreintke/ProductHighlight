using Mafi.Core.Mods;
using Mafi;
using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Prototypes;
using System;

namespace ProductHighlight
{
    public sealed class ProductHighlight : IMod
    {
        public string Name => "ProductHighlight";

        public int Version => 1;
        public static Version ModVersion = new Version(0, 0, 3);
        public bool IsUiOnly => false;

        public void ChangeConfigs(Lyst<IConfig> configs)
        {
        }

        public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
        {
        }

        public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
        {
        }

        public void RegisterPrototypes(ProtoRegistrator registrator)
        {
        }
    }

}
