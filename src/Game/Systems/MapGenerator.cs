using System;
using System.Linq;
using RogueSharp;
using RogueSharp.DiceNotation;
using RogueSharpV3Tutorial.Core;
using RogueSharpV3Tutorial.Monsters;

namespace RogueSharpV3Tutorial.Systems
{
   public class MapGenerator
   {
      private readonly int _width;
      private readonly int _height;
      private readonly int _maxRooms;
      private readonly int _roomMaxSize;
      private readonly int _roomMinSize;

      private readonly DungeonMap _map;

      // Constructing a new MapGenerator requires the dimensions of the maps it will create
      // as well as the sizes and maximum number of rooms
      public MapGenerator( int width, int height, int maxRooms, int roomMaxSize, int roomMinSize, int mapLevel  )
      {
         _width = width;
         _height = height;
         _maxRooms = maxRooms;
         _roomMaxSize = roomMaxSize;
         _roomMinSize = roomMinSize;
         _map = new DungeonMap();
      }

      // Generate a new map that places rooms randomly
      public DungeonMap CreateMap()
      {
         // Set the properties of all cells to false
         _map.Initialize( _width, _height );

         // Try to place as many rooms as the specified maxRooms
         for ( int r = 0; r < _maxRooms; r++ )
         {
            // Determine a the size and position of the room randomly
            int roomWidth = Game.Random.Next( _roomMinSize, _roomMaxSize );
            int roomHeight = Game.Random.Next( _roomMinSize, _roomMaxSize );
            int roomXPosition = Game.Random.Next( 0, _width - roomWidth - 1 );
            int roomYPosition = Game.Random.Next( 0, _height - roomHeight - 1 );

            // All of our rooms can be represented as Rectangles
            var newRoom = new Rectangle( roomXPosition, roomYPosition, roomWidth, roomHeight );

            // Check to see if the room rectangle intersects with any other rooms
            bool newRoomIntersects = _map.Rooms.Any( room => newRoom.Intersects( room ) );
            // As long as it doesn't intersect add it to the list of rooms. Otherwise throw it out
            if ( !newRoomIntersects )
            {
               _map.Rooms.Add( newRoom );
            }
         }

         // Iterate through each room that was generated
         for ( int r = 0; r < _map.Rooms.Count; r++ )
         {
            // Don't do anything with the first room
            if ( r == 0 )
            {
               continue;
            }
            // For all remaing rooms get the center of the room and the previous room
            int previousRoomCenterX = _map.Rooms[r - 1].Center.X;
            int previousRoomCenterY = _map.Rooms[r - 1].Center.Y;
            int currentRoomCenterX = _map.Rooms[r].Center.X;
            int currentRoomCenterY = _map.Rooms[r].Center.Y;

            // Give a 50/50 chance of which 'L' shaped connecting cooridors to make
            if ( Game.Random.Next( 1, 2 ) == 1 )
            {
               CreateHorizontalTunnel( previousRoomCenterX, currentRoomCenterX, previousRoomCenterY );
               CreateVerticalTunnel( previousRoomCenterY, currentRoomCenterY, currentRoomCenterX );
            }
            else
            {
               CreateVerticalTunnel( previousRoomCenterY, currentRoomCenterY, previousRoomCenterX );
               CreateHorizontalTunnel( previousRoomCenterX, currentRoomCenterX, currentRoomCenterY );
            }
         }

         // Iterate through each room that we wanted placed and call CreateRoom to make it
         foreach ( Rectangle room in _map.Rooms )
         {
            CreateRoom( room );
         }
         // Iterate through each room that we wanted placed
         // and dig out the room and create doors for it.
         foreach ( Rectangle room in _map.Rooms )
         {
            CreateRoom( room );
            CreateDoors( room );
         }

         CreateStairs();
         PlacePlayer();
         PlaceMonsters();

         return _map;
      }

      // Given a rectangular area on the map
      // set the cell properties for that area to true
      private void CreateRoom( Rectangle room )
      {
         for ( int x = room.Left + 1; x < room.Right; x++ )
         {
            for ( int y = room.Top + 1; y < room.Bottom; y++ )
            {
               _map.SetCellProperties( x, y, true, true );
            }
         }
      }
      private void CreateStairs()
      {
      _map.StairsUp = new Stairs
      {
         X = _map.Rooms.First().Center.X + 1,
         Y = _map.Rooms.First().Center.Y,
         IsUp = true
      };
      _map.StairsDown = new Stairs
      {
         X = _map.Rooms.Last().Center.X,
         Y = _map.Rooms.Last().Center.Y,
         IsUp = false
      };
      }
      
      // Carve a tunnel out of the map parallel to the x-axis
      private void CreateHorizontalTunnel( int xStart, int xEnd, int yPosition )
      {
         for ( int x = Math.Min( xStart, xEnd ); x <= Math.Max( xStart, xEnd ); x++ )
         {
            _map.SetCellProperties( x, yPosition, true, true );
         }
      }

      // Carve a tunnel out of the map parallel to the y-axis
      private void CreateVerticalTunnel( int yStart, int yEnd, int xPosition )
      {
         for ( int y = Math.Min( yStart, yEnd ); y <= Math.Max( yStart, yEnd ); y++ )
         {
            _map.SetCellProperties( xPosition, y, true, true );
         }
      }

      // Find the center of the first room that we created and place the Player there
      private void PlacePlayer()
      {
         Player player = Game.Player;
         if ( player == null )
         {
            player = new Player();
         }

         player.X = _map.Rooms[0].Center.X;
         player.Y = _map.Rooms[0].Center.Y;

         _map.AddPlayer( player );
      }
      private void CreateDoors( Rectangle room )
      {
      // The the boundries of the room
      int xMin = room.Left;
      int xMax = room.Right;
      int yMin = room.Top;
      int yMax = room.Bottom;
      
      // Put the rooms border cells into a list
      List<ICell> borderCells = _map.GetCellsAlongLine( xMin, yMin, xMax, yMin ).ToList();
      borderCells.AddRange( _map.GetCellsAlongLine( xMin, yMin, xMin, yMax ) );
      borderCells.AddRange( _map.GetCellsAlongLine( xMin, yMax, xMax, yMax ) );
      borderCells.AddRange( _map.GetCellsAlongLine( xMax, yMin, xMax, yMax ) );
      
      // Go through each of the rooms border cells and look for locations to place doors.
      foreach ( Cell cell in borderCells )
      {
         if ( IsPotentialDoor( cell ) )
         {
            // A door must block field-of-view when it is closed.
            _map.SetCellProperties( cell.X, cell.Y, false, true );
            _map.Doors.Add( new Door
            {
            X = cell.X,
            Y = cell.Y,
            IsOpen = false
            } );
         }
      }
      }
      
      // Checks to see if a cell is a good candidate for placement of a door
      private bool IsPotentialDoor( ICell cell )
      {
      // If the cell is not walkable
      // then it is a wall and not a good place for a door
      if ( !cell.IsWalkable )
      {
         return false;
      }
      
      // Store references to all of the neighboring cells 
      ICell right = _map.GetCell( cell.X + 1, cell.Y );
      ICell left = _map.GetCell( cell.X - 1, cell.Y );
      ICell top = _map.GetCell( cell.X, cell.Y - 1 );
      ICell bottom = _map.GetCell( cell.X, cell.Y + 1 );
      
      // Make sure there is not already a door here
      if ( _map.GetDoor( cell.X, cell.Y ) != null ||
            _map.GetDoor( right.X, right.Y ) != null ||
            _map.GetDoor( left.X, left.Y ) != null ||
            _map.GetDoor( top.X, top.Y ) != null ||
            _map.GetDoor( bottom.X, bottom.Y ) != null )
      {
         return false;
      }
      
      // This is a good place for a door on the left or right side of the room
      if ( right.IsWalkable && left.IsWalkable && !top.IsWalkable && !bottom.IsWalkable )
      {
         return true;
      }
      
      // This is a good place for a door on the top or bottom of the room
      if ( !right.IsWalkable && !left.IsWalkable && top.IsWalkable && bottom.IsWalkable )
      {
         return true;
      }
      return false;
      }
      private void PlaceMonsters()
      {
         foreach ( var room in _map.Rooms )
         {
            // Each room has a 60% chance of having monsters
            if ( Dice.Roll( "1D10" ) < 7 )
            {
               // Generate between 1 and 4 monsters
               var numberOfMonsters = Dice.Roll( "1D4" );
               for ( int i = 0; i < numberOfMonsters; i++ )
               {
                  // Find a random walkable location in the room to place the monster
                  Point? randomRoomLocationNullable = _map.GetRandomWalkableLocationInRoom( room );
                  Point randomRoomLocation = (Point)randomRoomLocationNullable;
                  // It's possible that the room doesn't have space to place a monster
                  // In that case skip creating the monster
                  if ( randomRoomLocation != null )
                  {
                     // Temporarily hard code this monster to be created at level 1
                     var monster = Kobold.Create( 1 );
                     monster.X = randomRoomLocation.X;
                     monster.Y = randomRoomLocation.Y;
                     _map.AddMonster( monster );
                  }
               }
            }
         }
      }
   }
}
