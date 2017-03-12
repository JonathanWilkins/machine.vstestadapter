﻿using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Machine.VSTestAdapter.Execution;
using Machine.VSTestAdapter.Helpers;
using Machine.VSTestAdapter.Configuration;

namespace Machine.VSTestAdapter
{
    public partial class MSpecTestAdapter : ITestExecutor
    {
        public void Cancel()
        {
            // Not supported
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            //Debugger.Launch();

            frameworkHandle.SendMessage(TestMessageLevel.Informational, Strings.EXECUTOR_STARTING);

            Settings settings = GetSettings(runContext);

            foreach (string currentAsssembly in sources.Distinct())
            {
                try
                {
#if !NETSTANDARD
                    if (!File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(currentAsssembly)), "Machine.Specifications.dll")))
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Informational, String.Format("Machine.Specifications.dll not found for {0}", currentAsssembly));
                        continue;
                    }
#endif

                    frameworkHandle.SendMessage(TestMessageLevel.Informational, String.Format(Strings.EXECUTOR_EXECUTINGIN, currentAsssembly));

                    this.executor.RunAssembly(currentAsssembly, settings, uri, frameworkHandle);
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, String.Format(Strings.EXECUTOR_ERROR, currentAsssembly, ex.Message));
                }
            }

            frameworkHandle.SendMessage(TestMessageLevel.Informational, String.Format("Complete on {0} assemblies ", sources.Count()));
            
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            //Debugger.Launch();

            frameworkHandle.SendMessage(TestMessageLevel.Informational, Strings.EXECUTOR_STARTING);

            int executedSpecCount = 0;

            Settings settings = GetSettings(runContext);

            string currentAsssembly = string.Empty;
            try
            {

                foreach (IGrouping<string, TestCase> grouping in tests.GroupBy(x => x.Source)) {
                    currentAsssembly = grouping.Key;
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format(Strings.EXECUTOR_EXECUTINGIN, currentAsssembly));

                    List<VisualStudioTestIdentifier> testsToRun = grouping.Select(test => test.ToVisualStudioTestIdentifier()).ToList();

                    this.executor.RunAssemblySpecifications(currentAsssembly, testsToRun, settings, uri, frameworkHandle);
                    executedSpecCount += grouping.Count();
                }

                frameworkHandle.SendMessage(TestMessageLevel.Informational, String.Format(Strings.EXECUTOR_COMPLETE, executedSpecCount, tests.GroupBy(x => x.Source).Count()));
            } catch (Exception ex)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, string.Format(Strings.EXECUTOR_ERROR, currentAsssembly, ex.Message));
            }
            finally
            {
            }
        }

        private static Settings GetSettings(IDiscoveryContext runContext)
        {
            return Settings.Parse(runContext?.RunSettings?.SettingsXml);
        }
    }
}