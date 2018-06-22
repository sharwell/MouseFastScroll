﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.MouseFastScroll.IntegrationTests.OutOfProcess
{
    using System;
    using System.Reflection;
    using Tvl.VisualStudio.MouseFastScroll.IntegrationTests.Harness;
    using Tvl.VisualStudio.MouseFastScroll.IntegrationTests.InProcess;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class TestInvoker_OutOfProc : OutOfProcComponent
    {
        internal TestInvoker_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            TestInvokerInProc = CreateInProcComponent<TestInvoker_InProc>(visualStudioInstance);
        }

        internal TestInvoker_InProc TestInvokerInProc
        {
            get;
        }

        public void LoadAssembly(string codeBase)
        {
            TestInvokerInProc.LoadAssembly(codeBase);
        }

        public Tuple<decimal, Exception> InvokeTest(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments)
        {
            return TestInvokerInProc.InvokeTest(
                test,
                messageBus,
                testClass,
                constructorArguments,
                testMethod,
                testMethodArguments);
        }
    }
}
