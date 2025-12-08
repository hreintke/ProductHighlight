using Mafi.Core.Mods;
using Mafi;
using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.Prototypes;
using System;

namespace ProductHighlight;

public sealed class ProductHighlight : IMod
{
    public string Name => typeof(ProductHighlight).Assembly.GetName().Name;

    public int Version => (typeof(ProductHighlight).Assembly.GetName().Version.Major * 100) +
                            (typeof(ProductHighlight).Assembly.GetName().Version.Minor * 10) +
                            (typeof(ProductHighlight).Assembly.GetName().Version.Build);

    public static Version ModVersion => typeof(ProductHighlight).Assembly.GetName().Version;

    public bool IsUiOnly => false;

    public Option<IConfig> ModConfig => throw new NotImplementedException();

    public void ChangeConfigs(Lyst<IConfig> configs)
    {
    }

    public void Initialize(DependencyResolver resolver, bool gameWasLoaded)
    {
        LogWrite.Info($"{Name} initialized");
    }

    public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded)
    {
    }

    public void RegisterPrototypes(ProtoRegistrator registrator)
    {
    }

    public void EarlyInit(DependencyResolver resolver)
    {
    }
}
