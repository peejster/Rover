using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

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
            await ultrasonicDistanceSensor.InitAsync();

            WriteLog("Moving forward");

            while (!_finish)
            {
                try
                {
                    driver.MoveForward();

                    await Task.Delay(200);

                    var distance = await ultrasonicDistanceSensor.GetDistanceInCmAsync(1000);
                    WriteData("Forward", distance);
                    if (distance > 35.0)
                        continue;

                    WriteLog($"Obstacle found at {distance:F2} cm or less. Turning right");
                    WriteData("Turn Right", distance);

                    await driver.TurnRightAsync();

                    WriteLog("Moving forward");
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                    driver.Stop();
                    WriteData("Stop", -1);
                }
            }
        }

        private async void WriteLog(string text)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Log.Text += $"{text} | ";
            });
        }

        private async void WriteData(string move, double distance)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                System.Diagnostics.Debug.WriteLine($"{move} {distance} cm");
                ForwardImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                TurnRightImage.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                switch (move)
                {
                    case "Forward":
                        ForwardImage.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                    case "Turn Right":
                        TurnRightImage.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                }
                Distance.Text = $"{distance:F2} cm";
            });
        }
    }
}