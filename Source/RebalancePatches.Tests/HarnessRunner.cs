using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimTestRedux;
using Verse;

namespace RebalancePatches.Tests
{
    internal static class HarnessRunner
    {
        internal sealed class Result
        {
            /// <summary>"rimtest" or "fallback" - which runner actually executed the suites.</summary>
            public string runner = "rimtest";

            /// <summary>Why the fallback was used, or what went wrong on the way. Null when clean.</summary>
            public string note;

            /// <summary>RimTest's own verdict for our assembly, when it could be read.</summary>
            public string runnerStatus;

            public int suitesFound;
            public int testsFound;

            public List<TestCoverage.FailureRecord> fallbackFailures = new List<TestCoverage.FailureRecord>();
        }

        public static Result Run()
        {
            Assembly ours = typeof(HarnessRunner).Assembly;
            var result = new Result();
            CountSuites(ours, result);

            if (Harness.SelfTest == Harness.SelfTestFallback)
            {
                Fallback(ours, result, "forced by -rbpselftest=fallback");
                return result;
            }

            MethodInfo runAssembly = AccessTools.Method(
                AccessTools.TypeByName("RimTestRedux.Testing.Runner"), "RunAssembly", new[] { typeof(Assembly) });
            if (runAssembly == null)
            {
                Fallback(ours, result, "RimTestRedux.Testing.Runner.RunAssembly(Assembly) not found - "
                    + "RimTest Redux moved or renamed its API");
                return result;
            }

            try
            {
                runAssembly.Invoke(null, new object[] { ours });
            }
            catch (Exception e)
            {
                Fallback(ours, result, "RimTest's runner threw: " + (e.InnerException ?? e));
                return result;
            }

            TestCoverage.Snapshot after = TestCoverage.Take();
            if (after.seen == 0 && result.testsFound > 0)
            {
                Fallback(ours, result, $"RimTest ran no tests although this assembly declares "
                    + $"{result.testsFound} - it did not register our suites");
                return result;
            }

            result.runnerStatus = AssemblyStatusOf(ours);
            return result;
        }

        private static string AssemblyStatusOf(Assembly ours)
        {
            try
            {
                MethodInfo get = AccessTools.Method(
                    AccessTools.TypeByName("RimTestRedux.Testing.AssemblyExplorer"),
                    "GetAssemblyStatus", new[] { typeof(Assembly) });
                return get?.Invoke(null, new object[] { ours })?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static void CountSuites(Assembly ours, Result result)
        {
            foreach (Type type in SuitesIn(ours))
            {
                result.suitesFound++;
                foreach (MethodInfo unused in TestsIn(type))
                    result.testsFound++;
            }
        }

        private static void Fallback(Assembly ours, Result result, string why)
        {
            result.runner = "fallback";
            result.note = why;
            Log.Warning("[RBP Harness] Falling back to the built-in runner: " + why);
            foreach (Type type in SuitesIn(ours))
            {
                foreach (MethodInfo test in TestsIn(type))
                {
                    int failedBefore = TestCoverage.Take().failed;
                    try
                    {
                        test.Invoke(null, null);
                    }
                    catch (Exception e)
                    {
                        if (TestCoverage.Take().failed != failedBefore)
                            continue;
                        Exception actual = e is TargetInvocationException && e.InnerException != null
                            ? e.InnerException
                            : e;
                        result.fallbackFailures.Add(new TestCoverage.FailureRecord
                        {
                            test = $"{type.Name}.{test.Name}",
                            messages = { actual.Message },
                        });
                    }
                }
            }
        }

        private static IEnumerable<Type> SuitesIn(Assembly ours)
        {
            foreach (Type type in ours.GetTypes())
                if (type.GetCustomAttribute<TestSuiteAttribute>() != null)
                    yield return type;
        }

        private static IEnumerable<MethodInfo> TestsIn(Type suite)
        {
            foreach (MethodInfo method in suite.GetMethods(BindingFlags.Public | BindingFlags.Static))
                if (method.GetCustomAttribute<TestAttribute>() != null && method.GetParameters().Length == 0)
                    yield return method;
        }
    }
}
