namespace Assured.Analyzers.Tests;

public class UncheckedUnwrapChecksTests
{
    [Fact]
    public async Task IfHasValue_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (r.HasValue())
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IfHasValue_InElseBranch_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (r.HasValue())
                    return 1;
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task EarlyReturnOnNotHasValue_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return -1;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task EarlyThrowOnNotHasValue_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    throw new Exception();
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Ternary_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return r.HasValue() ? r.UnwrapValue() : 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Ternary_InWrongBranch_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return r.HasValue() ? 0 : r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AndAlso_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            bool M(int x)
            {
                var r = Load(x);
                return r.HasValue() && r.UnwrapValue() > 3;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task OrElse_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            bool M(int x)
            {
                var r = Load(x);
                return r.HasValue() || r.UnwrapValue() > 3;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task ComparedWithFalse_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (r.HasValue() == false)
                    return 0;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NotEqualToTrue_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (true != r.HasValue())
                    return 0;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IsTruePattern_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (r.HasValue() is true)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task IsNotTruePattern_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (r.HasValue() is not true)
                    return 0;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task SwitchOnCheck_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                switch (r.HasValue())
                {
                    case true:
                        return r.UnwrapValue();
                    default:
                        return 0;
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task HasError_ThenUnwrapError_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            string M(int x)
            {
                var r = Load(x);
                if (r.HasError())
                    return r.UnwrapError();
                return "";
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NotHasError_ThenUnwrapValue_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (r.HasError())
                    return 0;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task TryUnwrapValue_AsGuard_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (r.TryUnwrapValue(out _))
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task TryUnwrapError_AsGuard_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            string M(int x)
            {
                var r = Load(x);
                if (!r.TryUnwrapError(out _))
                    return "";
                return r.UnwrapError();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GuardOnDifferentVariable_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var a = Load(x);
                var b = Other();
                if (a.HasValue())
                    return b.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task TernaryWithAndAlsoCondition_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return r.HasValue() && r.UnwrapValue() > 3 ? r.UnwrapValue() : 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task TernaryWithOrElseCondition_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return !r.HasValue() || r.UnwrapValue() < 0 ? 0 : r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                if (ok)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_Negated_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool bad = !r.HasValue();
                if (bad)
                    return 0;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_StaleAfterReassign_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                r = Other();
                if (ok)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_SurvivesCheckOfAnotherVariable_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                var q = Other();
                bool ok = q.HasValue();
                if (r.HasValue() && ok)
                    return q.UnwrapValue() + r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_SurvivesReassignOfAnotherVariable_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                var q = Other();
                bool ok = q.HasValue();
                r = Load(x + 1);
                if (ok)
                    return q.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_KeepsKnowledgeGainedAfterIt_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                var q = Other();
                bool ok = q.HasValue();
                if (r.HasValue())
                {
                    if (ok)
                        return q.UnwrapValue() + r.UnwrapValue();
                }
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_ContradictedByLaterCheck_MakesPathUnreachable()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                if (!r.HasValue())
                    return 0;
                if (!ok)
                    return r.UnwrapError().Length;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_CompoundAssignment_ForgetsTheGuard()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                ok ^= true;
                if (ok)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_PassedByRef_ForgetsTheGuard()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                Flip(ref ok);
                if (ok)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_Deconstructed_ForgetsTheGuard()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                int y;
                (ok, y) = (true, 1);
                if (ok)
                    return r.UnwrapValue() + y;
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_AssignedInsideLocalFunction_IsNotTrusted()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                void Force() => ok = true;
                Force();
                if (ok)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AssignmentInsideCondition_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok;
                if (ok = r.HasValue())
                    return r.UnwrapValue();
                return ok ? 1 : 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_CompoundAssignmentWithBranchingOperand_ForgetsTheGuard()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                ok |= Coin() && Coin();
                if (ok)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_SurvivesReassignOfAnotherKnownVariable_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                var other = Result<int, string>.Value(1);
                bool ok = r.HasValue();
                other = Other();
                return ok ? r.UnwrapValue() : 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task CheckOnAssignmentExpression_GuardsTheTarget()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                Result<int, string> r;
                if ((r = Load(x)).HasValue())
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WhileCheckOnAssignmentExpression_GuardsTheTarget()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                Result<int, string> r;
                int s = x;
                while ((r = Load(s)).HasValue())
                    s -= r.UnwrapValue();
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task BoolLocal_AssignedItsOwnNegationInsideCondition_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                bool ok = r.HasValue();
                if (ok = !ok)
                    return r.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }
}
