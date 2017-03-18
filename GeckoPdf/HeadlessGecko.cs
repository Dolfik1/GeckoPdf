using Gecko;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeckoPdf
{
    /// <summary>
    /// Provides static methods and properties to work with gecko in "fake" window
    /// </summary>
    internal static class HeadlessGecko
    {
        
        #region Fields

        private static volatile bool _initialized = false;
        public static Thread _appThread;
        private static Control _invoker; // very simple method to create new browsers is to use Invoke of this control

        public delegate void InitializedEventHandler();
        public static event InitializedEventHandler OnInitialized;
        private static SemaphoreSlim _initializedSignal;
        private static string _xulPath;

        #endregion

        #region Methods

        /// <summary>
        /// Init xul and start message loop
        /// </summary>
        private static void InitXul()
        {
            _invoker = new Control();
            _invoker.CreateControl();

            Xpcom.AfterInitalization += () =>
            {
                _initialized = true;

                _initializedSignal?.Release();
                OnInitialized?.Invoke();
            };

            Xpcom.EnableProfileMonitoring = false;
            Xpcom.Initialize(_xulPath);
            Application.Run();
        }

        /// <summary>
        /// Creates new instance of GeckoWebBrowser object
        /// </summary>
        /// <returns></returns>
        private static GeckoWebBrowser CreateBrowserInternal()
        {
            GeckoWebBrowser browser = new GeckoWebBrowser();
            browser.CreateControl();
            
            return browser;
        }

        /// <summary>
        /// True if Xul initialized
        /// </summary>
        public static bool IsInitialized
        {
            get { return _initialized; }
        }

        /// <summary>
        /// Initialize xul
        /// </summary>
        public static void Initialize(string binDirectory)
        {
            if (_initialized)
                return;

            _xulPath = binDirectory;
            _appThread = new Thread(InitXul);
            _appThread.SetApartmentState(ApartmentState.STA);
            _appThread.Start();
        }

        /// <summary>
        /// Asynchronously initialize xul
        /// </summary>
        /// <returns></returns>
        public static async Task InitializeAsync(string binDirectory)
        {
            _initializedSignal = new SemaphoreSlim(0, 1);
            Initialize(binDirectory);

            await _initializedSignal.WaitAsync();
        }

        /// <summary>
        /// Creates new instance of GeckoWebBrowser object
        /// </summary>
        /// <returns></returns>
        public static GeckoWebBrowser CreateBrowser()
        {
            if (!_initialized)
                throw new InvalidOperationException("Xul is not initialized yet");

            return (GeckoWebBrowser)_invoker.Invoke(new Func<GeckoWebBrowser>(CreateBrowserInternal));
        }

        /// <summary>
        /// Unloads Gecko application
        /// </summary>
        public static void Unload()
        {
            Application.Exit();
        }

        #endregion
    }
}
