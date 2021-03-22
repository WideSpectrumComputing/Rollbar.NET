﻿namespace UnitTest.Rollbar
{
    using System;
    using System.Diagnostics;

    public static class ExceptionSimulator
    {
        public static Exception GetExceptionChainOf(int totalExceptions)
        {
            while (--totalExceptions > 0)
            {
                return new ApplicationException("Outer exception #" + totalExceptions, GetExceptionChainOf(totalExceptions));
            }
            const int totalMostInnerExceptionFrames = 5;
            return GetExceptionWith(totalMostInnerExceptionFrames);
        }

        public static Exception GetExceptionWith(int totalFrames, string exceptionMessage = "Default exception message.")
        {
            try
            {
                Method(totalFrames, exceptionMessage);
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        private static void Method(int callDepth, string exceptionMessage)
        {
            int currentMethodCallID = callDepth; // just to simulate a local variable...

            while (--callDepth > 0)
            {
                Method(callDepth, exceptionMessage);
            }
            ExceptionalMethod(exceptionMessage);
        }

        private static void ExceptionalMethod()
        {
            ExceptionalMethod("Some nasty application exception!");
        }

        private static void ExceptionalMethod(string exceptionMessage)
        {
            throw new ApplicationException(exceptionMessage);
        }
    }
}
