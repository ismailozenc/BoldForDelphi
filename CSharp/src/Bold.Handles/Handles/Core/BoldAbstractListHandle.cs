// Delphi: Source/Handles/Core/BoldAbstractListHandle.pas
// Port map: CSharp/port/units/BoldAbstractListHandle.md

using System.ComponentModel;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>A rooted handle on a list with a current element, which First, Last, Next and Prior move.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractListHandle : TBoldRootedHandle
{
    /// <summary>Adds the handle to <paramref name="Owner"/> when given.</summary>
    protected TBoldAbstractListHandle(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)] private int GetCount() => List?.Count ?? 0;

    /// <summary>The current element as an object; raises when it is not one.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetCurrentBoldObject()
    {
        return CurrentElement switch
        {
            TBoldObject boldObject => boldObject,
            null => null,
            _ => throw new EBold(SysUtils.Format(sCurrentElementNotBoldObject, GetType().Name)),
        };
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetListElementType() =>
        List is { } list ? ((TBoldListTypeInfo)list.BoldType!).ListElementTypeInfo : StaticBoldType;

    [Delphi(PortStatus.Ported)] private bool GetHasNext() => CurrentIndex + 1 < Count;
    [Delphi(PortStatus.Ported)] private bool GetHasPrior() => CurrentIndex > 0;

    [Delphi(PortStatus.Ported)]
    private TBoldListTypeInfo? GetListType() => List is { } list ? (TBoldListTypeInfo)list.BoldType! : StaticListType;

    [Delphi(PortStatus.Ported)]
    private TBoldListTypeInfo? GetStaticListType() => StaticSystemTypeInfo?.ListTypeInfoByElement(StaticBoldType);

    [Delphi(PortStatus.Ported)] private TBoldObjectList? GetObjectList() => List as TBoldObjectList;
    [Delphi(PortStatus.Ported)] private TBoldObjectList? GetMutableObjectList() => MutableList as TBoldObjectList;

    [Delphi(PortStatus.Ported)] protected abstract TBoldElement? GetCurrentElement();
    [Delphi(PortStatus.Ported)] protected abstract int GetCurrentIndex();
    [Delphi(PortStatus.Ported)] protected abstract TBoldList? GetList();
    [Delphi(PortStatus.Ported)] protected abstract void SetCurrentIndex(int Value);

    /// <summary>The list when it can be changed, else null.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldList? GetMutableList()
    {
        TBoldList? BoldList = List;
        return BoldList is not null && BoldList.Mutable ? BoldList : null;
    }

    [Delphi(PortStatus.Ported)]
    public void First()
    {
        if (Count > 0)
        {
            CurrentIndex = 0;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Last() => CurrentIndex = Count - 1;

    [Delphi(PortStatus.Ported)]
    public void Next()
    {
        if (!GetHasNext())
        {
            throw new EBold(SysUtils.Format(sNoNextElement, GetType().Name));
        }

        CurrentIndex = CurrentIndex + 1;
    }

    [Delphi(PortStatus.Ported)]
    public void Prior()
    {
        if (!GetHasPrior())
        {
            throw new EBold(SysUtils.Format(sNoPreviousElement, GetType().Name));
        }

        CurrentIndex = CurrentIndex - 1;
    }

    /// <summary>Removes the current element from the list, or from the mutable list behind it.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveCurrentElement()
    {
        if (CurrentIndex == -1)
        {
            throw new EBold(SysUtils.Format(sNoCurrentElement, GetType().Name));
        }

        if (List!.Mutable)
        {
            List.RemoveByIndex(CurrentIndex);
        }
        else if (MutableList is { } mutableList)
        {
            mutableList.Remove(CurrentElement);
        }
        else
        {
            throw new EBold(SysUtils.Format(sCannotRemoveCurrentFromImmutable, GetType().Name, Name));
        }
    }

    [Delphi(PortStatus.Ported)] public int Count => GetCount();
    [Delphi(PortStatus.Ported)] public TBoldObject? CurrentBoldObject => GetCurrentBoldObject();
    [Delphi(PortStatus.Ported)] public TBoldElement? CurrentElement => GetCurrentElement();

    [Delphi(PortStatus.Ported)]
    public int CurrentIndex
    {
        get => GetCurrentIndex();
        set => SetCurrentIndex(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldList? List => GetList();
    [Delphi(PortStatus.Ported)] public TBoldObjectList? ObjectList => GetObjectList();
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ListElementType => GetListElementType();
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? ListType => GetListType();
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? StaticListType => GetStaticListType();
    [Delphi(PortStatus.Ported)] public bool HasNext => GetHasNext();
    [Delphi(PortStatus.Ported)] public bool HasPrior => GetHasPrior();
    [Delphi(PortStatus.Ported)] public TBoldList? MutableList => GetMutableList();
    [Delphi(PortStatus.Ported)] public TBoldObjectList? MutableObjectList => GetMutableObjectList();
}
