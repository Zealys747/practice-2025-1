using RLNET;
using RogueSharp;

namespace RogueSharpV3Tutorial.Interfaces
{
   public interface IDrawable
   {
      RLColor Color { get; set; }
      char Symbol { get; set; }
      int X { get; set; }
      int Y { get; set; }

      void Draw( RLConsole console, IMap map );
   }
}
