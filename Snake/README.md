# Parkstrikers Snake Client
#### Written by Ashton Hunt and Zachery Blomquist as part of CS3500, University of Utah, Fall 2022

### PS8/Client Notes

## What works:
- Our client can connect to and be accepted by servers
- Our client can send messages and data over the network appropriately
- Network errors are accomodated for
- Interrupted or cut off data receives are saved for later parsing with the full message
- Client can reliably reconnect after network errors
- Visual representations including snakes, walls, powerups, images, views, explosions etc
- Sufficiently high framerate regardless of playercount or connection source

## What doesn't work:
- Possibly unaccounted for errors

## What we still have to do:
- Get everything that doesn't work working

## Things we tried to remember in development:
- Keeping the model passive
- Install JSON only in projects that use JSON
- Keep networking calls in relevant controller projects only
- Maintaining separation of concerns with MVC
- Commuticating appropriately between the model, view, and controller
- Gracefully handling weird cases like snakes dying on our same join etc
- Snakes wrapping around the map in a successful and visually intuitive way

## Noteworthy design decisions:
- ClientModel's Update method takes in JObjects instead of strings. This leaves the JSON parsing duties up to GameController
- Client IDs are stored within the ClientModel
- A client is given an option to reconnect to the server or to fully disconnect from a server given networking errors
- Several different working sound effects upon death. All sound effects are loaded at program start. Invoked when died is true for snake with client ID.
- Added a class for explosions as to simulate a gif by rotating through loaded images at a high speed. Also utilizes !alive rather than dead.


### PS9/Server Notes

## What works:
- settings.xml is read properly. Note that this only is checked in the program executable's own directory, or exactly two directories above it.
- Client can connect
- Handshake works as designed
- Snakes move and are displayed correctly

## What doesn't work:
- dead snakes will not send

## Things we tried to remember in development:
- What steps there are on the connection handshake and in which order
- Vector logic, especially with turning.

## Noteworthy design decisions:
- Server class and program are within the same file, ServerProgram.cs
- settings.xml only is checked in the program executable's own directory, or exactly two directories above it.
- Order of members in settings.xml should be left as they are. Otherwise refer to ordering within the GameSettings class.
- Some settings can not be negative, or cannot be zero, or both. SnakeSpeed, SnakeLength, MaxPowers must be positive non-zero.
PowersDelay and SnakeGrowth must not be negative.