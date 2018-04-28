#include <Arduino.h>
#include <SPI.h>
#include "Adafruit_BLE.h"
#include "Adafruit_BluefruitLE_SPI.h"
#include "Adafruit_BluefruitLE_UART.h"

#include "BluefruitConfig.h"

#if SOFTWARE_SERIAL_AVAILABLE
  #include <SoftwareSerial.h>
#endif

Adafruit_BluefruitLE_SPI ble(BLUEFRUIT_SPI_CS, BLUEFRUIT_SPI_IRQ, BLUEFRUIT_SPI_RST);

int32_t hrmServiceId;
int32_t hrmMeasureCharId;
int32_t hrmLocationCharId;


void setup(void)
{
  boolean HRMsuccess;

  ble.begin(VERBOSE_MODE);
  ble.factoryReset();
  ble.echo(false);
  
  ble.info();
  ble.sendCommandCheckOK(F("AT+GAPDEVNAME=Bluefruit HRM"));

  /* Add the Heart Rate Service definition */
  /* Service ID should be 1 */
  HRMsuccess = ble.sendCommandWithIntReply( F("AT+GATTADDSERVICE=UUID=0x180D"), &hrmServiceId);

  /* Add the Heart Rate Measurement characteristic */
  /* Chars ID for Measurement should be 1 */
  HRMsuccess = ble.sendCommandWithIntReply( F("AT+GATTADDCHAR=UUID=0x2A37, PROPERTIES=0x10, MIN_LEN=1, MAX_LEN=3, VALUE=00-40"), &hrmMeasureCharId);
  /* Chars ID for Body should be 2 */
  HRMsuccess = ble.sendCommandWithIntReply( F("AT+GATTADDCHAR=UUID=0x2A38, PROPERTIES=0x10, MIN_LEN=2, MAX_LEN=3, VALUE=00-40"), &hrmLocationCharId);

  ble.sendCommandCheckOK( F("AT+GAPSETADVDATA=02-01-06-05-02-0d-18-0a-18") );
  ble.reset();
}

void loop(void)
{

  unsigned int myo = 0;
  unsigned int gsr = 0;
  unsigned int hrv = 0;
  unsigned int spo = 0;
  float tmp = 0;
  
  for (int i = 0; i < 10; i++) { 
    
    gsr += analogRead(A1);
    myo += analogRead(A0);
    tmp += analogRead(A5);
    hrv += analogRead(A3);
    spo += analogRead(A4);
  }

  hrv = map(hrv, 20, 50, 0, 31);
  hrv = constrain(hrv, 0, 31);
  Serial.print(hrv);
  Serial.print(", ");

  spo = map(spo, 20, 50, 0, 31);
  spo = constrain(spo, 0, 31);
  Serial.print(spo);
  Serial.print(", ");
  
  gsr = map(gsr, 610, 650, 0, 31);
  gsr = constrain(gsr, 0, 31);
  Serial.print(gsr);
  Serial.print(", ");

  myo = map(myo, 30, 60, 0, 31);
  myo = constrain(myo, 0, 31);
  Serial.print(myo);
  Serial.print(", ");

  tmp = tmp / 10.0;
  tmp = (1023 / tmp)  - 1;     // (1023/ADC - 1) 
  tmp = SERIESRESISTOR / tmp;

  float steinhart;
  steinhart = tmp / THERMISTORNOMINAL;     // (R/Ro)
  steinhart = log(steinhart);                  // ln(R/Ro)
  steinhart /= BCOEFFICIENT;                   // 1/B * ln(R/Ro)
  steinhart += 1.0 / (TEMPERATURENOMINAL + 273.15); // + (1/To)
  steinhart = 1.0 / steinhart;                 // Invert
  steinhart -= 273.15;  


  tmp = map(steinhart, 25, 1000, 0, 31);
  tmp = constrain(tmp, 0, 31);
  int temperature = tmp;
  Serial.println(temperature);

  gsr = gsr << 3;
  gsr |= (unsigned int)0;
  ble.print( F("AT+GATTCHAR=") );
  ble.print( hrmMeasureCharId );
  ble.print( F(",00-") );
  ble.println(gsr, HEX);

  if ( !ble.waitForOK() ){  delay(10);  }
  delay(90);

  myo = myo << 3;
  myo |= 1;
  ble.print( F("AT+GATTCHAR=") );
  ble.print( hrmMeasureCharId );
  ble.print( F(",00-") );
  ble.println(myo, HEX);

  if ( !ble.waitForOK() ){  delay(10);  }
  delay(90);

  temperature = temperature << 3;
  temperature |= 2;
  ble.print( F("AT+GATTCHAR=") );
  ble.print( hrmMeasureCharId );
  ble.print( F(",00-") );
  ble.println(temperature, HEX);

  if ( !ble.waitForOK() ){  delay(10);  }
  delay(90);

  spo = spo << 3;
  spo |= 3;
  ble.print( F("AT+GATTCHAR=") );
  ble.print( hrmMeasureCharId );
  ble.print( F(",00-") );
  ble.println(spo, HEX);

  if ( !ble.waitForOK() ){  delay(10);  }
  delay(90);

  hrv = hrv << 3;
  hrv |= 4;
  ble.print( F("AT+GATTCHAR=") );
  ble.print( hrmMeasureCharId );
  ble.print( F(",00-") );
  ble.println(hrv, HEX);

  if ( !ble.waitForOK() ){  delay(10);  }
  delay(90);
}
