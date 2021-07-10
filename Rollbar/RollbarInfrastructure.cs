﻿namespace Rollbar
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    using Rollbar.Diagnostics;
    using Rollbar.PayloadStore;

    public class RollbarInfrastructure
        : IDisposable

    {
        private static readonly TraceSource traceSource = new TraceSource(typeof(RollbarInfrastructure).FullName);

        internal readonly TimeSpan _sleepInterval = TimeSpan.FromMilliseconds(25);

        private readonly object _syncLock = new object();

        private Thread _infrastructureThread;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _isInitialized = false;

        private IRollbarInfrastructureConfig _config = null;

        private IPayloadStoreRepository _storeRepository = null;



        #region singleton implementation

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static RollbarInfrastructure Instance
        {
            get
            {
                return NestedSingleInstance.Instance;
            }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="RollbarInfrastructure" /> class from being created.
        /// </summary>
        private RollbarInfrastructure()
        {
            traceSource.TraceInformation($"Creating the {typeof(RollbarInfrastructure).Name}...");
        }

        /// <summary>
        /// Class NestedSingleInstance. This class cannot be inherited.
        /// </summary>
        private sealed class NestedSingleInstance
        {
            /// <summary>
            /// Prevents a default instance of the <see cref="NestedSingleInstance"/> class from being created.
            /// </summary>
            private NestedSingleInstance()
            {
            }

            /// <summary>
            /// The instance
            /// </summary>
            internal static readonly RollbarInfrastructure Instance =
                new RollbarInfrastructure();
        }

        #endregion singleton implementation

        public bool IsInitialized
        {
            get
            {
                return this._isInitialized;
            }
        }

        public IRollbarInfrastructureConfig Config
        {
            get
            {
                return this._config;
            }
        }

        public void Init(IRollbarInfrastructureConfig config)
        {
            Assumption.AssertNotNull(config, nameof(config));

            lock(this._syncLock)
            {
                if(this._isInitialized)
                {
                    string msg = $"{typeof(RollbarInfrastructure).Name} can not be initialized more than once!";
                    traceSource.TraceInformation(msg);
                    throw new RollbarException(
                        InternalRollbarError.InfrastructureError, 
                        msg
                        );
                }

                //TODO: implement...
                this._config = config;
                this._config.Reconfigured += _config_Reconfigured;
                try
                {
                    RollbarQueueController.Instance.Init(config);
                    //TODO: RollbarConfig
                    // - init RollbarTelemetry service as needed
                    // - init ConnectivityMonitor service as needed
                }
                catch(Exception ex)
                {
                    throw new RollbarException(
                        InternalRollbarError.InfrastructureError, 
                        "Exception while initializing the internal services!", 
                        ex
                        );
                }

                this._isInitialized = true;
            }
        }

        private void _config_Reconfigured(object sender, EventArgs e)
        {
            //TODO: RollbarConfig - implement
            //throw new NotImplementedException();
        }

        public void Start()
        {
            traceSource.TraceInformation($"Starting the {typeof(RollbarInfrastructure).Name}...");
        }

        public void Stop(bool immediate)
        {
            traceSource.TraceInformation($"Stopping the {typeof(RollbarInfrastructure).Name}...");

            if(!immediate && this._cancellationTokenSource != null)
            {
                this._cancellationTokenSource.Cancel();
                return;
            }

            this._cancellationTokenSource?.Cancel();
            if(this._infrastructureThread != null)
            {

                if(!this._infrastructureThread.Join(TimeSpan.FromSeconds(60)))
                {
                    this._infrastructureThread.Abort();
                }

                CompleteProcessing();
            }
        }
        private void CompleteProcessing()
        {
            Debug.WriteLine("Entering " + this.GetType().FullName + "." + nameof(this.CompleteProcessing) + "() method...");
            traceSource.TraceInformation($"{typeof(RollbarInfrastructure).Name} is completing processing...");
        }


        #region IDisposable Support

        /// <summary>
        /// The disposed value
        /// </summary>
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if(!disposedValue)
            {
                if(disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    CompleteProcessing();
                    this._config.Reconfigured -= _config_Reconfigured;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RollbarQueueController() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>This code added to correctly implement the disposable pattern.</remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support


    }
}