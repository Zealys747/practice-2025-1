using RogueSharp;
using RogueSharpV3Tutorial.Core;
using RogueSharpV3Tutorial.Interfaces;
using RogueSharpV3Tutorial.Systems;
using RSPath = RogueSharp.Path;
using RSPathFinder = RogueSharp.PathFinder;

namespace RogueSharpV3Tutorial.Behaviors
{
   public class StandardMoveAndAttack : IBehavior
   {
      public bool Act( Monster monster, CommandSystem commandSystem )
      {
         DungeonMap dungeonMap = Game.DungeonMap;
         Player player = Game.Player;
         FieldOfView monsterFov = new FieldOfView( dungeonMap );

         // If the monster has not been alerted, compute a field-of-view 
         // Use the monsters Awareness value for the distance in the FoV check
         // If the player is in the monster's FoV then alert it
         // Add a message to the MessageLog regarding this alerted status
         if ( !monster.TurnsAlerted.HasValue )
         {
            monsterFov.ComputeFov( monster.X, monster.Y, monster.Awareness, true );
            if ( monsterFov.IsInFov( player.X, player.Y ) )
            {
               Game.MessageLog.Add( $"{monster.Name} is eager to fight {player.Name}" );
               monster.TurnsAlerted = 1;
            }
         }
         
         if ( monster.TurnsAlerted.HasValue )
         {
            // Before we find a path, make sure to make the monster and player Cells walkable
            dungeonMap.SetIsWalkable( monster.X, monster.Y, true );
            dungeonMap.SetIsWalkable( player.X, player.Y, true );

            RSPathFinder pathFinder = new RSPathFinder( dungeonMap );
            RSPath path = null;

            try
            {
               path = pathFinder.ShortestPath( 
                  dungeonMap.GetCell( monster.X, monster.Y ), 
                  dungeonMap.GetCell( player.X, player.Y ) );
            }
            catch ( PathNotFoundException )
            {
               // The monster can see the player, but cannot find a path to him
               // This could be due to other monsters blocking the way
               // Add a message to the message log that the monster is waiting
               Game.MessageLog.Add( $"{monster.Name} waits for a turn" );
            }

            // Don't forget to set the walkable status back to false
            dungeonMap.SetIsWalkable( monster.X, monster.Y, false );
            dungeonMap.SetIsWalkable( player.X, player.Y, false );

            // In the case that there was a path, tell the CommandSystem to move the monster
            if ( path != null )
            {
               try
               {
                  // TODO: This should be path.StepForward() but there is a bug in RogueSharp V3
                  // The bug is that a path returned from the pathfinder does not include the source Cell
                  //commandSystem.MoveMonster( monster, path.Steps.First() );
                  commandSystem.MoveMonster( monster, path.StepForward() );
               }
               catch ( NoMoreStepsException )
               {
                  Game.MessageLog.Add( $"{monster.Name} growls in frustration" );
               }
            }

            monster.TurnsAlerted++;

            // Lose alerted status every 15 turns. 
            // As long as the player is still in FoV the monster will be realerted
            // Otherwise the monster will quit chasing the player.
            if ( monster.TurnsAlerted > 15 )
            {
               monster.TurnsAlerted = null;
            }
         }
         return true;
      }
   }
}
