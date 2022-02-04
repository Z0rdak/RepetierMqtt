# 0.1.2 - Fix crash on serialization of empty data objects
  * Add error handling for serializing empty data objects
# 0.1.1 - Rework published payload
  * Rework payload of published event and response messages
  * Payload for events now contain the printer and event name beside the actual event data
  * Payload for command responses now contain the callbackId and command name beside the actual response data
  * Remove trailing callbackId in the topic of published command responses
# 0.1.0 - Initial release
  * TBD
