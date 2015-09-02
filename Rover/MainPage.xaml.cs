using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Rover
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public sealed partial class MainPage : Page
    {

        private BackgroundWorker _worker;
        private CoreDispatcher _dispatcher;

        private bool _finish;

        public MainPage()
        {
            InitializeComponent();

            Loaded += MainPage_Loaded;

            Unloaded += MainPage_Unloaded;
        }

        private void MainPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            _worker = new BackgroundWorker();
            _worker.DoWork += DoWork;
            _worker.RunWorkerAsync();
        }

        private void MainPage_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            _finish = true;
        }

        private async void DoWork(object sender, DoWorkEventArgs e)
        {
            var driver = new TwoMotorsDriver(new Motor(27, 22), new Motor(5, 6));
            var ultrasonicDistanceSensor = new UltrasonicDistanceSensor(23, 24);

            await WriteLog("Moving forward");

            while (!_finish)
            {
                driver.MoveForward();

                await Task.Delay(200);

                var distance = await ultrasonicDistanceSensor.GetDistanceInCmAsync(1000);
                if (distance > 35.0)
                    continue;

                await WriteLog($"Obstacle found at {distance} cm or less. Turning right");

                await driver.TurnRightAsync();

                await WriteLog("Moving forward");
            }
        }

        private async Task WriteLog(string text)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Log.Text += $"{text} | ";
            });
        }
    }
}

