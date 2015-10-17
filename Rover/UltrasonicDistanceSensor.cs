using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Rover
{
    public class UltrasonicDistanceSensor
    {
        private readonly GpioPin _gpioPinTrig;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly GpioPin _gpioPinEcho;

        public UltrasonicDistanceSensor(int trigGpioPin, int echoGpioPin)
        {
            var gpio = GpioController.GetDefault();

            _gpioPinTrig = gpio.OpenPin(trigGpioPin);
            _gpioPinEcho = gpio.OpenPin(echoGpioPin);
            _gpioPinTrig.SetDriveMode(GpioPinDriveMode.Output);
            _gpioPinEcho.SetDriveMode(GpioPinDriveMode.Input);
            _gpioPinTrig.Write(GpioPinValue.Low);

        }

        public Task<double> GetDistanceInCmAsync(int timeoutInMilliseconds)
        {
            return Task.Run(() =>
            {
                double distance = double.MaxValue;
                // turn on the pulse
                _gpioPinTrig.Write(GpioPinValue.High);
                Task.Delay(TimeSpan.FromTicks(100)).Wait();
                _gpioPinTrig.Write(GpioPinValue.Low);

                SpinWait.SpinUntil(()=> { return _gpioPinEcho.Read() != GpioPinValue.Low; });
                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.ElapsedMilliseconds < timeoutInMilliseconds && _gpioPinEcho.Read() == GpioPinValue.High)
                {
                    distance = stopwatch.Elapsed.TotalSeconds * 17150;
                }
                stopwatch.Stop();
                return distance;
            });
        }
    }
}