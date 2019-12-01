const int GreenPin = 11, WarningPin = 12, RedPin = 13;
int greenState = 0, warningState = 0, redState = 0;
char receiveVal;
char sendVal = 'Y';
int serialCounter = 0; // to check if we're still connnected to ...

void setup()
{
  pinMode(GreenPin, OUTPUT);
  pinMode(WarningPin, OUTPUT);
  pinMode(RedPin, OUTPUT);
  Serial.begin(9600);

  checkLeds();
}

void loop()
{
  /* we check message (char) sent from Skype.Arduino program */
  if (Serial.available() > 0)
  {
    reset();
    receiveVal = Serial.read();
    if (receiveVal == 'X')
    {
      Serial.println(sendVal);
      serialCounter = 0;
      sendVal = 'X';
    }
    else
    {
      setActiveLed(receiveVal);
    }
  }
  /* Skype.Arduino must sent a message at least every 5s */
  if (serialCounter++ == 200)
  {
    reset();
    updateLedStatus();
  }

  delay(50);
}

void reset()
{
  greenState = 0;
  warningState = 0;
  redState = 0;
}

void setActiveLed(char skypeStatus)
{
  switch (skypeStatus)
  {
    case 'G':
      greenState = 1;
      break;
    case 'W':
      warningState = 1;
      break;
    case 'R':
      redState = 1;
      break;
    case '?':
      reset();
  }
  updateLedStatus();
}

void updateLedStatus()
{
  digitalWrite(GreenPin, greenState);
  digitalWrite(WarningPin, warningState);
  digitalWrite(RedPin, redState);
}

void checkLeds()
{
  checkLed(GreenPin);
  checkLed(WarningPin);
  checkLed(RedPin);
}

void checkLed(int Pin)
{
  digitalWrite(Pin, 1);
  delay(1000);
  digitalWrite(Pin, 0);
}
