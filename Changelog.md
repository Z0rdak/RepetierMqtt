﻿# [0.1.3] - 2023-06-16
## Added
* RepetierMqtt now supports net5.0 and net6.0.
## Changed 
* Updated RepetierSharp version to 0.1.8.1
* Updated MQTTnet version to 4.2.1.781 (latest)
# 0.1.2 - Fix crash on serialization of empty data objects
  * Add error handling for serializing empty data objects
# 0.1.1 - Rework published payload
  * Rework payload of published event and response messages
  * Payload for events now contain the printer and event name beside the actual event data
  * Payload for command responses now contain the callbackId and command name beside the actual response data
  * Remove trailing callbackId in the topic of published command responses
# 0.1.0 - Initial release
  * TBD
