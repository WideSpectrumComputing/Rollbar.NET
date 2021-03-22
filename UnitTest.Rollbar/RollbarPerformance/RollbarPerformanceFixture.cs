﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#if INSTRUMENT
namespace UnitTest.Rollbar.RollbarPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Diagnostics;

    using global::Rollbar;
    using global::Rollbar.Instrumentation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Net.Http;
    using System.Threading;
    using global::Rollbar.Classification;
    using global::Rollbar.Common;
    using global::Rollbar.Diagnostics;
    using global::Rollbar.DTOs;


    [TestClass]
    [TestCategory(nameof(RollbarPerformanceFixture))]
    public class RollbarPerformanceFixture
    {
        private int _apiErrorsCount = 0;
        private int _commErrorsCount = 0;
        private int _internalErrorsCount = 0;
        private int _commSuccessCount = 0;

        private void ResetAllCounts()
        {
            _apiErrorsCount = 0;
            _commErrorsCount = 0;
            _internalErrorsCount = 0;
            _commSuccessCount = 0;
        }

        [TestInitialize]
        public void SetupFixture()
        {
            //SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        [TestCleanup]
        public void TearDownFixture()
        {

        }

        [TestMethod]
        [Conditional(InstrumentationCondition.Instrument)]
        public void EvaluatePerformance()
        {
            ResetAllCounts();

            RollbarQueueController.Instance.InternalEvent += Instance_InternalEvent;
            ClassificationDeclaration classificationDeclaration = new ClassificationDeclaration();
            foreach(var classifier in EnumUtility.GetAllValues<PayloadSize>())
            {
                EvaluateUsing(classifier, classificationDeclaration);
            }
            BlockUntilRollbarQueuesAreEmpty();
            RollbarQueueController.Instance.InternalEvent -= Instance_InternalEvent;

            // Any SDK error would invalidate the test:
            Assert.AreEqual(0, this._apiErrorsCount);
            Assert.AreEqual(0, this._commErrorsCount);
            Assert.AreEqual(0, this._internalErrorsCount);

            // Let's make sure expected amount of payloads was 
            // delivered to the Rollbar API:
            Assert.AreEqual(
                Constants.TotalMeasurementSamples
                * EnumUtility.GetAllValues<PayloadSize>().Length 
                * EnumUtility.GetAllValues<PayloadType>().Length 
                * (EnumUtility.GetAllValues<MethodVariant>().Length - 1) 
                * EnumUtility.GetAllValues<Method>().Length 
                , this._commSuccessCount
                );
        }

        private void Instance_InternalEvent(object sender, RollbarEventArgs e)
        {
            //Console.WriteLine(e.TraceAsString());

            RollbarApiErrorEventArgs apiErrorEvent = e as RollbarApiErrorEventArgs;
            if (apiErrorEvent != null)
            {
                _apiErrorsCount++;
                return;
            }
            CommunicationEventArgs commEvent = e as CommunicationEventArgs;
            if (commEvent != null)
            {
                _commSuccessCount++;
                return;
            }
            CommunicationErrorEventArgs commErrorEvent = e as CommunicationErrorEventArgs;
            if (commErrorEvent != null)
            {
                _commErrorsCount++;
                return;
            }
            InternalErrorEventArgs internalErrorEvent = e as InternalErrorEventArgs;
            if (internalErrorEvent != null)
            {
                _internalErrorsCount++;
                return;
            }
        }

        private void EvaluateUsing(PayloadSize theClassifier, ClassificationDeclaration classificationDeclaration)
        {
            classificationDeclaration.PayloadSize = theClassifier;
            foreach (var classifier in EnumUtility.GetAllValues<PayloadType>())
            {
                EvaluateUsing(classifier, classificationDeclaration);
            }
        }

        private void EvaluateUsing(PayloadType theClassifier, ClassificationDeclaration classificationDeclaration)
        {
            classificationDeclaration.PayloadType = theClassifier;
            foreach (var classifier in EnumUtility.GetAllValues<MethodVariant>())
            {
                EvaluateUsing(classifier, classificationDeclaration);
            }
        }

        private void EvaluateUsing(MethodVariant theClassifier, ClassificationDeclaration classificationDeclaration)
        {
            classificationDeclaration.MethodVariant = theClassifier;
            foreach (var classifier in EnumUtility.GetAllValues<Method>())
            {
                EvaluateUsing(classifier, classificationDeclaration);
            }
        }

        private void EvaluateUsing(Method theClassifier, ClassificationDeclaration classificationDeclaration)
        {
            classificationDeclaration.Method = theClassifier;

            switch(classificationDeclaration.Method)
            {
                case Method.Log:
                    EvaluateLogMethod(classificationDeclaration);
                    break;
                default:
                    Assumption.FailValidation("Unexpected value!", nameof(classificationDeclaration.Method));
                    break;
            }

        }

        private void EvaluateLogMethod(ClassificationDeclaration classificationDeclaration)
        {
            RollbarConfig loggerConfig = ProvideRollbarConfig();

            object payload = null;
            switch (classificationDeclaration.PayloadType)
            {
                case PayloadType.Message:
                    {
                        payload = ProvideObjectToLog(classificationDeclaration);
                    }
                    break;
                case PayloadType.Exception:
                    {
                        payload = ProvideObjectToLog(classificationDeclaration);
                    }
                    break;
                default:
                    break;
            }
            Assert.IsNotNull(payload);

            // Let's give things change to stabilize: 
            //Thread.Sleep(TimeSpan.FromSeconds(2));

            using (var rollbar = RollbarFactory.CreateNew().Configure(loggerConfig))
            {
                for (int i = 0; i < Constants.TotalMeasurementSamples; i++)
                {
                    switch (classificationDeclaration.MethodVariant)
                    {
                        case MethodVariant.Async:
                            {
                                ILogger logger = rollbar;
                                using (PerformanceUtil.GetPerformanceTimer(classificationDeclaration))
                                {
                                    logger.Log(ErrorLevel.Warning, payload);
                                }
                                break;
                            }
                        case MethodVariant.AsyncWaited:
                            {
                                ILogger logger = rollbar;
                                using (PerformanceUtil.GetPerformanceTimer(classificationDeclaration))
                                {
                                    //TODO: implement and add SynchronousPackage around the payload object!!!
                                    logger.Log(ErrorLevel.Warning, new ObjectPackage(payload, true));
                                    //logger.Log(ErrorLevel.Warning, payload);
                                }
                                break;
                            }
                        case MethodVariant.Blocking:
                            {
                                // NOTE: for blocking call we want to eliminate effect of 
                                // the max reporting rate restriction, so that the wait time 
                                // that is a result of the rate limit is not counted against
                                // the blocking call:
                                BlockUntilRollbarQueuesAreEmpty();
                                Thread.Sleep(TimeSpan.FromSeconds(60 / rollbar.Config.MaxReportsPerMinute));

                                ILogger logger = rollbar.AsBlockingLogger(Constants.RollbarBlockingTimeout);
                                using (PerformanceUtil.GetPerformanceTimer(classificationDeclaration))
                                {
                                    logger.Log(ErrorLevel.Critical, payload);
                                }
                                break;
                            }
                        default:
                            // nothing to do...
                            break;
                    }
                }

                //BlockUntilRollbarQueuesAreEmpty();
                //Thread.Sleep(TimeSpan.FromMilliseconds(250));
                //if (classificationDeclaration.MethodVariant == MethodVariant.Async)
                //{
                //    // NOTE: for async call we want to make sure the logger's instance is not
                //    // disposed until all the buffered payloads delivered to the Rollbar API.
                //    // This delay ios needed until issue #197 is resolved...
                //    BlockUntilRollbarQueuesAreEmpty();
                //}
            }
        }

        private void BlockUntilRollbarQueuesAreEmpty()
        {
            DateTime startedAt = DateTime.Now;
            while (RollbarQueueController.Instance.GetTotalPayloadCount() > 0)
            {
                if (DateTime.Now.Subtract(startedAt) > TimeSpan.FromMinutes(5))
                {
                    Assumption.FailValidation("Something wrong with blocking!", nameof(DateTime.Now));
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(250));
            }
        }

        private RollbarConfig ProvideRollbarConfig()
        {
            RollbarConfig loggerConfig = new RollbarConfig(Constants.RollbarAccessToken)
            {
                Environment = Constants.RollbarEnvironment,
            };

            Person person = new Person("RPE");
            person.Email = "rpe@RollbarPerformanceEvaluator.org";
            person.UserName = "RollbarPerformanceEvaluator";
            loggerConfig.Person = person;

            return loggerConfig;
        }

        private object ProvideObjectToLog(ClassificationDeclaration classificationDeclaration)
        {
            switch (classificationDeclaration.PayloadType)
            {
                case PayloadType.Message:
                    return ProvideMessage(classificationDeclaration.PayloadSize);
                case PayloadType.Exception:
                    return ProvideException(classificationDeclaration.PayloadSize);
                default:
                    Assumption.FailValidation("Unexpected value!", nameof(classificationDeclaration.PayloadType));
                    break;
            }
            return null;
        }

        private string ProvideMessage(PayloadSize payloadSize)
        {
            string smallMessage = @"Small message 1234 56789! ";
            StringBuilder result = new StringBuilder(smallMessage);

            int multiplier = 1;
            switch(payloadSize)
            {
                case PayloadSize.Small:
                    multiplier = 1;
                    break;
                case PayloadSize.Medium:
                    multiplier = 1 * Constants.SmallToMediumMessageMultiplier;
                    break;
                case PayloadSize.Large:
                    multiplier = 1 * Constants.SmallToMediumMessageMultiplier * Constants.MediumToLargeMessageMultiplier;
                    break;
                default:
                    Assumption.FailValidation("Unexpected value!", nameof(payloadSize));
                    break;
            }

            int counter = 0;
            while (++counter < multiplier)
            {
                result.AppendLine(smallMessage);
            }

            return result.ToString();
        }

        private System.Exception ProvideException(PayloadSize payloadSize)
        {
            const int totalExceptionFramesBaseline = 3;
            int multiplier = 1;
            switch (payloadSize)
            {
                case PayloadSize.Small:
                    multiplier = 1;
                    break;
                case PayloadSize.Medium:
                    multiplier = 1 * Constants.SmallToMediumExceptionCallStackDepthMultiplier;
                    break;
                case PayloadSize.Large:
                    multiplier = 1 * Constants.SmallToMediumExceptionCallStackDepthMultiplier * Constants.MediumToLargeExceptionCallStackDepthMultiplier;
                    break;
                default:
                    Assumption.FailValidation("Unexpected value!", nameof(payloadSize));
                    break;
            }

            return ExceptionSimulator.GetExceptionWith(multiplier * totalExceptionFramesBaseline);
        }


    }
}
#endif