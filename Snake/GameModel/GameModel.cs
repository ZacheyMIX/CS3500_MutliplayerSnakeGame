using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnakeGame;
using System;
using System.Runtime.Serialization;

namespace GameModel
{
    // Notes about IDs:
    // IDs cannot be negative,
    // Each object is unique to their own ID. e.g. two snakes cannot have the same ID, but a snake can have the same ID as a wall.
    // you cannot assume anything about the order of IDs for walls and powerups

    // Notes about Vector2D:
    // represents two dimensional space vector. Can represent locations. They will be used more in the server, but possibly in the client.
    /// <summary>
    /// Clientside representation of the world.
    /// Stores locations and states of snakes, walls, and powerups.
    /// All other logic is managed by the server and communicated to our client program.
    /// </summary>
    public class ClientWorld
    {
        /// <summary>
        /// integer ID numbers to Snake objects.
        /// </summary>
        private Dictionary<int, Snake> snakes;
        /// <summary>
        /// integer ID numbers to wall objects.
        /// </summary>
        private Dictionary<int, Wall> walls;
        /// <summary>
        /// integer ID numbers to powerup objects.
        /// </summary>
        private Dictionary<int, Powerup> powerups;

        /// <summary>
        /// represents snakes that died this last frame so the view can display cute lil' explosions
        /// </summary>
        private Dictionary<int, Snake> deadSnakes;

        /// <summary>
        /// represents a death animation i.e an explosion
        /// </summary>
        private Explosion explosion;

        /// <summary>
        /// Field to make snakes dictionary accessible to the outside
        /// </summary>
        public Dictionary<int, Snake> Snakes { get { return snakes; } }
        /// <summary>
        /// Field to make walls dictionary accessible to the outside
        /// </summary>
        public Dictionary<int, Wall> Walls { get { return walls; } }
        /// <summary>
        /// Field to make powerups dictionary accessible to the outside
        /// </summary>
        public Dictionary<int, Powerup> Powerups { get { return powerups; } }
        /// <summary>
        /// Field to make deadSnakes dictionary accessible to the outside
        /// </summary>
        public Dictionary<int, Snake> DeadSnakes { get { return deadSnakes; } }


        public int ID { get; set; }

        public int WorldSize { get; set; }

        public string PlayerName { get; set; }

        /// <summary>
        /// Default constructor for clientside World class.
        /// Clientside worlds are inherently basic,
        /// so this should be the only constructor needed.
        /// </summary>
        public ClientWorld()
        {
            snakes = new();
            walls = new();
            powerups = new();
            deadSnakes = new();
            explosion = new();
            PlayerName = "";
            WorldSize = -1;
            ID = -1;
        }


        ////////////////////////
        // CLIENT SIDE UPDATERS
        ////////////////////////

        /// <summary>
        /// method for updating snakes dictionary
        /// note that this is different from UpdateWalls and UpdatePowerups
        /// in that this takes in an already parsed Snake object.
        /// </summary>
        public void UpdateSnakes(Snake? newSnake)
        {
            // method checks if snake is valid,
            // removes identical snakes (if applicable) temporarily
            // finally, if the snake is still connected, added back
            // to be drawn as a (possibly) living, (possibly) breathing, gen-you-wine snake.
            if (newSnake == null)
                return;

            if (snakes.ContainsKey(newSnake.ID))
                snakes.Remove(newSnake.ID);

            if (newSnake!.died && !deadSnakes.ContainsKey(newSnake.ID))
            {
                deadSnakes.Add(newSnake.ID, newSnake);
            }
                

            if (newSnake.dc || !newSnake.alive)
                return;     // doesn't keep snake in snakes set if snake is disconnected

            if(deadSnakes.ContainsKey(newSnake.ID))    // means new snake is a revived snake and needs to be removed
                    deadSnakes.Remove(newSnake.ID);

            // snake isn't already in snakes set and snake isn't dead, didn't die, and is still connected
            snakes.Add(newSnake.ID, newSnake!);  // again, this object should just be a snake object if it contains a key called "snake".
            return;
        }

        /// <summary>
        /// Updates walls dictionary with given nullable wall object
        /// Walls are never removed
        /// </summary>
        /// <param name="newObj"></param>
        public void UpdateWalls(Wall? newWall)
        {
            // Walls are only sent once and at the beginning
            // so method just adds these walls to the appropriate data structure.
            if (newWall is Wall)
                walls.Add(newWall.ID, newWall!);
        }

        /// <summary>
        /// Updates powerups dictionary with given nullable Powerup object
        /// Powerups are removed after being eaten
        /// </summary>
        /// <param name="newObj"></param>
        public void UpdatePowerups(Powerup? newPwp)
        {
            // Whenever a new JSON string regarding a powerup is received,
            // method checks if that ID is already in powerup data structure.
            // if it is, it's removed temporarily.
            // if the JSON string is sent because that powerup died,
            // the powerup is not added back to the data structure.
            // otherwise the powerup is added.

            if (!(newPwp is Powerup))   // if newPwp is not a Powerup
                return; // shouldn't happen but just in case

            if (powerups.ContainsKey(newPwp.ID))
                powerups.Remove(newPwp.ID);

            if (newPwp.died)
                return;

            powerups.Add(newPwp.ID, newPwp);
            return;
        }

        /// <summary>
        /// Resets the world on disconnect
        /// </summary>
        public void Reset()
        {
            snakes.Clear();
            walls.Clear();
            powerups.Clear();
            deadSnakes.Clear();
            ID = -1;
            WorldSize = -1;
            PlayerName = "";
        }
    }

    public class ServerWorld
    {
        // NOTE THAT ALL SETTINGS ATTRIBUTES ARE STORED IN THE PROGRAM'S GAMESETTINGS INSTANCE

        /// <summary>
        /// integer ID numbers to Snake objects.
        /// </summary>
        private Dictionary<int, Snake> snakes;
        /// <summary>
        /// integer ID numbers to wall objects.
        /// </summary>
        private List<Wall> walls;
        /// <summary>
        /// integer ID numbers to powerup objects.
        /// </summary>
        private Dictionary<int, Powerup> powerups;

        /// <summary>
        /// represents how large the world is
        /// </summary>
        private int WorldSize;


        /// <summary>
        /// Field to make walls list accessible to the outside
        /// </summary>
        public List<Wall> Walls { get { return walls; } }

        /// <summary>
        /// Field to make snakes dictionary accessible to the outside
        /// </summary>
        public Dictionary<int, Snake> Snakes { get { return snakes; } }

        /// <summary>
        /// Field ot makes powerups dictionary accessible to the outside
        /// </summary>
        public Dictionary<int, Powerup> Powerups { get { return powerups; } }

        /// <summary>
        /// used in random events like spawning positions
        /// </summary>
        Random random;

        /// <summary>
        /// represents how many units a snake moves in one frame
        /// </summary>
        private int SnakeSpeed;

        /// <summary>
        /// represents how many units long a snake is at spawn
        /// </summary>
        private int SnakeLength;

        /// <summary>
        /// represents how much a snake's length increases after obtaining a powerup
        /// expressed in frames per movement
        /// </summary>
        private int SnakeGrowth;

        /// <summary>
        /// represents how many powerups can be present in the game at one time
        /// </summary>
        private int MaxPowers;

        /// <summary>
        /// represents how many frames it takes to respawn a snake
        /// </summary>
        private int RespawnRate;

        /// <summary>
        /// represents how many frames it takes to spawn a new powerup
        /// </summary>
        private int PowersDelay;

        /// <summary>
        /// represents the most recent ID starting at 0
        /// </summary>
        private int PowerIds = 0;

        /// <summary>
        /// Default constructor for serverside World class.
        /// Different XML settings may require parameter constructors,
        /// but this default should work with default settings.
        /// </summary>
        public ServerWorld()
        {
            snakes = new();
            walls = new();
            powerups = new();
            random = new();
        }

        /// <summary>
        /// Constructor built for existing settings
        /// </summary>
        public ServerWorld(GameSettings settings)
        {
            snakes = new();
            powerups = new();
            random = new();

            // data members taken from settings object
            WorldSize = settings.UniverseSize;
            walls = settings.Walls;
            SnakeSpeed = (int)settings.SnakeSpeed!;
            SnakeLength = (int)settings.SnakeLength!;
            SnakeGrowth = (int)settings.SnakeGrowth!;
            MaxPowers = (int)settings.MaxPowers!;
            RespawnRate = settings.RespawnRate;
            PowersDelay = settings.PowersDelay;
        }


        ////////////////////////
        // CLIENT SIDE UPDATERS
        ////////////////////////

        /// <summary>
        /// method for updating snakes dictionary
        /// note that this is different from UpdateWalls and UpdatePowerups
        /// in that this takes in an already parsed Snake object.
        /// </summary>
        public bool AddSnake(string playerName, int ID)
        {
            // add a new snake on connection that has name field and ID field provided.
            if (!snakes.ContainsKey(ID))
            {
                // construct head randomly and then create tail
                Snake newSnake = new Snake(playerName, ID, SnakeLength, SnakeGrowth, SnakeSpeed);


                newSnake.Spawn(random, WorldSize);
                // where 12 is the length of newborn snakes in world units

                snakes.Add(ID, newSnake);

                // TODO: check to ensure snakes don't spawn on walls

                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates powerups dictionary with given nullable Powerup object
        /// Powerups are removed after being eaten
        /// </summary>
        /// <param name="newObj"></param>
        public void UpdatePowerups(Powerup? newPwp)
        {
            // Whenever a new JSON string regarding a powerup is received,
            // method checks if that ID is already in powerup data structure.
            // if it is, it's removed temporarily.
            // if the JSON string is sent because that powerup died,
            // the powerup is not added back to the data structure.
            // otherwise the powerup is added.

            if (!(newPwp is Powerup))   // if newPwp is not a Powerup
                return; // shouldn't happen but just in case

            if (powerups.ContainsKey(newPwp.ID))
                powerups.Remove(newPwp.ID);

            if (newPwp.died)
                return;

            powerups.Add(newPwp.ID, newPwp);
            return;
        }

        /// <summary>
        /// Method that adds a new powerup to the world and increments the ID everytime to create a new ID for each Powerup
        /// </summary>
        /// <returns></returns>
        public bool AddPower()
        {
            Random random = new Random();
            if (!powerups.ContainsKey(PowerIds) && powerups.Count <= MaxPowers && random.Next(200) == 3)
            {
                Powerup newPowerup = new Powerup(PowerIds);
                newPowerup.SpawnPower(WorldSize);
                powerups.Add(PowerIds, newPowerup);
                PowerIds++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the current powerup when called
        /// </summary>
        public void RemovePower()
        {

        }

        /// <summary>
        /// moves snake along in the game world
        /// </summary>
        /// <param name="iD"> ID of the snake to move </param>
        /// <param name="movement"> </param>
        public void MoveSnake(int iD, ControlCommand movement)
        {
            Vector2D left = new(-1, 0);
            Vector2D right = new(1, 0);
            Vector2D up = new(0, -1);
            Vector2D down = new(0, 1);

            if (!snakes.ContainsKey(iD))
                return;

            // snake is facing left or right
            // so we are checking if this is a valid up or down input
            if (snakes[iD].Direction.Equals(left) || snakes[iD].Direction.Equals(right))
            {
                if (movement.moving == "up")
                    snakes[iD].Turn(up);

                if (movement.moving == "down")
                    snakes[iD].Turn(down);
            }

            // snake is facing up or down
            // so we are checking if this is a valid left or right input
            else if (snakes[iD].Direction.Equals(up) || snakes[iD].Direction.Equals(down))
            {
                if (movement.moving == "left")
                    snakes[iD].Turn(left);

                if (movement.moving == "right")
                    snakes[iD].Turn(right);
            }

        }

        /// <summary>
        /// checks if snake should respawn by now.
        /// spawns the snake if so.
        /// </summary>
        /// <param name="snake"> snake that may need to be respawned </param>
        public void CheckForRespawn(Snake snake)
        {
            // if snake is alive, we shouldn't respawn it
            if (snake.alive)
                return;
            if (snake.DeathCounter == RespawnRate)
                snake.Spawn(random, WorldSize);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Snake
    {
        /// <summary>
        /// snakes unique ID, objects can have the same ID, but not the same object can have the same ID
        /// determined by server.
        /// </summary>
        [JsonProperty(PropertyName = "snake")]
        public readonly int ID;
        /// <summary>
        /// a string representing the players name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public readonly string name;
        /// <summary>
        /// A list<Vector2D> representing the entire body of the snake.
        /// Each point in this list represent one vertex of the snakes body where consecutive 
        /// veritices make up a straight segment of the body. The first point of the list give the 
        /// location of the snake tail, and the last gives the location of the snakes head.
        /// </summary>
        [JsonProperty(PropertyName = "body")]
        public readonly List<Vector2D> body;
        /// <summary>
        /// Represents snakes orientation, will always be axis aligned
        /// </summary>
        [JsonProperty(PropertyName = "dir")]
        private Vector2D dir;
        /// <summary>
        /// allows for reading private dir member outside this object
        /// </summary>
        public Vector2D Direction { get { return dir; } }
        /// <summary>
        /// Represents the players score
        /// </summary>
        [JsonProperty(PropertyName = "score")]
        public int score;
        /// <summary>
        /// A bool indicaticating the snake died within a certain frame.
        /// </summary>
        [JsonProperty(PropertyName = "died")]
        public bool died;
        /// <summary>
        /// A bool indicating whether the snake is alive or dead
        /// </summary>
        [JsonProperty(PropertyName = "alive")]
        public bool alive;
        /// <summary>
        /// A bool indicating wheter the player disconected on that frame
        /// </summary>
        [JsonProperty(PropertyName = "dc")]
        public bool dc;
        /// <summary>
        /// A bool indicating whether the player joined on this frame.
        /// </summary>
        [JsonProperty(PropertyName = "join")]
        public readonly bool join;


        ///////////////////////
        // SERVER DATA MEMBERS
        ///////////////////////
        
        /// <summary>
        /// How fast a snake travels. default 3 units per frame.
        /// </summary>
        private int speed;

        /// <summary>
        /// How much their length increases in units of frames worth of movement.
        /// default is 12.
        /// </summary>
        private int growth;

        /// <summary>
        /// used for convenience when referencing the head of our snake
        /// </summary>

        private Vector2D Head { get { return body[body.Count-1]; } }

        /// <summary>
        /// Snake should only be able to turn again once this turn counter is greater than or equal to
        /// speed * frames
        /// </summary>

        private int turnCounter;

        /// <summary>
        /// Snake should respawn once deathCounter == set respawn rate.
        /// Invoked by controller.
        /// </summary>
        private int deathCounter;

        /// <summary>
        /// allows for deathCounter to be read from outside
        /// </summary>
        public int DeathCounter { get { return deathCounter; } }

        /// <summary>
        /// Snake should stop growing/not grow when this counter is >= growth member.
        /// Invoked by controller.
        /// </summary>
        private int growthCounter;

        /// <summary>
        /// represents width of snake
        /// </summary>
        private int width;

        /// <summary>
        /// represents how long the snake is on respawn
        /// </summary>
        private int length;

        public readonly Explosion explode;

        /// <summary>
        /// Snake object constructor. Since the client only ever deserializes objects, we only need the default constructor.
        /// </summary>
        public Snake()
        {
            ID = 0;
            name = "";
            body = new();
            dir = new();
            score = 0;
            died = false;
            alive = false;
            dc = false;
            join = false;
            explode = new();
            speed = 3;
            growth = 12;
        }

        /// <summary>
        /// Serverside snake constructor. Used when a new snake enters the game.
        /// </summary>
        /// <param name="Name"> name of the snake as a string </param>
        /// <param name="iD"> ID of the snake as an integer </param>
        /// <param name="len"> initial length of a snake as an integer</param>
        /// <param name="grow"> how many frames worth of movement the snake grows after a powerup,
        /// as an integer. </param>
        /// <param name="spd"> how many units a snake moves in a frame </param>
        public Snake(string Name, int iD, int len, int grow, int spd)
        {
            // note: this is the state of our snake on its first frame on the server

            ID = iD;
            name = Name;
            body = new();
            score = 0;
            died = false;
            alive = false;
            dc = false;
            join = true;
            explode = new();
            growth = grow;
            dir = new();
            body = new();

            // set width of snake
            width = 10; // 10 game units

            // set counters
            deathCounter = 0;
            turnCounter = 0;
            growthCounter = growth + 1;

            // settings set by settings.xml
            length = len;
            speed = spd;
            growth = grow;
        }

        /// <summary>
        /// Spawns this snake on the specified vectors head and tail
        /// use this after snake join and snake respawn
        /// </summary>
        public void Spawn(Random random, int WorldSize)
        {
            // don't respawn again if already alive
            if (alive)
                return;
            Vector2D head, tail;


            head = new(random.Next(-WorldSize / 4, WorldSize / 4),
                random.Next(-WorldSize / 4, WorldSize / 4));

            int randomDir = random.Next(4); // randomizes whatever direction we spawn in

            if (randomDir == 0)
                tail = new(head.X - length, head.Y);
            else if (randomDir == 1)
                tail = new(head.X + length, head.Y);
            else if (randomDir == 2)
                tail = new(head.X, head.Y - length);
            else // randomDir == 3
                tail = new(head.X, head.Y + length);

            body.Add(head);
            body.Add(tail);

            // normalize direction
            dir = tail - head;
            dir.Normalize();

            alive = true;
        }

        /// <summary>
        /// turns snake in direction specified by newdir
        /// </summary>
        public void Turn(Vector2D newdir)
        {
            // not enough time has passed to be able to turn again
            if (turnCounter * speed < width)
                return;

            body.Add(new Vector2D(Head.X, Head.Y)); // adds new body segment at last place before turn
            dir = newdir;                           // changes body direction
            turnCounter = 0;                        // reset turn counter
        }

        /// <summary>
        /// moves snake its predetermined distance every frame
        /// </summary>
        /// <param name="worldSize"> used to compute crossing the border </param>
        public bool Move(int worldSize)
        {
            // should not be invoked if the snake is dead
            if (!alive)
                return false;

            //add to head, tail
            //remove tail if it catches up to next portion

            // update head
            Head.X += speed * dir.X;
            Head.Y += speed * dir.Y;

            // check if head has crossed over the world boundary

            if (Head.X < -worldSize / 2)
            {
                double headX = Head.X;  // used so that we don't lose track of original head position
                body.Add(new Vector2D(-worldSize / 2, Head.Y));     // add first border
                body.Add(new Vector2D(headX + worldSize, Head.Y));  // add second border
                body.Add(new Vector2D(headX + worldSize, Head.Y));  // add head at second border
            }
            if (Head.X > worldSize / 2)
            {
                // same as before just different direction
                double headX = Head.X;
                body.Add(new Vector2D(worldSize / 2, Head.Y));
                body.Add(new Vector2D(headX - worldSize, Head.Y));
                body.Add(new Vector2D(headX - worldSize, Head.Y));
            }
            if (Head.Y < -worldSize / 2)
            {
                // same as before just on Y border
                double headY = Head.Y;
                body.Add(new Vector2D(Head.X, -worldSize / 2));
                body.Add(new Vector2D(Head.X, headY + worldSize));
                body.Add(new Vector2D(Head.X, headY + worldSize));
            }
            if (Head.Y > worldSize / 2)
            {
                // same as before just different direction
                double headY = Head.Y;
                body.Add(new Vector2D(Head.X, worldSize / 2));
                body.Add(new Vector2D(Head.X, headY - worldSize));
                body.Add(new Vector2D(Head.X, headY - worldSize));
            }

            // check if snake is growing before moving the tail
            if (growthCounter <= growth)
                growthCounter++;

            else
            {
                // update tail
                // tail should gravitate towards next body vector
                Vector2D tempDirection = body[1] - body[0]; // throws here from invalid index
                tempDirection.Normalize();

                body[0].X += tempDirection.X * speed;
                body[0].Y += tempDirection.Y * speed;

                // remove last portion if tail catches up to or is further than next portion of body
                if (body[0].Equals(body[1]))
                    body.Remove(body[0]);
                else if (tempDirection.Equals(new Vector2D(0, -1)) && body[0].Y < body[1].Y)
                // tail is moving up and is further up than the next segment
                    body.Remove(body[0]);
                else if (tempDirection.Equals(new Vector2D(-1, 0)) && body[0].X < body[1].X)
                // tail is moving left and is further left than the next segment
                    body.Remove(body[0]);
                else if (tempDirection.Equals(new Vector2D(0, 1)) && body[0].Y > body[1].Y)
                    // tail is moving down and is further down than the next segment
                    body.Remove(body[0]);
                else if (tempDirection.Equals(new Vector2D(1, 0)) && body[0].X > body[1].X)
                // tail is moving right and is further right than the next segment
                    body.Remove(body[0]);

                // if tail is on border we should remove it
                if (body[0].X <= -worldSize / 2)    // left border
                {
                    body.Remove(body[0]);   // removes tail
                    //body.Remove(body[0]);   // removes border body portion
                }
                else if (body[0].X >= worldSize / 2)// right border
                {
                    body.Remove(body[0]);
                   //body.Remove(body[0]);
                }
                else if (body[0].Y <= -worldSize / 2)// top border
                {
                    body.Remove(body[0]);
                   //body.Remove(body[0]);
                }
                else if (body[0].Y >= worldSize / 2)// bottom border
                {
                    body.Remove(body[0]);
                  //body.Remove(body[0]);
                }
            }

            // movement completed successfully
            return true;
        }

        /// <summary>
        /// method used for setting the snake up to grow after a powerup has been picked up
        /// </summary>
        public void Grow()
        {
            growthCounter = 0;
            score++;
        }

        /// <summary>
        /// changes snake to be representative of disconnecting from server
        /// to be invoked whenever a client is disconnected
        /// </summary>
        public void Disconnect()
        {
            dc = true;
            alive = false;
            died = true;
        }

        /// <summary>
        /// Helps set counter for turn timing
        /// </summary>
        public void incrementTurnCounter()
        {
            turnCounter++;
        }

        /// <summary>
        /// Helps set counter for death timing
        /// </summary>
        public void incrementDeathCounter()
        {
            deathCounter++;
            died = false;
        }

        /// <summary>
        /// Checks if this snake collides with wall in parameter
        /// kills snake if this is a valid collision
        /// </summary>
        public bool CheckWallCollision(Wall wall)
        {
            // walls are 50x50 square units
            // if head falls within region of wall, snake dies.
            return (Head.X <= wall.p1.X + 25 && Head.X >= wall.p2.X - 25) && 
                (Head.Y <= wall.p1.Y + 25 || Head.Y >= wall.p2.Y - 25);
        }

        /// <summary>
        /// Snake has collided into something.
        /// </summary>
        public void die()
        {
            died = true;
            alive = false;
            deathCounter = 0;
        }

    }

    [DataContract (Namespace = "")]
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        /// <summary>
        /// ID
        /// </summary>
        [DataMember (Name = "ID")]
        [JsonProperty(PropertyName = "wall")]
        public readonly int ID;
        /// <summary>
        /// A vector2D representing 1 end point of the wall
        /// </summary>
        [DataMember (Name = "p1")]
        [JsonProperty(PropertyName = "p1")]
        public readonly Vector2D p1;
        /// <summary>
        /// A vector2D representing the other end point of the wall
        /// </summary>
        [DataMember (Name = "p2")]
        [JsonProperty(PropertyName = "p2")]
        public readonly Vector2D p2;
        /// <summary>
        /// Wall object constructor. Since the client only ever deserializes objects, we only need the default constructor.
        /// </summary>
        public Wall()
        {
            ID = 0;
            p1 = new();
            p2 = new();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup
    {
        /// <summary>
        /// ID
        /// </summary>
        [JsonProperty(PropertyName = "power")]
        public readonly int ID;
        /// <summary>
        /// Represents the location of the powerup
        /// </summary>
        [JsonProperty(PropertyName = "loc")]
        public Vector2D loc;

        /// <summary>
        /// A bool indicates whether the powerup has died or has been consumed.
        /// </summary>
        [JsonProperty(PropertyName = "died")]
        public readonly bool died;


        /// <summary>
        /// Powerup object constructor. Since the client only ever deserializes objects, we only need the default constructor.
        /// </summary>
        public Powerup()
        {
            ID = 0;
            loc = new();
            died = false;
        }

        /// <summary>
        /// Cutsom Powerup object constructor. Since the client only ever deserializes objects, we only need the default constructor.
        /// </summary>
        public Powerup(int ID)
        {
            this.ID = ID;
            loc = new();
            died = false;
        }

        /// <summary>
        /// Spawns powerup based on random point within the worldsize
        /// </summary>
        /// <param name="loc"></param>
        public void SpawnPower(int WorldSize)
        {
            Random random = new Random();
            loc = new(random.Next(-WorldSize / 3, WorldSize / 3),
                random.Next(-WorldSize / 3, WorldSize / 3));
        }

    }

    public class Explosion
    {
        public int currentFrame;
        public Explosion()
        {
            currentFrame = 0;
        }

        public void runThroughFrames()
        {
            currentFrame++;   
        }
    }


    /// <summary>
    /// Helper class for sending move commands over the network
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ControlCommand
    {
        /// <summary>
        /// the only possible strings are none, up, down, left, or right.
        /// </summary>
        [JsonProperty(PropertyName = "moving")]
        public string moving;
        /// <summary>
        /// Only necessary constructor for our helper class
        /// </summary>
        /// <param name="direction"> should only be "none", "up", "down", "left" or "right" </param>
        public ControlCommand(string direction)
        {
            moving = direction;
        }
    }

    /// <summary>
    /// Used as a kind of helper class on Server construct to read provided settings.xml file
    /// </summary>
    [DataContract(Namespace = "")]
    public class GameSettings
    {
        // note that this doesn't do anything.
        [DataMember(Name = "FramesPerShot", Order = 0)]
        public int FramesPerShot;

        // note that all following nullable data members are not included in legacy settings.
        // therefore, we need our OnDeserialize method to make these work.

        [DataMember(Name = "MSPerFrame", Order = 1)]
        public int MSPerFrame;

        [DataMember(Name = "RespawnRate", Order = 2)]
        public int RespawnRate;

        [DataMember(Name = "UniverseSize", Order = 3)]
        public int UniverseSize;

        [DataMember(Name = "PowersDelay", Order = 4)]
        public int? PowersDelay;

        [DataMember(Name = "MaxPowers", Order = 5)]
        public int? MaxPowers;

        [DataMember(Name = "SnakeSpeed", Order = 6)]
        public int? SnakeSpeed;

        [DataMember(Name = "SnakeGrowth", Order = 7)]
        public int? SnakeGrowth;

        [DataMember(Name = "SnakeLength", Order = 8)]
        public int? SnakeLength;


        [DataMember(Name = "Walls", Order = 9)]
        public List<Wall> Walls;

        public GameSettings()
        {
            Walls = new();
            FramesPerShot = 0;
            RespawnRate = 0;
            UniverseSize = 0;
            MSPerFrame = 0;

            // default settings for a server without these fields in its settings.xml file
            SnakeSpeed = 3;
            SnakeLength = 120;
            SnakeGrowth = 12;
            MaxPowers = 20;
            PowersDelay = 20;
        }

        /// <summary>
        /// sets fields to default settings if they're excluded from read file
        /// </summary>
        [OnDeserialized()]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (SnakeSpeed is null || SnakeSpeed <= 0)  // snake speed should not be negative
                SnakeSpeed = 3;
            if (SnakeLength is null || SnakeLength <= 0)// snake length should not be negative
                SnakeLength = 120;
            if (SnakeGrowth is null || SnakeGrowth < 0) // snake growth should not be negative but can be zero
                SnakeGrowth = 12;
            if (MaxPowers is null || MaxPowers <= 0)    // max powerups should not be negative
                MaxPowers = 20;
            if (PowersDelay is null || PowersDelay < 0) // powerup delay should not be negative but can be zero
                PowersDelay = 20;
            if (UniverseSize < SnakeLength)
                UniverseSize = (int)SnakeLength * 17;

            //// check if wall positions are valid
            //foreach (Wall wall in Walls)
            //{
            //    // checks if positions are too low
            //    if (wall.p1.X < -UniverseSize / 2 || wall.p1.Y < -UniverseSize / 2)
            //        Walls.Remove(wall);
            //    else if (wall.p2.X < -UniverseSize / 2 || wall.p2.Y < -UniverseSize / 2)
            //        Walls.Remove(wall);

            //    // checks if positions are too high
            //    else if (wall.p1.X > UniverseSize / 2 || wall.p1.Y > UniverseSize / 2)
            //        Walls.Remove(wall);
            //    else if (wall.p2.X > UniverseSize / 2 || wall.p2.Y > UniverseSize / 2)
            //        Walls.Remove(wall);
            //}

        }

    }

}