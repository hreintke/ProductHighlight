using HarmonyLib;
using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Core;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Dynamic;
using Mafi.Core.Entities.Static;
using Mafi.Core.GameLoop;
using Mafi.Core.Products;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain.Designation;
using Mafi.Core.Vehicles;
using Mafi.Core.Vehicles.Jobs;
using Mafi.Core.Vehicles.Trucks;
using Mafi.Unity;
using Mafi.Unity.Entities.Static;
using Mafi.Unity.Mine;
using RTG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Jobs;
using UnityEngine;


namespace ProductHighlight;


[GlobalDependency(RegistrationMode.AsSelf)]
[HarmonyPatch]
public class ShowCargoJobs
{
    private readonly Harmony harmony;
    private static LinesFactory linesFactory;
    private static EntitiesManager entitiesManager;
    private static ProductProto.ID currentProduct;
    private static PropertyInfo jobsProperty;
    private static FieldInfo outbufField;
    static private GameLoopEvents gameLoopEvents;

    static Dict<VehicleJobId,LineMb> lineDict = new Dict<VehicleJobId, LineMb>();
    ShowCargoJobs(LinesFactory lf, EntitiesManager em, GameLoopEvents ge)
    {
        harmony = new Harmony("ShowCargoJobs");

        if (Harmony.HasAnyPatches("ShowCargoJobs"))
        {
            LogWrite.Info($"Allready applied , removing UT harmony patches");
            harmony.UnpatchAll("ShowCargoJobs");
        }

        harmony.PatchAll();
        LogWrite.Info("ShowCargoJobs Harmony patches applied");

        linesFactory = lf;
        entitiesManager = em;
        gameLoopEvents = ge;
        gameLoopEvents.Terminate.AddNonSaveable<ShowCargoJobs>(this, terminateEvent);

        currentProduct = new ProductProto.ID();

        jobsProperty = typeof(Truck).GetProperty("Jobs", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        outbufField = typeof(CargoPickUpJob).GetField("m_outputBuffer", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

    }

    void terminateEvent()
    {
        LogWrite.Info("Remove ShowCargoJobs Harmony patches");
        harmony.UnpatchAll("ShowCargoJobs");
    }

    public void activate(ProductProto.ID newCurrent)
    {
        deactivate();
        harmony.PatchAll();
        currentProduct = newCurrent;
    }

    public void deactivate() 
    {
        currentProduct = new ProductProto.ID();
        foreach (var line in lineDict)
        {
            line.Value.Hide();
            line.Value.ReturnToPool();
        }
        lineDict.Clear();
        harmony.UnpatchAll("ShowCargoJobs");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CargoDeliveryJob), MethodType.Constructor,
    new Type[]
        { typeof(VehicleJobId),
                typeof(CargoDeliveryJob.Factory),
                typeof(Truck),
                typeof(ProductQuantity),
                typeof(RegisteredInputBuffer),
                typeof(Option<Lyst<SecondaryInputBufferSpec>>)
    })]
    static void deliver(CargoDeliveryJob __instance, Truck truck, ProductQuantity toDeliver, RegisteredInputBuffer inputBuffer)
    {
        if (toDeliver.Product.Id != currentProduct)
        {
            return;
        }

        VehicleJobsSequence jobs = (VehicleJobsSequence)jobsProperty.GetValue(truck);

        for (int i = 0; i < jobs.Count; i++)
        {
            if (jobs[i] is CargoPickUpJob cpu)
            {
                RegisteredOutputBuffer outbuf = (RegisteredOutputBuffer)outbufField.GetValue(cpu);
                LineMb l = linesFactory.CreateLinePooled(outbuf.Entity.Position3f.ToVector3(), inputBuffer.Entity.Position3f.ToVector3(), 0.5f, Color.yellow);
                l.Show();
                lineDict[__instance.Id] = l;
            }
        }      
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(VehicleJob),"InvokeJobDone")]
    static void jobdone(VehicleJob __instance)
    {
        if (__instance is CargoDeliveryJob cdj)
        {
            if (cdj.CargoToDeliver.Product.Id != currentProduct)
            {
                return;
            }
            if (lineDict.ContainsKey(cdj.Id))
            {

                lineDict[cdj.Id].Hide();
                lineDict[cdj.Id].ReturnToPool();
                lineDict.Remove(cdj.Id);
            }
        }
        if (__instance is CargoPickUpJob cpu)
        {
            if (cpu.CargoToPickup.Product.Id != currentProduct)
            {
                return;
            }
        }
    }
}
