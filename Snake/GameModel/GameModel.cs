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
        // NOTE THAT ALL SETTINGS ATTRIBUTES ARE SET IN THE PROGRAM'S GAMESETTINGS INSTANCE

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
        /// Field that makes powerups dictionary accessible to the outside
        /// </summary>
        public Dictionary<int, Powerup> Powerups { get { return powerups; } }

        /// <summary>
        /// Field that makes GameMode accessible to the outside
        /// </summary>
        public bool BattleRoyale { get { return GameMode; } }

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
        /// represents the special game mode
        /// </summary>
        private bool GameMode;

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
            GameMode = false;
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
            PowersDelay = (int)settings.PowersDelay!;
            GameMode = settings.BattleRoyale;
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


                newSnake.Spawn(random, WorldSize, walls, snakes);
                // where 12 is the length of newborn snakes in world units

                snakes.Add(ID, newSnake);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Method that adds a new powerup to the world and increments the ID everytime to create a new ID for each Powerup
        /// </summary>
        /// <returns></returns>
        public bool AddPower()
        {
            Random random = new Random();
            if (!powerups.ContainsKey(PowerIds) && powerups.Count < MaxPowers && random.Next(PowersDelay * 5) == 3)
            {
                Powerup newPowerup = new Powerup(PowerIds);
                newPowerup.SpawnPower(WorldSize, Walls, Snakes);
                powerups.Add(PowerIds, newPowerup);
                PowerIds++;
                return true;
            }
            return false;
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
            if (snake.DeathCounter >= RespawnRate)
                snake.Spawn(random, WorldSize, walls, snakes);
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
        public bool died;


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
        public void SpawnPower(int WorldSize, List<Wall> walls, Dictionary<int, Snake> snakes)
        {
            Random random = new Random();
            bool invalidSpawnPoint = true;

            while (invalidSpawnPoint)
            {
                loc = new(random.Next(-WorldSize / 3, WorldSize / 3),
                random.Next(-WorldSize / 3, WorldSize / 3));

                // check if this collides with any wall
                if (walls.Count == 0)
                    break;

                foreach (Wall wall in walls)
                {
                    invalidSpawnPoint = CheckWallCollision(wall);
                    if (invalidSpawnPoint)
                        break;
                }
                foreach (Snake s in snakes.Values)
                {
                    invalidSpawnPoint = CheckSnakeCollision(s);
                    if (invalidSpawnPoint)
                        break;
                }
            }
        }

        public void die()
        {
            died = true;
        }

        /// <summary>
        /// Checks if this snake collides with wall in parameter
        /// returns true if this snake collides with a wall
        /// </summary>
        public bool CheckWallCollision(Wall wall)
        {
            int powerWidth = 32; // width from middle to one side
            int wallWidth = 25; // width from middle to one side

            return (
                (loc.X + powerWidth > wall.p1.X - wallWidth) &&    // snake overlaps wall left side
                (loc.X - powerWidth < wall.p2.X + wallWidth) &&    // snake overlaps wall right side
                (loc.Y + powerWidth > wall.p1.Y - wallWidth) &&    // snake overlaps wall top side
                (loc.Y - powerWidth < wall.p2.Y + wallWidth) ||    // snake overlaps wall bottom side
                // some snakes may have different positional order. This ensures that both checks are valid.
                (loc.X + powerWidth > wall.p2.X - wallWidth) &&    // snake overlaps wall left side
                (loc.X - powerWidth < wall.p1.X + wallWidth) &&    // snake overlaps wall right side
                (loc.Y + powerWidth > wall.p2.Y - wallWidth) &&    // snake overlaps wall top side
                (loc.Y - powerWidth < wall.p1.Y + wallWidth)       // snake overlaps wall bottom side
            );

        }

        /// <summary>
        /// Checks if this powerup collides with snake in parameter
        /// returns true if this powerup collides with a snake
        /// </summary>
        public bool CheckSnakeCollision(Snake snake)
        {
            int snakeWidth = 5;
            int powerWidth = 10;

            // go through each snake portion and then return true if we collide with any rectangle described
            // collides only if the powerup overlaps with that part.
            for (int i = 0; i < snake.body.Count - 1; i++)
            {
                // check first if our head's X overlaps with the body portion
                if
                ((loc.X + powerWidth > snake.body[i].X - snakeWidth) &&      // snake overlaps portion left side
                (loc.X - powerWidth < snake.body[i + 1].X + snakeWidth) &&    // snake overlaps portion right side
                (loc.Y + powerWidth > snake.body[i].Y - snakeWidth) &&      // snake overlaps portion top side
                (loc.Y - powerWidth < snake.body[i + 1].Y + snakeWidth) ||    // snake overlaps portion bottom side
                // some snakes may have different positional order. This ensures that both checks are valid.
                (loc.X + powerWidth > snake.body[i + 1].X - snakeWidth) &&    // snake overlaps portion left side
                (loc.X - powerWidth < snake.body[i].X + snakeWidth) &&      // snake overlaps portion right side
                (loc.Y + powerWidth > snake.body[i + 1].Y - snakeWidth) &&    // snake overlaps portion top side
                (loc.Y - powerWidth < snake.body[i].Y + snakeWidth))         // snake overlaps portion bottom side
                {
                    return true;
                }
            }
            return false;

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

        [DataMember(Name = "BattleRoyale", Order = 9)]
        public bool BattleRoyale;

        [DataMember(Name = "Walls", Order = 10)]
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
            BattleRoyale = false;
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
            if (BattleRoyale != true) 
                BattleRoyale = false;
        }

    }

}