using NetworkUtil;
using Newtonsoft.Json;
using SnakeGame;
using System.Data;

namespace ClientModel
{
    // Notes about IDs:
    // IDs cannot be negative,
    // Each object is unique to their own ID. e.g. two snakes cannot have the same ID, but a snake can have the same ID as a wall.
    // you cannot assume anything about the order of IDs for walls and powerups

    // Notes about Vector2D:
    // represents two dimensional space vector. Can represent locations. They will be used more in the server, but possibly in the client.
    public class World
    {
        public IEnumerable<Snake> snakes;
        public IEnumerable<Wall> walls;
        public IEnumerable<Powerup> powerups;
        private SocketState state;

        public World(string hostname)
        {
            snakes = new List<Snake>();
            walls = new List<Wall>();
            powerups = new List<Powerup>();
            // TODO: start a connection to the server and start updating our clientside model
            state = new()
            Networking.ConnectToServer(state.OnNetworkAction, hostname, 11000);
        }

        public void Update(SocketState s)
        {

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
        public int snake;
        /// <summary>
        /// a string representing the players name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string name;
        /// <summary>
        /// A list<Vector2D> representing th eentire body of the snake.
        /// Each point in this list represent one vertex of the snakes body where consecutive 
        /// veritices make up a straiht segment ofthe body. The frst point o fth elist give the 
        /// location fof the snake tail, and the last gives the location of the snakes head.
        /// </summary>
        [JsonProperty(PropertyName = "body")]
        public List<Vector2D> body;
        /// <summary>
        /// Represents snakes orientation, will always be axis aligned
        /// </summary>
        [JsonProperty(PropertyName = "dir")]
        public Vector2D dir;
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
        public bool join;
        public Snake()
        {
            snake = 0;
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
        public int wall;
        /// <summary>
        /// A vector2D representing 1 end point of the wall
        /// </summary>
        [JsonProperty(PropertyName = "p1")]
        public Vector2D p1;
        /// <summary>
        /// A vector2D representing the other end point of the wall
        /// </summary>
        [JsonProperty(PropertyName = "p2")]
        public Vector2D p2;
        public Wall()
        {
            wall = 0;
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
        public int power;
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
        public Powerup()
        {
            power = 0;
            loc = new();
            died = false;
        }
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class ControlCommands
    {
        /// <summary>
        /// the only possible strings are none, up, down, left, or right.
        /// </summary>
        [JsonProperty(PropertyName = "moving")]
        public string moving;
        public ControlCommands()
        {
            moving = "";
        }
    }
}