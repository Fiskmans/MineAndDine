using Godot;
using MineAndDine.Code.Materials;
using MineAndDine.Code.Tests;
using System;

public partial class GenerationTests : TestGroup
{
    public override void _Ready()
    {
        base._Ready();

        foreach (MaterialType mat in MaterialGroups.Indexes(MaterialGroups.Generatable))
        {
            AddTest(new GenerationTest { myMaterial = mat });
        }
    }
}
