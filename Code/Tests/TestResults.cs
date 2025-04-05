using Godot;
using MineAndDine.Code.Extensions;
using MineAndDine.Code.Tests;
using System;

public partial class TestResults : Label
{
    public override void _Process(double delta)
    {
        base._Process(delta);

        int running = 0;
        int failed = 0;
        int passed = 0;

        foreach(Test test in GetTree().Root.AllChildren<Test>())
        {
            switch (test.Result)
            {
                case Test.ResultType.NotStarted:
                    break;
                case Test.ResultType.Running:
                    running++;
                    break;
                case Test.ResultType.Passed:
                    passed++;
                    break;
                case Test.ResultType.Failed:
                    failed++;
                    break;
                default:
                    break;
            }

        }

        Text = $"Running: {running}\nPassed: {passed}\nFailed: {failed}";
    }
}
