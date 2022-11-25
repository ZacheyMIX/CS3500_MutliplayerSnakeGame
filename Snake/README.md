# Parkstrikers Snake Client
#### Written by Ashton Hunt and Zachery Blomquist as part of CS3500, University of Utah, Fall 2022

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
- Six different working sound effects upon death. All sound effects are loaded at program start
- Added a class for explosions as to simulate a gif by rotating through loaded images at a high speed