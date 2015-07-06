using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml.Controls;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Rover
{
    public sealed partial class MainPage : Page
    {
        // specify which GPIO pins are wired to the left motor
        private const int IN1 = 27;
        private const int IN2 = 22;
        private GpioPin leftMotorA;
        private GpioPin leftMotorB;

        //specify which GPIO pins are wired to the right motor
        private const int IN3 = 5;
        private const int IN4 = 6;
        private GpioPin rightMotorA;
        private GpioPin rightMotorB;

        // specify which GPIO pins are wired to the distance sensor
        private const int Trig_Pin = 23;
        private const int Echo_Pin = 24;
        private GpioPin trig;
        private GpioPin echo;

        // detect failures in the distance sensor
        bool echoFailure = false;

        // stopwatch to time the echo on the distance sensor
        Stopwatch sw = new Stopwatch();

        // duration of the echo
        TimeSpan elapsedTime;

        // distance between the rover and an obstacle
        double distanceToObstacle;

        public MainPage()
        {
            this.InitializeComponent();

            InitGPIO();

            // as long as the GPIO pins initialized properly, get moving
            while (leftMotorA != null)
            {
                // start moving forward
                MoveForward();

                // as long as there is an obstacle in the way
                while (ObstacleDetected())
                {
                    // initiate right turn
                    TurnRight();

                    // continue right turn for 250 milliseconds - a quarter turn
                    Task.Delay(TimeSpan.FromMilliseconds(250)).Wait();

                    // stop
                    FullStop();
                }
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                leftMotorA = null;
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            // initialize left motor
            leftMotorA = gpio.OpenPin(IN1);
            leftMotorB = gpio.OpenPin(IN2);
            leftMotorA.Write(GpioPinValue.Low);
            leftMotorB.Write(GpioPinValue.Low);
            leftMotorA.SetDriveMode(GpioPinDriveMode.Output);
            leftMotorB.SetDriveMode(GpioPinDriveMode.Output);

            // initialize right motor
            rightMotorA = gpio.OpenPin(IN3);
            rightMotorB = gpio.OpenPin(IN4);
            rightMotorA.Write(GpioPinValue.Low);
            rightMotorB.Write(GpioPinValue.Low);
            rightMotorA.SetDriveMode(GpioPinDriveMode.Output);
            rightMotorB.SetDriveMode(GpioPinDriveMode.Output);

            // initialize distance sensor
            trig = gpio.OpenPin(Trig_Pin);
            echo = gpio.OpenPin(Echo_Pin);
            trig.SetDriveMode(GpioPinDriveMode.Output);
            echo.SetDriveMode(GpioPinDriveMode.Input);
            trig.Write(GpioPinValue.Low);

            GpioStatus.Text = "GPIO pins initialized correctly.";
        }

        private void MoveForward()
        {
            // spin the left motor in the forward direction
            leftMotorA.Write(GpioPinValue.Low);
            leftMotorB.Write(GpioPinValue.High);

            // spin the right motor in the forward direction
            rightMotorA.Write(GpioPinValue.Low);
            rightMotorB.Write(GpioPinValue.High);

            Direction.Text = "Forward";
        }

        /* not used initially but will be needed as the robot gets smarter
        private void MoveReverse()
        {
            // spin the left motor in the reverse direction
            leftMotorA.Write(GpioPinValue.High);
            leftMotorB.Write(GpioPinValue.Low);

            // spin the right motor in the reverse direction
            rightMotorA.Write(GpioPinValue.High);
            rightMotorB.Write(GpioPinValue.Low);

            Direction.Text = "Reverse";
        }
        */

        /* not used initially but will be needed as the robot gets smarter
        private void TurnLeft()
        {
            // spin the left motor in the reverse direction
            leftMotorA.Write(GpioPinValue.High);
            leftMotorB.Write(GpioPinValue.Low);

            // spin the right motor in the forward direction
            rightMotorA.Write(GpioPinValue.Low);
            rightMotorB.Write(GpioPinValue.High);

            Direction.Text = "Left";
        }
        */

        private void TurnRight()
        {
            // spin the left motor in the forward direction
            leftMotorA.Write(GpioPinValue.Low);
            leftMotorB.Write(GpioPinValue.High);

            // spin the right motor in the reverse direction
            rightMotorA.Write(GpioPinValue.High);
            rightMotorB.Write(GpioPinValue.Low);

            Direction.Text = "Right";
        }

        private void FullStop()
        {
            // Stop the left motor
            leftMotorA.Write(GpioPinValue.Low);
            leftMotorB.Write(GpioPinValue.Low);

            // Stop the right motor
            rightMotorA.Write(GpioPinValue.Low);
            rightMotorB.Write(GpioPinValue.Low);

            Direction.Text = "Neutral";
        }

        private bool ObstacleDetected()
        {
            // use the distance sensor to get a reading on objects in front of PiBot
            DistanceReading();

            // if something is within 30 cm or 1 foot
            // then it is an obstacle that needs to be avoided
            if (distanceToObstacle < 30.0)
            {
                Obstacle.Text = "Found at " + Convert.ToString(distanceToObstacle) + " cm";
                return true;
            }
            else
            {
                return false;
            }
        }

        private void DistanceReading()
        {
            // reset the stopwatch
            sw.Reset();

            // ensure the trigger is off
            trig.Write(GpioPinValue.Low);

            // wait for the sensor to settle
            Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

            // turn on the pulse
            trig.Write(GpioPinValue.High);

            // let the pulse run for 10 microseconds
            Task.Delay(TimeSpan.FromMilliseconds(.01)).Wait();

            // turn off the pulse
            trig.Write(GpioPinValue.Low);

            //start the stopwatch
            sw.Start();

            // wait until the echo starts
            while (echo.Read() == GpioPinValue.Low)
            {
                if (sw.ElapsedMilliseconds > 1000)
                {
                    // if you have waited for more than a second, then there was a failure in the echo
                    echoFailure = true;
                    break;
                }
            }

            if (echoFailure)
            {
                // reset the echoFailure in preparation for the next reading
                echoFailure = false;
            }
            else
            {
               // echo is working properly
               // so restart the stop watch at zero
               sw.Restart();
            }

            // stop the stopwatch when the echo stops
            while (echo.Read() == GpioPinValue.High) ;
            sw.Stop();

            // the duration of the echo is equal to the pulse's roundtrip time
            elapsedTime = sw.Elapsed;

            // speed of sound is 34300 cm per second or 34.3 cm per millisecond
            // since the sound waves traveled to the obstacle and back to the sensor
            // I am dividing by 2 to represent travel time to the obstacle
            distanceToObstacle = elapsedTime.TotalMilliseconds * 34.3 / 2.0;
        }
    }
}
