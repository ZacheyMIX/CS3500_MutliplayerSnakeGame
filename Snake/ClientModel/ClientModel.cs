using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnakeGame;

namespace ClientModel
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
    public class World
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
        public World()
        {
            // World size is 2000x2000. Might need to change this to make that a parameter.
            snakes = new();
            walls = new();
            powerups = new();
            deadSnakes = new();
            PlayerName = "";
            WorldSize = -1;
            ID = -1;
        }

        /// <summary>
        /// Updates the World model every time a new Json string is received
        /// </summary>
        public void Update(JObject newObj)
        {
            if (newObj.ContainsKey("snake"))
            {
                // method checks if snake is valid,
                // removes identical snakes (if applicable) temporarily
                // if the snake died this same frame, and isn't already in the deadSnakes data structure,
                // snake is added to deadSnakes for display in view.
                // otherwise, snake has been dead for a little bit, and isn't added to anything.
                // finally, if the snake isn't dead, snake is added to snakes
                // to be drawn as a living, breathing, gen-you-wine snake.

                Snake? newSnake = newObj.ToObject<Snake>(); // nullable only to appease return type of DeserializeObject method.
                if (!(newSnake is Snake))
                    return; // shouldn't happen but just in case

                if (snakes.ContainsKey(newSnake.ID))
                    snakes.Remove(newSnake.ID);

                // checks if snake is supposed to go up in a puff of smoke this frame
                if (newSnake!.died && !deadSnakes.ContainsKey(newSnake.ID))
                    deadSnakes.Add(newSnake.ID, newSnake);


                if (newSnake.dc || !newSnake.alive)
                    return;     // doesn't keep snake in snakes set if snake is dead or disconnected

                if (deadSnakes.ContainsKey(newSnake.ID))    // means new snake is a revived snake and needs to be removed
                    deadSnakes.Remove(newSnake.ID);

                // snake isn't already in snakes set and snake isn't dead, didn't die, and is still connected
                snakes.Add(newSnake.ID, newSnake!);  // again, this object should just be a snake object if it contains a key called "snake".
                return;
            }
            else if (newObj.ContainsKey("wall"))
            {
                // Walls are only sent once and at the beginning
                // so method just adds these walls to the appropriate data structure.
                Wall? newWall = newObj.ToObject<Wall>();
                if (newWall is Wall)
                    walls.Add(newWall.ID, newWall!);
                return;
            }
            else if (newObj.ContainsKey("power"))
            {
                // Whenever a new JSON string regarding a powerup is received,
                // method checks if that ID is already in powerup data structure.
                // if it is, it's removed temporarily.
                // if the JSON string is sent because that powerup died,
                // the powerup is not added back to the data structure.
                // otherwise the powerup is added.

                Powerup? newPwp = newObj.ToObject<Powerup>();

                if (!(newPwp is Powerup))   // if newPwp is not a Powerup
                    return; // shouldn't happen but just in case
                
                if (powerups.ContainsKey(newPwp.ID))
                    powerups.Remove(newPwp.ID);

                if (newPwp.died)
                    return;

                powerups.Add(newPwp.ID, newPwp);
                return;
            }
        }

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
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        /// <summary>
        /// ID
        /// </summary>
        [JsonProperty(PropertyName = "wall")]
        public readonly int ID;
        /// <summary>
        /// A vector2D representing 1 end point of the wall
        /// </summary>
        [JsonProperty(PropertyName = "p1")]
        public readonly Vector2D p1;
        /// <summary>
        /// A vector2D representing the other end point of the wall
        /// </summary>
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
}