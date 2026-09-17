namespace Assured.Analyzers.Tests;

public class UncheckedUnwrapControlFlowTests
{
    [Fact]
    public async Task WhileHasValue_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                int s = 0;
                while (r.HasValue())
                {
                    s += r.UnwrapValue();
                    r = Load(s);
                }
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Loop_GuardInsideBody_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                int s = 0;
                for (int i = 0; i < x; i++)
                {
                    if (r.HasError())
                        r = Load(i);
                    else
                        s += r.UnwrapValue();
                }
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Loop_GuardBeforeLoop_ReassignedInside_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                int s = 0;
                for (int i = 0; i < x; i++)
                {
                    s += r.UnwrapValue();
                    r = Load(i);
                }
                return s;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Loop_GuardBeforeLoop_NotReassigned_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                int s = 0;
                for (int i = 0; i < x; i++)
                    s += r.UnwrapValue();
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Loop_BoolLocalInBody_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                int s = 0;
                for (int i = 0; i < x; i++)
                {
                    bool ok = r.HasValue();
                    if (ok)
                        s += r.UnwrapValue();
                }
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Loop_ShortCircuitCondition_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                int s = 0;
                while (r.HasValue() && s < 10)
                {
                    s += r.UnwrapValue();
                    r = Load(s);
                }
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Loop_CapturedConditionInBody_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                int s = 0;
                while (s < 10)
                {
                    bool ok = r.HasValue() && s > 2;
                    if (ok)
                        s += r.UnwrapValue();
                    else
                        s++;
                }
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Catch_Unguarded_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                try
                {
                    return 1;
                }
                catch (Exception)
                {
                    return r.UnwrapValue();
                }
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Catch_GuardedInside_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                try
                {
                    return 1;
                }
                catch (Exception)
                {
                    return r.HasValue() ? r.UnwrapValue() : 0;
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Catch_SeesGuardBeforeTry_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                try
                {
                    Work();
                    return 1;
                }
                catch (Exception)
                {
                    return r.UnwrapValue();
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Catch_SeesReassignmentInsideTry_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                try
                {
                    r = Other();
                    Work();
                    return 1;
                }
                catch (Exception)
                {
                    return r.UnwrapValue();
                }
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AfterTryCatch_KeepsGuardBeforeTry_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                try
                {
                    Work();
                }
                catch (Exception)
                {
                    Work();
                }
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Finally_SeesGuardBeforeTry_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                int s = 0;
                try
                {
                    Work();
                }
                finally
                {
                    s = r.UnwrapValue();
                }
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Finally_Unguarded_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                int s = 0;
                try
                {
                    Work();
                }
                finally
                {
                    s = r.UnwrapValue();
                }
                return s;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Lambda_SeesGuardAtCreation_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            Func<int> M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return () => 0;
                return () => r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Lambda_Unguarded_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            Func<int> M(int x)
            {
                var r = Load(x);
                return () => r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task NestedLambda_SeesGuardAtCreation_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            Func<Func<int>> M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return () => () => 0;
                return () => () => r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LocalFunction_DoesNotSeeOuterGuard_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                return Inner();

                int Inner() => r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task LocalFunction_GuardedInside_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return Inner();

                int Inner() => r.HasValue() ? r.UnwrapValue() : 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Catch_SeesStateInTheMiddleOfAnExpression_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                try
                {
                    r = Result<int, string>.Value(Fail(r = Other()));
                }
                catch (Exception)
                {
                    return r.UnwrapValue();
                }
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AfterTryFinally_KeepsGuardBeforeTry_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                try
                {
                    Work();
                }
                finally
                {
                    Work();
                }
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AfterTryFinally_ForgetsWhatFinallyAssigns_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                try
                {
                    Work();
                }
                finally
                {
                    r = Other();
                }
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AfterTryFinally_ForgetsTernaryAssignedInFinally_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                try
                {
                    Work();
                }
                finally
                {
                    r = Coin() ? Other() : Load(0);
                }
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RejectedFilter_ChangesReachTheNextCatch_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                try
                {
                    Work();
                }
                catch (Exception) when ((r = Other()).HasValue())
                {
                }
                catch (Exception)
                {
                    return r.UnwrapValue();
                }
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }
}
