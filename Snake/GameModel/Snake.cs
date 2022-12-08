using Newtonsoft.Json;
using SnakeGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameModel
{
    // Moved here so it takes up less space in GameModel.cs

    /// <summary>
    /// represents snakes for both the client and the server
    /// as a server does more logical things with snakes,
    /// server related methods will be more complex.
    /// All players in our game are snakes, represented by a list of Vector2Ds.
    /// A snake can move, eat, and collide with other objects.
    /// </summary>
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

        private Vector2D Head { get { return body[body.Count - 1]; } }

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
        /// Spawns this snake on a random position using random parameter
        /// use this after snake join and snake respawn
        /// </summary>
        public void Spawn(Random random, int WorldSize, List<Wall> walls, Dictionary<int, Snake> snakes)
        {
            // don't respawn again if already alive
            if (alive)
                return;
            Vector2D head, tail;
            bool invalidSpawnPoint = true;

            while (invalidSpawnPoint)
            {
                body.Clear();

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

                // if there's no walls to overlap with, don't check for wall overlaps 
                if (walls.Count == 0)
                    break;

                // check if there's some overlap with any wall on the map
                foreach (Wall wall in walls)
                {
                    invalidSpawnPoint = SpawnedOnAWall(wall);
                    if (invalidSpawnPoint)
                        break;
                }

                // don't proceed to check for invalid spawns on snakes if wall spawn is invalid
                if (invalidSpawnPoint)
                    continue;

                // check if we spawned on top of any snakes
                foreach (Snake s in snakes.Values)
                {
                    invalidSpawnPoint = CheckSnakeCollision(s);
                    if (invalidSpawnPoint)
                        break;
                }
            }
            score = 0;
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
        /// returns true if this snake collides with a wall
        /// </summary>
        public bool CheckWallCollision(Wall wall)
        {
            int snakeWidth = 5; // width from middle to one side
            int wallWidth = 25; // width from middle to one side
            // walls are 50x50 square units
            // if head falls within region of wall, snake dies.
            foreach (Vector2D segment in body)
            {
                // no gap must be present between any of the 4 sides of the rectangles
                if (
                    (segment.X + snakeWidth > wall.p1.X - wallWidth) &&    // snake overlaps wall left side
                    (segment.X - snakeWidth < wall.p2.X + wallWidth) &&    // snake overlaps wall right side
                    (segment.Y + snakeWidth > wall.p1.Y - wallWidth) &&    // snake overlaps wall top side
                    (segment.Y - snakeWidth < wall.p2.Y + wallWidth)       // snake overlaps wall bottom side
                )
                {
                    return true;
                }

                // some walls may have different positional order. This ensures that both checks are valid.
                if (
                    (segment.X + snakeWidth > wall.p2.X - wallWidth) &&    // snake overlaps wall left side
                    (segment.X - snakeWidth < wall.p1.X + wallWidth) &&    // snake overlaps wall right side
                    (segment.Y + snakeWidth > wall.p2.Y - wallWidth) &&    // snake overlaps wall top side
                    (segment.Y - snakeWidth < wall.p1.Y + wallWidth)       // snake overlaps wall bottom side
                )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// used to verify snake's spawn position.
        /// returns true if current spawn position is on a wall,
        /// and false if not.
        /// </summary>
        private bool SpawnedOnAWall(Wall wall)
        {
            // much more than normal so we have more room to move when we spawn
            int snakeWidth = 10;
            int wallWidth = 100;
            // snake goes from snake's head X - snakeWidth to snake tail's head X + snakeWidth
            // and from snake's head Y - snakeWidth to snake tail's Y + snakeWidth
            // check the reverse as well in case of spawning in reverse
            // wall goes from p1 X - wallWidth to p2 X + wallWidth
            // and from p1 Y - wallWidth to p2 Y + wallWidth
            // reverse this too in case of reverse
            bool firstWallCheck =
                (body[1].X + snakeWidth > wall.p1.X - wallWidth) &&
                (body[0].X - snakeWidth < wall.p2.X + wallWidth) &&
                (body[1].Y + snakeWidth > wall.p1.Y - wallWidth) &&
                (body[0].Y - snakeWidth < wall.p2.Y + wallWidth);
            bool secondWallCheck =
                (body[0].X + snakeWidth > wall.p2.X - wallWidth) &&
                (body[1].X - snakeWidth < wall.p1.X + wallWidth) &&
                (body[0].Y + snakeWidth > wall.p2.Y - wallWidth) &&
                (body[1].Y - snakeWidth < wall.p1.Y + wallWidth);
            return firstWallCheck || secondWallCheck;
        }

        /// <summary>
        /// Checks collision between snake and powerups
        /// returns true if a snake collides with a powerup
        /// </summary>
        public bool CheckPowerCollision(Powerup power)
        {
            int snakeWidth = 5;
            int powerWidth = 8;

            foreach(Vector2D segment in body)
            {

                if (
                        (segment.X + snakeWidth > power.loc.X - powerWidth) &&    // snake overlaps powerup left side
                        (segment.X - snakeWidth < power.loc.X + powerWidth) &&    // snake overlaps powerup right side
                        (segment.Y + snakeWidth > power.loc.Y - powerWidth) &&    // snake overlaps powerup top side
                        (segment.Y - snakeWidth < power.loc.Y + powerWidth)       // snake overlaps powerup bottom side
                    )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks for collisions with every snake in the game.
        /// Redirects to CheckSelfCollision if the snake given is this snake.
        /// </summary>
        public bool CheckSnakeCollision(Snake snake)
        {
            if (snake.ID == ID)
                return CheckSelfCollision();

            int snakeWidth = 5;

            // go through each snake portion and then return true if we collide with any rectangle described
            // collides only if this snake's head overlaps with that part.
            for (int i = 0; i < snake.body.Count - 1; i++)
            {
                // check first if our head's X overlaps with the body portion
                if
                ((Head.X + snakeWidth > snake.body[i].X - snakeWidth) &&      // snake overlaps portion left side
                (Head.X - snakeWidth < snake.body[i+1].X + snakeWidth) &&    // snake overlaps portion right side
                (Head.Y + snakeWidth > snake.body[i].Y - snakeWidth) &&      // snake overlaps portion top side
                (Head.Y - snakeWidth < snake.body[i+1].Y + snakeWidth) ||    // snake overlaps portion bottom side
                // some walls may have different positional order. This ensures that both checks are valid.
                (Head.X + snakeWidth > snake.body[i+1].X - snakeWidth) &&    // snake overlaps portion left side
                (Head.X - snakeWidth < snake.body[i].X + snakeWidth) &&      // snake overlaps portion right side
                (Head.Y + snakeWidth > snake.body[i+1].Y - snakeWidth) &&    // snake overlaps portion top side
                (Head.Y - snakeWidth < snake.body[i].Y + snakeWidth))         // snake overlaps portion bottom side
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// checks if snake collides with itself
        /// </summary>
        private bool CheckSelfCollision()
        {
            int snakeWidth = 5;

            for (int i = 0; i < body.Count - 3; i++)
            {
                    // check if our head overlaps with the body portion
                    if
                   ((Head.X + snakeWidth > body[i].X - snakeWidth) &&        // snake overlaps portion left side
                    (Head.X - snakeWidth < body[i + 1].X + snakeWidth) &&    // snake overlaps portion right side
                    (Head.Y + snakeWidth > body[i].Y - snakeWidth) &&        // snake overlaps portion top side
                    (Head.Y - snakeWidth < body[i + 1].Y + snakeWidth) ||    // snake overlaps portion bottom side
                    // some walls may have different positional order. This ensures that both checks are valid.
                    (Head.X + snakeWidth > body[i + 1].X - snakeWidth) &&    // snake overlaps portion left side
                    (Head.X - snakeWidth < body[i].X + snakeWidth) &&        // snake overlaps portion right side
                    (Head.Y + snakeWidth > body[i + 1].Y - snakeWidth) &&    // snake overlaps portion top side
                    (Head.Y - snakeWidth < body[i].Y + snakeWidth))          // snake overlaps portion bottom side
                    {
                        return true;
                    }
            }

            return false;
        }

        /// <summary>
        /// Snake has collided into something.
        /// we need to represent its state accordingly
        /// </summary>
        public void die()
        {
            if (!alive)
                return;

            died = true;
            alive = false;
            deathCounter = 0;
        }

    }
}
