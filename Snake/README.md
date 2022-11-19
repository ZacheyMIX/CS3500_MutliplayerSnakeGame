# Parkstrikers Snake Client
#### Written by Ashton Hunt and Zachery Blomquist as part of CS3500, University of Utah, Fall 2022

## What works:
- Our client can connect to and be accepted by servers
- Our client can send messages and data over the network appropriately
- Network errors are accomodated for

## What doesn't work:
- Visual representations
- A network error occurs immediately after connection
- Possibly unaccounted for errors

## What we still have to do:
- Get everything that doesn't work working
- Connect GameController.cs, WorldPanel.cs, and MainPage.xaml.cs

## Things we tried to remember in development:
- Keeping the model passive
- Install JSON only in projects that use JSON
- Keep networking calls in relevant controller projects only
- Maintaining separation of concerns with MVC
- Commuticating appropriately between the model, view, and controller