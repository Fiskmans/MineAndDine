using Godot;
using System;

public partial class Interactable : StaticBody3D
{
    public delegate void InteractionHandler(Interactable aSender);
    public event InteractionHandler OnActivate;

    public void Activate()
    {
        OnActivate?.Invoke(this);
    }
}
