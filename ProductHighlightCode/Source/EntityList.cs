using Mafi.Collections;
using Mafi.Core;
using ProductHighlight.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EntityList
{
    private int currentIndex;
    private Lyst<EntityId> currentList;

    public EntityList()
    {
        reset();
    }

    public void reset()
    {
        currentIndex = -1;
        currentList = new Lyst<EntityId>();
    }

    public void addEntity(EntityId entity)
    {
        if (!currentList.Contains(entity))
        {
            currentList.Add(entity);
        }
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

    public Lyst<EntityId> getLyst => currentList;
}