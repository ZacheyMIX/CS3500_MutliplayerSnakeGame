using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnakeGame;
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
        private Dictionary<long, Snake> snakes;
        /// <summary>
        /// integer ID numbers to wall objects.
        /// </summary>
        private List<Wall> walls;
        /// <summary>
        /// integer ID numbers to powerup objects.
        /// </summary>
        private Dictionary<int, Powerup> powerups;

        //public List<Wall> Walls { get { return walls; } }


        ////////////////////////////
        // SERVER SIDE DATA MEMBERS
        ////////////////////////////

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
        }


        ////////////////////////
        // CLIENT SIDE UPDATERS
        ////////////////////////

        /// <summary>
        /// method for updating snakes dictionary
        /// note that this is different from UpdateWalls and UpdatePowerups
        /// in that this takes in an already parsed Snake object.
        /// </summary>
        public void AddSnake(string playerName, long ID)
        {
            // add a new snake on connection that has name field and ID field provided.
            if (!snakes.ContainsKey(ID))
                snakes.Add(ID, new Snake(playerName, ID));
        }

        /// <summary>
        /// Updates walls dictionary with given wall object
        /// Walls are never removed
        /// should be used at server initialization when reading XML file
        /// </summary>
        /// <param name="newObj"></param>
        public void SetWalls(List<Wall> newWalls)
        {
            walls = newWalls;
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

        public void MoveSnake(long iD, ControlCommand movement)
        {
            // TODO: write movement method
        }

        // note: we removed Reset method.
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
        /// A list<Vector2D> representing th eentire body of the snake.
        /// Each point in this list represent one vertex of the snakes body where consecutive 
        /// veritices make up a straiht segment ofthe body. The frst point of the list give the 
        /// location of the snake tail, and the last gives the location of the snakes head.
        /// </summary>
        [JsonProperty(PropertyName = "body")]
        public readonly List<Vector2D> body;
        /// <summary>
        /// Represents snakes orientation, will always be axis aligned
        /// </summary>
        [JsonProperty(PropertyName = "dir")]
        public readonly Vector2D dir;
        /// <summary>
        /// Represents the players score
        /// </summary>
        [JsonProperty(PropertyName = "score")]
        public readonly int score;
        /// <summary>
        /// A bool indicaticating the snake died within a certain frame.
        /// </summary>
        [JsonProperty(PropertyName = "died")]
        public readonly bool died;
        /// <summary>
        /// A bool indicating whether the snake is alive or dead
        /// </summary>
        [JsonProperty(PropertyName = "alive")]
        public readonly bool alive;
        /// <summary>
        /// A bool indicating wheter the player disconected on that frame
        /// </summary>
        [JsonProperty(PropertyName = "dc")]
        public readonly bool dc;
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

        public Snake(string Name, long iD)
        {
            ID = (int)iD;
            name = Name;
            body = new();   // remember to randomize
            dir = new();    // remember to randomize
            score = 0;
            died = false;
            alive = false;
            dc = false;
            join = false;
            explode = new();
            speed = 3;
            growth = 12;
        }

        // REMEMBER TO REMOVE
        private void Speed()
        {
            speed++;
            growth++;
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
        public readonly Vector2D loc;

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

}