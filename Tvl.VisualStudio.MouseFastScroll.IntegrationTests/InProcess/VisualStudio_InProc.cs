﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.MouseFastScroll.IntegrationTests.InProcess
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using Tvl.VisualStudio.MouseFastScroll.IntegrationTests.Harness;
    using Command = EnvDTE.Command;
    using DTE2 = EnvDTE80.DTE2;
    using File = System.IO.File;
    using Path = System.IO.Path;
    using vsBuildErrorLevel = EnvDTE80.vsBuildErrorLevel;

    internal partial class VisualStudio_InProc : InProcComponent
    {
        private VisualStudio_InProc()
        {
        }

        public static VisualStudio_InProc Create()
            => new VisualStudio_InProc();

        public new void WaitForApplicationIdle()
            => InProcComponent.WaitForApplicationIdle();

        public new void WaitForSystemIdle()
            => InProcComponent.WaitForSystemIdle();

        public new bool IsCommandAvailable(string commandName)
            => InProcComponent.IsCommandAvailable(commandName);

        public new void ExecuteCommand(string commandName, string args = "")
            => InProcComponent.ExecuteCommand(commandName, args);

        public void AddCodeBaseDirectory(string directory)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string path = Path.Combine(directory, new AssemblyName(e.Name).Name + ".dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }

                return null;
            };
        }

        public string[] GetAvailableCommands()
        {
            List<string> result = new List<string>();
            var commands = GetDTE().Commands;
            foreach (Command command in commands)
            {
                try
                {
                    string commandName = command.Name;
                    if (command.IsAvailable)
                    {
                        result.Add(commandName);
                    }
                }
                finally
                {
                }
            }

            return result.ToArray();
        }

        public void ActivateMainWindow(bool skipAttachingThreads = false)
            => InvokeOnUIThread(() =>
            {
                var dte = GetDTE();

                var activeVisualStudioWindow = (IntPtr)dte.ActiveWindow.HWnd;
                Debug.WriteLine($"DTE.ActiveWindow.HWnd = {activeVisualStudioWindow}");

                if (activeVisualStudioWindow == IntPtr.Zero)
                {
                    activeVisualStudioWindow = (IntPtr)dte.MainWindow.HWnd;
                    Debug.WriteLine($"DTE.MainWindow.HWnd = {activeVisualStudioWindow}");
                }

                IntegrationHelper.SetForegroundWindow(activeVisualStudioWindow, skipAttachingThreads);
            });

        public int GetErrorListErrorCount()
        {
            var dte = (DTE2)GetDTE();
            var errorList = dte.ToolWindows.ErrorList;

            var errorItems = errorList.ErrorItems;
            var errorItemsCount = errorItems.Count;

            var errorCount = 0;

            try
            {
                for (var index = 1; index <= errorItemsCount; index++)
                {
                    var errorItem = errorItems.Item(index);

                    if (errorItem.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                    {
                        errorCount += 1;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return GetErrorListErrorCount();
            }

            return errorCount;
        }

        public void WaitForNoErrorsInErrorList()
        {
            while (GetErrorListErrorCount() != 0)
            {
                System.Threading.Thread.Yield();
            }
        }

        public void Quit()
            => GetDTE().Quit();
    }
}
