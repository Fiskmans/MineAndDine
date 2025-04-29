using Godot;
using MineAndDine.Code;
using MineAndDine.Code.Materials;
using System;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;

public partial class Container : MeshInstance3D
{
    [Export]
    public int myCapacity = 1;

    public int myAmount = 0;

    AnimationPlayer myFillAnimation;

    public float Fraction
    {
        get => (float)myAmount / (float)myCapacity;
    }

    public bool Empty
    {
        get => myAmount == 0;
    }

    private MaterialsArray<int> _Contents;
    public MaterialsArray<int> Contents
    {
        get
        {
            return _Contents;
        }
        set
        {
            _Contents = value;
            Refresh();
        }
    }

    public override string ToString()
    {
        return $"{myAmount}/{myCapacity} [{Fraction}%]";
    }

    public override void _Ready()
    {
        myFillAnimation = GetNode<AnimationPlayer>("Fill");
        myFillAnimation?.Play("Fill");
        myFillAnimation?.Seek(Fraction, true);

        GD.Print("fraction: ", Fraction);
    }

    public bool TakeFrom<U>(Container aOther, MaterialsArray<U> aGroup)
    {
        bool res = TakeFrom(ref aOther._Contents, aGroup);
        aOther.Refresh();
        return res;
    }

    public bool TakeFrom<T, U>(ref MaterialsArray<T> aMaterials, MaterialsArray<U> aGroup) where T : System.Numerics.INumber<T>
    {
        bool res = MaterialInteractions.Move(aGroup, ref aMaterials, ref _Contents, myCapacity);
        Refresh();
        return res;
    }

    public bool GiveTo<U>(Container aOther, MaterialsArray<U> aGroup)
    {
        bool res = GiveTo(ref aOther._Contents, aGroup);
        aOther.Refresh();
        return res;
    }

    public bool GiveTo<T, U>(ref MaterialsArray<T> aMaterials, MaterialsArray<U> aGroup) where T : System.Numerics.INumber<T>
    {
        bool res = MaterialInteractions.Move(aGroup, ref _Contents, ref aMaterials, T.CreateChecked(myCapacity));
        Refresh();
        return res;
    }

    private void Refresh()
    {
        int amount = MaterialInteractions.Total(ref _Contents);

        //TODO trigger event on changed

        myAmount = amount;

        myFillAnimation?.Seek(Fraction, true);
        (Mesh.SurfaceGetMaterial(0) as ShaderMaterial)?.SetShaderParameter("albedo_color", MaterialInteractions.Color(ref _Contents)); // TODO: figure out why this doesnt work
    }
}
