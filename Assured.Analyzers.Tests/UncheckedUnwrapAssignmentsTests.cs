namespace Assured.Analyzers.Tests;

public class UncheckedUnwrapAssignmentsTests
{
    [Fact]
    public async Task ValueFactory_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                var r = Result<int, string>.Value(1);
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ErrorFactory_UnwrapValue_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                var r = Result<int, string>.Error("boom");
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task FactoryAsReceiver_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M() => Result<int, string>.Value(1).UnwrapValue();
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ImplicitValue_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 5;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ImplicitError_UnwrapValue_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = "boom";
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task ImplicitError_UnwrapError_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            string M()
            {
                Result<int, string> r = "boom";
                return r.UnwrapError();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReassignedAfterGuard_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                r = Other();
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RepairedInBranch_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    r = Result<int, string>.Value(0);
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AssignedInBothBranches_Mixed_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r;
                if (Coin())
                    r = 1;
                else
                    r = "e";
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AssignedInBothBranches_Values_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r;
                if (Coin())
                    r = 1;
                else
                    r = 2;
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task TernaryOfValues_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                var r = Coin() ? Result<int, string>.Value(1) : Result<int, string>.Value(2);
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task TernaryOfValueAndUnknown_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Coin() ? Result<int, string>.Value(1) : Load(x);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task OutArgument_ResetsKnowledge()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                Fill(out r);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RefArgument_ResetsKnowledge()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                Touch(ref r);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Deconstruction_ResetsKnowledge()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                int x;
                (r, x) = (Other(), 2);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task DeconstructionDeclaration_IsUnknown()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                var (r, x) = (Other(), 2);
                return r.UnwrapValue() + x;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task CopyOfGuarded_InheritsState()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var a = Load(x);
                if (!a.HasValue())
                    return 0;
                var b = a;
                return b.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AssignedValue_IsEvaluatedInStateBeforeTheAssignment()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                int s = 0;
                while (r.HasValue())
                {
                    r = Load(r.UnwrapValue() - 1);
                    s++;
                }
                return s;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Map_KeepsValueState()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                return r.Map(v => v + 1).UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Map_KeepsErrorState()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = "e";
                return r.Map(v => v + 1).UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task MapError_KeepsValueState()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                return r.MapError(e => e.Length).UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Bind_OnValue_IsUnknown()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            string M()
            {
                Result<int, string> r = 1;
                return r.Bind(Next).UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Bind_OnError_StaysError()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            string M()
            {
                Result<int, string> r = "e";
                return r.Bind(Next).UnwrapError();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task GuardedThenMapped_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                if (!r.HasValue())
                    return 0;
                return r.Map(v => v * 2).UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Field_Guarded_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                if (_field.HasValue())
                    return _field.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task StaticField_Guarded_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                if (_staticField.HasValue())
                    return _staticField.UnwrapValue();
                return 0;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Parameter_Guarded_IsClean()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(Result<int, string> r) => r.HasValue() ? r.UnwrapValue() : 0;
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Property_IsNotTracked_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                if (Prop.HasValue())
                    return Prop.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task FieldOfAnotherInstance_IsNotTracked_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(Fixture other)
            {
                if (other._field.HasValue())
                    return other._field.UnwrapValue();
                return 0;
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RefArgument_IsForgottenAfterTheCall_NotWhenItIsEvaluated()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                TouchAndTake(ref r, r = Result<int, string>.Value(2));
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RefArgument_UnwrapInLaterArgument_RunsBeforeTheCall()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            void M()
            {
                Result<int, string> r = 1;
                TouchAndTake(ref r, r.UnwrapValue());
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task RefLocalAlias_MakesTheVariableUntracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                ref Result<int, string> alias = ref r;
                alias = Other();
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RefLocal_IsNotTracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = Other();
                ref Result<int, string> alias = ref r;
                alias = Result<int, string>.Value(1);
                r = Other();
                return alias.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AssignedInsideLocalFunction_MakesTheVariableUntracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                void Reset() => r = Other();
                Reset();
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task AssignedInsideLambda_MakesTheVariableUntracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                Action reset = () => r = Other();
                reset();
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task ReadInsideLambda_KeepsTheVariableTracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                Func<bool> peek = () => r.HasValue();
                peek();
                return r.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReassignedFromTernary_IsApplied()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                r = Coin() ? Result<int, string>.Error("a") : Result<int, string>.Error("b");
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RefArgument_NextToBranchingArgument_IsForgotten()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = 1;
                TouchAndTake(ref r, Coin() ? 1 : 2);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task ChainedAssignment_CarriesTheValue()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> a, b;
                a = b = Result<int, string>.Value(1);
                return a.UnwrapValue() + b.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AssignmentTarget_IsEvaluatedBeforeTheValue()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            void M(int[] array)
            {
                Result<int, string> r = "error";
                array[r.UnwrapValue()] = (r = Result<int, string>.Value(0)).ValueOr(0);
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task ConditionalRefAlias_MakesBothVariablesUntracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> a = 1, b = 2;
                ref Result<int, string> alias = ref (Coin() ? ref a : ref b);
                alias = Other();
                return a.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task RefParameters_ThatMayAliasEachOther_AreNotTracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            static int Read(ref Result<int, string> a, ref Result<int, string> b)
            {
                a = 1;
                b = Other();
                return a.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task SingleRefParameter_IsTracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            static int Read(ref Result<int, string> a)
            {
                a = 1;
                return a.UnwrapValue();
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DynamicCall_WithRefArgument_IsForgotten()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(dynamic writer)
            {
                Result<int, string> r = 1;
                writer.Reset(ref r);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task ConditionalRefWrite_WithAnUntrackedAlternative_IsNotDefinite()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(Result<int, string>[] slots)
            {
                Result<int, string> r = "failure";
                (Coin() ? ref r : ref slots[0]) = Result<int, string>.Value(1);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task DynamicCall_WithOutArgument_IsForgotten()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(dynamic writer)
            {
                Result<int, string> r = 1;
                writer.Fill(out r);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task DynamicObjectCreation_WithRefArgument_IsForgotten()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            class Holder
            {
                public Holder(dynamic ignored, ref Result<int, string> r) => r = Other();
            }

            int M(dynamic argument)
            {
                Result<int, string> r = 1;
                var holder = new Holder(argument, ref r);
                return r.UnwrapValue();
            }
            """);

        Assert.Single(diagnostics);
    }
}
