using RLNET;
using RogueSharp;
using RogueSharpV3Tutorial.Interfaces;
using RogueSharpV3Tutorial.Core;

public class Stairs : IDrawable
{
  public RLColor Color
  {
    get; set;
  }
  public char Symbol
  {
    get; set;
  }
  public int X
  {
    get; set;
  }
  public int Y
  {
    get; set;
  }
  public bool IsUp
  {
    get; set;
  }
 
  public void Draw( RLConsole console, IMap map )
  {
    if ( !map.GetCell( X, Y ).IsExplored )
    {
      return;
    }
 
    Symbol = IsUp ? '<' : '>';
 
    if ( map.IsInFov( X, Y ) )
    {
      Color = Colors.Player;
    }
    else
    {
      Color = Colors.Floor;
    }
 
    console.Set( X, Y, Color, null, Symbol );
  }
}