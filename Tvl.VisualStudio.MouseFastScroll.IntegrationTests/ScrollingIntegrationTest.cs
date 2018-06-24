﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.MouseFastScroll.IntegrationTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Text.Formatting;
    using Tvl.VisualStudio.MouseFastScroll.IntegrationTests.Harness;
    using Tvl.VisualStudio.MouseFastScroll.IntegrationTests.InProcess;
    using Tvl.VisualStudio.MouseFastScroll.IntegrationTests.Threading;
    using WindowsInput.Native;
    using Xunit;
    using Xunit.Abstractions;
    using _DTE = EnvDTE._DTE;
    using DTE = EnvDTE.DTE;
    using ServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;
    using vsSaveChanges = EnvDTE.vsSaveChanges;

    [Collection(nameof(SharedIntegrationHostFixture))]
    public class ScrollingIntegrationTest
    {
        public ScrollingIntegrationTest(ITestOutputHelper testOutputHelper, VisualStudioInstanceFactory instanceFactory)
        {
            TestOutputHelper = testOutputHelper;
            Editor = Editor_InProc.Create();
            SendKeys = new IdeSendKeys();
        }

        protected ITestOutputHelper TestOutputHelper
        {
            get;
        }

        private Editor_InProc Editor
        {
            get;
        }

        private IdeSendKeys SendKeys
        {
            get;
        }

        [IdeFact]
        public async Task BasicScrollingBehaviorAsync()
        {
            var dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(_DTE));
            var window = dte.ItemOperations.NewFile(Name: Guid.NewGuid() + ".txt");

            string initialText = string.Join(string.Empty, Enumerable.Range(0, 400).Select(i => Guid.NewGuid() + Environment.NewLine));
            await Task.Run(() => Editor.SetText(initialText));

            string additionalTypedText = Guid.NewGuid().ToString() + "\n" + Guid.NewGuid().ToString();
            await Task.Run(() => Editor.Activate());
            await SendKeys.SendAsync(additionalTypedText);

            string expected = initialText + additionalTypedText.Replace("\n", Environment.NewLine);
            Assert.Equal(expected, await Task.Run(() => Editor.GetText()));

            Assert.Equal(expected.Length, await Task.Run(() => Editor.GetCaretPosition()));

            // Move the caret and verify the final position. Note that the MoveCaret operation does not scroll the view.
            int firstVisibleLine = await Task.Run(() => Editor.GetFirstVisibleLine());
            Assert.True(firstVisibleLine > 0, "Expected the view to start after the first line at this point.");
            await Task.Run(() => Editor.MoveCaret(0));
            Assert.Equal(0, await Task.Run(() => Editor.GetCaretPosition()));
            Assert.Equal(firstVisibleLine, await Task.Run(() => Editor.GetFirstVisibleLine()));

            await SendKeys.SendAsync(inputSimulator =>
            {
                inputSimulator.Keyboard
                    .KeyDown(VirtualKeyCode.CONTROL)
                    .KeyPress(VirtualKeyCode.HOME)
                    .KeyUp(VirtualKeyCode.CONTROL);
            });

            Assert.True(await Task.Run(() => Editor.IsCaretOnScreen()));
            firstVisibleLine = await Task.Run(() => Editor.GetFirstVisibleLine());
            Assert.Equal(0, firstVisibleLine);

            int lastVisibleLine = await Task.Run(() => Editor.GetLastVisibleLine());
            var lastVisibleLineState = (VisibilityState)await Task.Run(() => Editor.GetLastVisibleLineState());
            Assert.True(firstVisibleLine < lastVisibleLine);

            Point point = await Task.Run(() => Editor.GetCenterOfEditorOnScreen());
            TestOutputHelper.WriteLine($"Moving mouse to ({point.X}, {point.Y}) and scrolling down.");
            int horizontalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int verticalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            point = new ScaleTransform(65535.0 / horizontalResolution, 65535.0 / verticalResolution).Transform(point);
            TestOutputHelper.WriteLine($"Screen resolution of ({horizontalResolution}, {verticalResolution}) translates mouse to ({point.X}, {point.Y}).");

            await SendKeys.SendAsync(inputSimulator =>
            {
                inputSimulator.Mouse
                    .MoveMouseTo(point.X, point.Y)
                    .VerticalScroll(-1);
            });

            Assert.Equal(0, await Task.Run(() => Editor.GetCaretPosition()));
            Assert.Equal(3, await Task.Run(() => Editor.GetFirstVisibleLine()));

            await SendKeys.SendAsync(inputSimulator =>
            {
                inputSimulator.Mouse
                    .MoveMouseTo(point.X, point.Y)
                    .VerticalScroll(1);
            });

            Assert.Equal(0, await Task.Run(() => Editor.GetCaretPosition()));
            Assert.Equal(0, await Task.Run(() => Editor.GetFirstVisibleLine()));

            await SendKeys.SendAsync(inputSimulator =>
            {
                inputSimulator
                    .Mouse.MoveMouseTo(point.X, point.Y)
                    .Keyboard.KeyDown(VirtualKeyCode.CONTROL)
                    .Mouse.VerticalScroll(-1)
                    .Keyboard.Sleep(10).KeyUp(VirtualKeyCode.CONTROL);
            });

            int expectedLastVisibleLine = lastVisibleLine + (lastVisibleLineState == VisibilityState.FullyVisible ? 1 : 0);
            Assert.Equal(0, await Task.Run(() => Editor.GetCaretPosition()));
            Assert.Equal(expectedLastVisibleLine, await Task.Run(() => Editor.GetFirstVisibleLine()));

            await SendKeys.SendAsync(inputSimulator =>
            {
                inputSimulator
                    .Mouse.MoveMouseTo(point.X, point.Y)
                    .Keyboard.KeyDown(VirtualKeyCode.CONTROL)
                    .Mouse.VerticalScroll(1)
                    .Keyboard.Sleep(10).KeyUp(VirtualKeyCode.CONTROL);
            });

            Assert.Equal(0, await Task.Run(() => Editor.GetCaretPosition()));
            Assert.Equal(0, await Task.Run(() => Editor.GetFirstVisibleLine()));

            window.Close(vsSaveChanges.vsSaveChangesNo);
        }

        /////// <summary>
        /////// Verifies that the Ctrl+Scroll operations do not change the zoom level in the editor.
        /////// </summary>
        ////[IdeFact]
        ////public void ZoomDisabled()
        ////{
        ////    var window = VisualStudio.Dte.ItemOperations.NewFile(Name: Guid.NewGuid() + ".txt");

        ////    string initialText = string.Join(string.Empty, Enumerable.Range(0, 400).Select(i => Guid.NewGuid() + Environment.NewLine));
        ////    VisualStudio.Editor.SetText(initialText);

        ////    string additionalTypedText = Guid.NewGuid().ToString() + "\n" + Guid.NewGuid().ToString();
        ////    VisualStudio.Editor.SendKeys(additionalTypedText);

        ////    string expected = initialText + additionalTypedText.Replace("\n", Environment.NewLine);
        ////    Assert.Equal(expected, VisualStudio.Editor.GetText());

        ////    Assert.Equal(expected.Length, VisualStudio.Editor.GetCaretPosition());

        ////    VisualStudio.SendKeys.Send(inputSimulator =>
        ////    {
        ////        inputSimulator.Keyboard
        ////            .KeyDown(VirtualKeyCode.CONTROL)
        ////            .KeyPress(VirtualKeyCode.HOME)
        ////            .KeyUp(VirtualKeyCode.CONTROL);
        ////    });

        ////    int firstVisibleLine = VisualStudio.Editor.GetFirstVisibleLine();
        ////    Assert.Equal(0, firstVisibleLine);

        ////    int lastVisibleLine = VisualStudio.Editor.GetLastVisibleLine();
        ////    VisibilityState lastVisibleLineState = VisualStudio.Editor.GetLastVisibleLineState();
        ////    Assert.True(firstVisibleLine < lastVisibleLine);

        ////    double zoomLevel = VisualStudio.Editor.GetZoomLevel();

        ////    Point point = VisualStudio.Editor.GetCenterOfEditorOnScreen();
        ////    int horizontalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        ////    int verticalResolution = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
        ////    point = new ScaleTransform(65535.0 / horizontalResolution, 65535.0 / verticalResolution).Transform(point);

        ////    VisualStudio.SendKeys.Send(inputSimulator =>
        ////    {
        ////        inputSimulator
        ////            .Mouse.MoveMouseTo(point.X, point.Y)
        ////            .Keyboard.KeyDown(VirtualKeyCode.CONTROL)
        ////            .Mouse.VerticalScroll(-1)
        ////            .Keyboard.Sleep(10).KeyUp(VirtualKeyCode.CONTROL);
        ////    });

        ////    int expectedLastVisibleLine = lastVisibleLine + (lastVisibleLineState == VisibilityState.FullyVisible ? 1 : 0);
        ////    Assert.Equal(0, VisualStudio.Editor.GetCaretPosition());
        ////    Assert.Equal(expectedLastVisibleLine, VisualStudio.Editor.GetFirstVisibleLine());
        ////    Assert.Equal(zoomLevel, VisualStudio.Editor.GetZoomLevel());

        ////    VisualStudio.SendKeys.Send(inputSimulator =>
        ////    {
        ////        inputSimulator
        ////            .Mouse.MoveMouseTo(point.X, point.Y)
        ////            .Keyboard.KeyDown(VirtualKeyCode.CONTROL)
        ////            .Mouse.VerticalScroll(1)
        ////            .Keyboard.Sleep(10).KeyUp(VirtualKeyCode.CONTROL);
        ////    });

        ////    Assert.Equal(0, VisualStudio.Editor.GetCaretPosition());
        ////    Assert.Equal(0, VisualStudio.Editor.GetFirstVisibleLine());
        ////    Assert.Equal(zoomLevel, VisualStudio.Editor.GetZoomLevel());

        ////    window.Close(vsSaveChanges.vsSaveChangesNo);
        ////}

        ////[VersionTrait(typeof(VS2012))]
        ////public sealed class VS2012 : TrivialIntegrationTest
        ////{
        ////    public VS2012(ITestOutputHelper testOutputHelper, VisualStudioInstanceFactory instanceFactory)
        ////        : base(testOutputHelper, instanceFactory, Versions.VisualStudio2012)
        ////    {
        ////    }
        ////}

        ////[VersionTrait(typeof(VS2013))]
        ////public sealed class VS2013 : TrivialIntegrationTest
        ////{
        ////    public VS2013(ITestOutputHelper testOutputHelper, VisualStudioInstanceFactory instanceFactory)
        ////        : base(testOutputHelper, instanceFactory, Versions.VisualStudio2013)
        ////    {
        ////    }
        ////}

        ////[VersionTrait(typeof(VS2015))]
        ////public sealed class VS2015 : TrivialIntegrationTest
        ////{
        ////    public VS2015(ITestOutputHelper testOutputHelper, VisualStudioInstanceFactory instanceFactory)
        ////        : base(testOutputHelper, instanceFactory, Versions.VisualStudio2015)
        ////    {
        ////    }
        ////}

        ////[VersionTrait(typeof(VS2017))]
        ////public sealed class VS2017 : TrivialIntegrationTest
        ////{
        ////    public VS2017(ITestOutputHelper testOutputHelper, VisualStudioInstanceFactory instanceFactory)
        ////        : base(testOutputHelper, instanceFactory, Versions.VisualStudio2017)
        ////    {
        ////    }
        ////}
    }
}