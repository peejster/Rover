// include the libraries for Windows Virtual Shields
#include <ArduinoJson.h>
#include <VirtualShield.h>
#include <Text.h>
#include <Speech.h>
#include <Recognition.h>

// include the library for the LED matrix
#include <NS_Rainbow.h>

// define the shield
VirtualShield shield;

// connect to the screen
Text screen = Text(shield);

// connect text to speech
Speech speech = Speech(shield);

// connect speech to text
Recognition recognition = Recognition(shield);

// define the pin to be used for the LED matrix
const int ledPin = 9;

// define the pin to be used for the piezo buzzer
const int buzzerPin = 3;

// define the matrix with 64 LEDs and the pin it is connected to
NS_Rainbow matrix = NS_Rainbow(64, ledPin); 

// track the current x position of the drop within each column
int xPosition[8];

// track the color of the drop within each column
uint32_t xColor[8];

// use a timer to indicate when a drop should move within each column
unsigned long dropTimer[8];

// track the fall rate for the drop within each column
int dropRate[8];

// track the x position of last lit LED within each column
int lastOn[8];

// track the x position where the drops have stacked
int stackRow[8];

// track the columns in which the drops are stacked all the way to the top
boolean columnFull[8];

// track the color of the drop when it is stacked for each LED
uint32_t lastColor[8][8];

// set upper and lower bounds for the drop rate
int minRate = 100;
int maxRate = 250;

// track the rate at which drops stack
unsigned long stackRate;

// use a timer to indicate when drops should stack
unsigned long stackTimer;

// track the number of full columns
int fullCount;

// set loop counters for the rows and columns
int x, y;

String startMessage = "Set the timer for one, two, or five minutes";

void setup()
{
  // initialize the LED matrix
  delay(100);
  matrix.begin();
  
  // clear all the LEDs
  // and show the blank matrix
  matrix.clear();
  matrix.show();
  
  // set the brightness of the matrix
  // within a range of 0 - 255
  matrix.setBrightness(128);

  // begin Virtual Shields for Arduino communication.
  // You may specify a baud rate as a parameter, default is 115200
  shield.begin();
}

void loop()
{
  // clear the phone's screen
  screen.clear();

  // print and speak instructions
  screen.print(startMessage);
  speech.speak(startMessage);
  delay(3000);

  // blocks until a word is recognized
  if (recognition.listenFor("one,two,five", false))
  {
    switch (recognition.recognizedIndex)
    {
      case 1:
        // start timer with a duration of one minute in milliseconds
        startTimer(60000);
        break;
      case 2:
        // start timer with a duration of two minutes in milliseconds
        startTimer(120000);
        break;
      case 3:
        // start timer with a duration of five minutes in milliseconds
        startTimer(300000);
        break;
    }
  }
}

void startTimer(unsigned long timer)
{
  // reset full column counter
  fullCount = 0;

  // itereate through each column
  for (y = 0; y < 8; y++)
  {
    // start drops at the top of each column - i.e. x = 0
    xPosition[y] = 0;

    // clear column stacks
    columnFull[y] = false;
    stackRow[y] = 8;

    // randomly generate the drop rate for each column
    // and set the timer
    dropRate[y] = random(minRate, maxRate);  
    dropTimer[y] = millis() + dropRate[y]; 
    
    // randomly generate a drop color for each column
    xColor[y] = random(0xFFFFFF);
  }

  // calculate the rate at which drops will stack
  // divide the timer by the number of LEDs in the matrix
  // and then set the stacking timer
  stackRate = timer / 64;
  stackTimer = millis() + stackRate;

  while (fullCount != 8)
  {
    // iterate through each column
    for (y = 0; y < 8; y++)
    {
      // check if the column is already full
      if (columnFull[y] == false)
      {
        // since the column isn't full
        // check if it's time to move the drop
        if (millis() > dropTimer[y])
        {
          // since it's time to move the drop
          // check if the drop has reached the top of the stack
          if (xPosition[y] == stackRow[y])
          {
            // since the drop has reached the top of the stack
            // check if it's time to stack the droplet
            if (millis() > stackTimer)
            {
              // it's time to stack the drop
              // so don't turn it off
              // move the stack row up the column
              stackRow[y] = stackRow[y] - 1;
            
              // record the color of the drop when it is stacked
              lastColor[stackRow[y]][y] = xColor[y];
            
              // restart the stack timer
              stackTimer = stackTimer + stackRate;

              // check if the column is full
              if (stackRow[y] == 0)
              {
                // since the column is full
                // set columnFull to true
                // and increment the number of full columns
                columnFull[y] = true;
                fullCount++;
              }
            }
            else
            {
              // drop has reached the top of the stack
              // but it's not time to stack the drop
              // so just 'turn off' the drop
              // and display the LEDs which have just been set
              matrix.setColor(convertRowCol(lastOn[y], y), 0, 0, 0);
              matrix.show();
            }
          
            // restart the drop at the top
            xPosition[y] = 0;
        
            // generate a new drop rate for the column
            dropRate[y] = random(minRate, maxRate);
          
            // generate a new color for the column
            xColor[y] = random(0xFFFFFF);
          }
          else
          {
            // the drop has not reached the top of the stack
            // check if drop is at the top of the column
            if (xPosition[y] != 0)
            {
              // drop is not at the top of the column
              // so turn off the last drop
              matrix.setColor(convertRowCol(lastOn[y], y), 0, 0, 0);
            }

            // turn on the current drop
            matrix.setColor(convertRowCol(xPosition[y], y), xColor[y]);

            // display the LEDs which have just been set
            matrix.show();

            // remember position of current drop
            lastOn[y] = xPosition[y];
        
            // increment the x position for the next drop within the column
            xPosition[y]++;
          }

          // restart the drop timer
          dropTimer[y] = millis() + dropRate[y];
        }
      }
    }
  }

  // all columns are full so the countdown has ended
  alarm();  
}

// convert (row,col) position to sequential position
int convertRowCol(int row, int col)
{
  // return the sequential position
  return (row*8)+col; 
}

// once the timer ends, sound the alarm
void alarm()
{
  // clear all the LEDs
  // and show the blank matrix
  matrix.clear();
  matrix.show();

  // alarm is sounded for 5 cycles
  for (int z = 0; z < 5; z++)
  {
    // iterate through the rows
    for (x = 0; x < 8; x++)
    {
      // iterate through the columns
      for (y = 0; y < 8; y++)
      {
        // turn on each drop using its last color
        matrix.setColor(convertRowCol(x, y), lastColor[x][y]);
      }
    }

    // display the LEDs which have just been set
    matrix.show();
         
    // turn on the buzzer
    // at a frequency roughly equivalent to G6
    tone(buzzerPin, 1568);
         
    // wait 1.5 seconds
    delay(1500);

    // clear all the LEDs
    // and show the blank matrix
    matrix.clear();
    matrix.show();

    // turn off the buzzer
    noTone(buzzerPin);
         
    // wait 1.5 seconds
    delay(1500);
  }
}
